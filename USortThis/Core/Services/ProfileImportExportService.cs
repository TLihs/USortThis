using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Service for importing and exporting profile data.
    /// </summary>
    public sealed class ProfileImportExportService
    {
        private const int CurrentFormatVersion = 1;
        private readonly IIdFactory _idFactory;
        private readonly IFolderBridgeModelFactory _bridgeFactory;
        private readonly IFolderItemModelFactory _folderFactory;
        private readonly IFilterRuleCriteriaFactory _criteriaFactory;
        private readonly IDatabaseService _databaseService;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileImportExportService"/> class.
        /// </summary>
        /// <param name="idFactory">The ID factory.</param>
        /// <param name="bridgeFactory">The bridge model factory.</param>
        /// <param name="folderFactory">The folder item model factory.</param>
        /// <param name="criteriaFactory">The filter rule criteria factory.</param>
        /// <param name="databaseService">The database service.</param>
        public ProfileImportExportService(IIdFactory idFactory, IFolderBridgeModelFactory bridgeFactory,
            IFolderItemModelFactory folderFactory, IFilterRuleCriteriaFactory criteriaFactory, IDatabaseService databaseService)
        {
            _idFactory = idFactory;
            _bridgeFactory = bridgeFactory;
            _folderFactory = folderFactory;
            _criteriaFactory = criteriaFactory;
            _databaseService = databaseService;
        }

        /// <summary>
        /// Exports the specified profile to the given file path.
        /// </summary>
        /// <param name="profile">The profile to export.</param>
        /// <param name="filePath">The file path to export the profile to.</param>
        /// <exception cref="ArgumentNullException">Thrown when profile is null.</exception>
        /// <exception cref="ArgumentException">Thrown when file path is null or whitespace.</exception>
        public void Export(ProfileItemModel profile, string filePath)
        {
            ArgumentNullException.ThrowIfNull(profile);
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

            var document = new ProfileDocument
            {
                FormatVersion = CurrentFormatVersion,
                Name = profile.Name,
                ConflictHandling = profile.ConflictHandling.ToString(),
                Bridges = profile.GetFolderBridges().Select(bridge => new BridgeDocument
                {
                    Name = bridge.CustomName,
                    SourcePath = bridge.SourceFolder?.FolderPath,
                    MoveSourceAfterCopy = bridge.MoveSourceAfterCopy,
                    Routes = bridge.Routes.Select(route => new RouteDocument
                    {
                        Name = route.Name,
                        DestinationPath = route.DestinationPath,
                        PathMode = route.PathMode.ToString(),
                        DateFolderPattern = route.DateFolderPattern,
                        UseCreationTime = route.UseCreationTime,
                        Criteria = route.Criterias.Select(ExportCriteria).ToList()
                    }).ToList()
                }).ToList()
            };

            File.WriteAllText(filePath, JsonSerializer.Serialize(document, new JsonSerializerOptions { WriteIndented = true }));
        }

        /// <summary>
        /// Imports a profile from the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to import the profile from.</param>
        /// <returns>The imported profile.</returns>
        /// <exception cref="ArgumentException">Thrown when file path is null or whitespace.</exception>
        /// <exception cref="InvalidDataException">Thrown when the file does not contain a valid profile.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the imported profile could not be saved.</exception>
        public ProfileItemModel Import(string filePath)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
            var document = JsonSerializer.Deserialize<ProfileDocument>(File.ReadAllText(filePath))
                ?? throw new InvalidDataException("The selected file does not contain a profile.");
            if (document.FormatVersion != CurrentFormatVersion)
                throw new InvalidDataException($"Unsupported profile format version: {document.FormatVersion}.");
            if (string.IsNullOrWhiteSpace(document.Name))
                throw new InvalidDataException("The imported profile has no name.");

            var profile = new ProfileItemModel(_idFactory.CreateId(), $"{document.Name} (imported)");
            if (Enum.TryParse(document.ConflictHandling, out ProfileItemModel.ConflictHandlingMode conflictHandling))
                profile.ConflictHandling = conflictHandling;

            foreach (var bridgeDocument in document.Bridges ?? [])
            {
                var source = string.IsNullOrWhiteSpace(bridgeDocument.SourcePath)
                    ? null
                    : _folderFactory.Create(bridgeDocument.SourcePath, string.Empty, string.Empty);
                var bridge = _bridgeFactory.Create(bridgeDocument.Name ?? "Imported bridge", source, null);
                bridge.MoveSourceAfterCopy = bridgeDocument.MoveSourceAfterCopy;

                foreach (var routeDocument in bridgeDocument.Routes ?? [])
                {
                    if (!Enum.TryParse(routeDocument.PathMode, out FilterRuleModel.DestinationPathMode pathMode))
                        throw new InvalidDataException($"Route '{routeDocument.Name}' has an unsupported destination mode.");
                    var route = new FilterRuleModel(_idFactory.CreateId(), routeDocument.Name ?? "Imported route")
                    {
                        DestinationPath = routeDocument.DestinationPath ?? string.Empty,
                        PathMode = pathMode,
                        DateFolderPattern = routeDocument.DateFolderPattern ?? "yyyy-MM-dd",
                        UseCreationTime = routeDocument.UseCreationTime
                    };

                    foreach (var criteriaDocument in routeDocument.Criteria ?? [])
                        route.AddRuleCriteria(ImportCriteria(criteriaDocument));
                    bridge.AddRule(route);
                }
                profile.AddFolderBridge(bridge);
            }

            if (!_databaseService.SaveDataToProfilesTable(profile))
                throw new InvalidOperationException("The imported profile could not be saved.");
            foreach (var bridge in profile.GetFolderBridges())
                if (bridge.SourceFolder != null)
                    _databaseService.SaveFolder(bridge.SourceFolder);
            _databaseService.SaveProfileBridges(profile.Id, profile.GetFolderBridges());
            foreach (var bridge in profile.GetFolderBridges())
                foreach (var route in bridge.Routes)
                    _databaseService.SaveBridgeRoute(profile.Id, bridge, route);
            return profile;
        }

        /// <summary>
        /// Exports the given criteria to a <see cref="ProfileCriteriaDocument"/>.
        /// </summary>
        /// <param name="criteria">The criteria to export.</param>
        /// <returns>The exported criteria.</returns>
        /// <exception cref="InvalidDataException">Thrown when the criteria type is unsupported.</exception>
        private ProfileCriteriaDocument ExportCriteria(FilterRuleCriteriaBaseModel criteria) => criteria switch
        {
            FilterRuleCriteriaModelNone => new() { Type = "None" },
            FilterRuleCriteriaModelExtension extension => new() { Type = "Extension", Extensions = extension.Extensions },
            FilterRuleCriteriaModelFileName fileName => new() { Type = "FileName", Pattern = fileName.Pattern, UseRegex = fileName.UseRegex },
            FilterRuleCriteriaModelMinDate minDate => new() { Type = "MinDate", Date = minDate.MinDate, UseCreationTime = minDate.UseCreationTime },
            FilterRuleCriteriaModelMaxDate maxDate => new() { Type = "MaxDate", Date = maxDate.MaxDate, UseCreationTime = maxDate.UseCreationTime },
            _ => throw new InvalidDataException($"Unsupported criteria type: {criteria.GetType().Name}.")
        };

        /// <summary>
        /// Imports the given criteria from a <see cref="ProfileCriteriaDocument"/>.
        /// </summary>
        /// <param name="criteria">The criteria document to import.</param>
        /// <returns>The imported criteria.</returns>
        /// <exception cref="InvalidDataException">Thrown when the criterion type is unsupported or when date information is missing for date criteria.</exception>
        private FilterRuleCriteriaBaseModel ImportCriteria(ProfileCriteriaDocument criteria) => criteria.Type switch
        {
            "None" => _criteriaFactory.CreateNoFilter(),
            "Extension" => _criteriaFactory.CreateExtensionFilter(criteria.Extensions ?? string.Empty),
            "FileName" => _criteriaFactory.CreateFileNameFilter(criteria.Pattern ?? string.Empty, criteria.UseRegex),
            "MinDate" => _criteriaFactory.CreateMinDateFilter(criteria.Date ?? throw new InvalidDataException("Minimum date criterion has no date."), criteria.UseCreationTime),
            "MaxDate" => _criteriaFactory.CreateMaxDateFilter(criteria.Date ?? throw new InvalidDataException("Maximum date criterion has no date."), criteria.UseCreationTime),
            _ => throw new InvalidDataException($"Unsupported criterion type: {criteria.Type}.")
        };

        private sealed class ProfileDocument
        {
            public int FormatVersion { get; set; }
            public string? Name { get; set; }
            public string? ConflictHandling { get; set; }
            public List<BridgeDocument>? Bridges { get; set; }
        }

        private sealed class BridgeDocument
        {
            public string? Name { get; set; }
            public string? SourcePath { get; set; }
            public bool MoveSourceAfterCopy { get; set; }
            public List<RouteDocument>? Routes { get; set; }
        }

        private sealed class RouteDocument
        {
            public string? Name { get; set; }
            public string? DestinationPath { get; set; }
            public string? PathMode { get; set; }
            public string? DateFolderPattern { get; set; }
            public bool UseCreationTime { get; set; }
            public List<ProfileCriteriaDocument>? Criteria { get; set; }
        }

        private sealed class ProfileCriteriaDocument
        {
            public string Type { get; set; } = string.Empty;
            public string? Extensions { get; set; }
            public string? Pattern { get; set; }
            public bool UseRegex { get; set; }
            public DateTime? Date { get; set; }
            public bool UseCreationTime { get; set; }
        }
    }
}
