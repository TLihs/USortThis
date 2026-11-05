using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using USortThis.Core.Commands;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    public class ProfileEditorWindowViewModel : ViewModelBase
    {
        private readonly ProfileListModel _profileListModel;
        private readonly ProfileItemModel _profile;
        private readonly bool _isNewProfile;
        private readonly IFolderBridgeModelFactory? _bridgeFactory;
        private readonly IFolderItemModelFactory? _folderFactory;
        private readonly IFileItemModelFactory _fileItemFactory;
        private readonly IRuleTestInteractionService _ruleTestInteractionService;
        private readonly IFilterRuleCriteriaFactory? _criteriaFactory;
        private readonly IFilterRuleCriteriaViewModelFactory? _criteriaVmFactory;
        private readonly HashSet<long> _originalRuleIds = [];
        private string _profileName;
        private RelayCommand? _saveRelayCommand;
        private readonly ObservableCollection<string> _validationErrors = [];
        private readonly ObservableCollection<string> _validationWarnings = [];

        public ICommand SaveCommand { get; }
        public ICommand AddRuleCommand { get; }
        public ICommand RemoveRuleCommand { get; }
        public ICommand AddCriteriaCommand { get; private set; }
        public ICommand RemoveCriteriaCommand { get; private set; }
        public ICommand CancelCommand { get; }
        public ICommand AddBridgeCommand { get; }
        public ICommand RemoveBridgeCommand { get; }
        public ICommand AddRouteCommand { get; }
        public ICommand BrowseSourceCommand { get; }
        public ICommand BrowseDestinationCommand { get; }
        public ICommand TestSampleFilesCommand { get; }

        // Keep references to RelayCommands so we can call RaiseCanExecuteChanged
        private RelayCommand? _addCriteriaRelayCommand;
        private RelayCommand? _removeCriteriaRelayCommand;
        private RelayCommand? _removeRuleRelayCommand;
        private RelayCommand? _removeBridgeRelayCommand;
        private RelayCommand? _addRouteRelayCommand;
        private RelayCommand? _browseSourceRelayCommand;
        private RelayCommand? _browseDestinationRelayCommand;

        public event EventHandler? RequestClose;

        // Rules for selected profile
        public ObservableCollection<FilterRuleModel> Rules { get; } = new ObservableCollection<FilterRuleModel>();
        public ObservableCollection<FolderBridgeModel> Bridges { get; } = [];
        public ObservableCollection<ConfiguredLocationModel> ConfiguredLocations { get; } = [];
        public ObservableCollection<string> ValidationErrors => _validationErrors;
        public ObservableCollection<string> ValidationWarnings => _validationWarnings;
        public IReadOnlyList<ConflictHandlingOption> ConflictHandlingOptions { get; } =
        [
            new(ProfileItemModel.ConflictHandlingMode.Skip, "Skip"),
            new(ProfileItemModel.ConflictHandlingMode.Overwrite, "Overwrite"),
            new(ProfileItemModel.ConflictHandlingMode.KeepBoth, "Keep both"),
            new(ProfileItemModel.ConflictHandlingMode.AskEachTime, "Ask each time")
        ];

        public ProfileItemModel.ConflictHandlingMode ConflictHandling
        {
            get => _profile.ConflictHandling;
            set
            {
                if (_profile.ConflictHandling == value) return;
                _profile.ConflictHandling = value;
                OnPropertyChanged();
            }
        }

        private ConfiguredLocationModel? _selectedSourceLocation;
        public ConfiguredLocationModel? SelectedSourceLocation
        {
            get => _selectedSourceLocation;
            set
            {
                if (_selectedSourceLocation == value) return;
                _selectedSourceLocation = value;
                OnPropertyChanged();
                if (value != null) SourcePath = value.Path;
            }
        }

        private ConfiguredLocationModel? _selectedDestinationLocation;
        public ConfiguredLocationModel? SelectedDestinationLocation
        {
            get => _selectedDestinationLocation;
            set
            {
                if (_selectedDestinationLocation == value) return;
                _selectedDestinationLocation = value;
                OnPropertyChanged();
                if (value != null && SelectedRule != null) SelectedRule.DestinationPath = value.Path;
            }
        }

        private FolderBridgeModel? _selectedBridge;
        public FolderBridgeModel? SelectedBridge
        {
            get => _selectedBridge;
            set
            {
                if (_selectedBridge == value) return;
                _selectedBridge = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(BridgeName));
                OnPropertyChanged(nameof(SourcePath));
                OnPropertyChanged(nameof(MoveSourceAfterCopy));
                Rules.Clear();
                if (value != null)
                {
                    foreach (var route in value.Routes)
                    {
                        Rules.Add(route);
                    }
                }
                SelectedRule = null;
                _removeBridgeRelayCommand?.RaiseCanExecuteChanged();
                _addRouteRelayCommand?.RaiseCanExecuteChanged();
                _browseSourceRelayCommand?.RaiseCanExecuteChanged();
                _browseDestinationRelayCommand?.RaiseCanExecuteChanged();
                ValidateRules();
            }
        }

        public string BridgeName
        {
            get => SelectedBridge?.CustomName ?? string.Empty;
            set
            {
                if (SelectedBridge != null && SelectedBridge.CustomName != value)
                {
                    SelectedBridge.CustomName = value;
                    OnPropertyChanged();
                }
            }
        }

        public string SourcePath
        {
            get => SelectedBridge?.SourceFolder?.FolderPath ?? string.Empty;
            set
            {
                if (SelectedBridge == null || _folderFactory == null) return;
                var currentPath = SelectedBridge.SourceFolder?.FolderPath ?? string.Empty;
                if (currentPath == value) return;
                SelectedBridge.SourceFolder = string.IsNullOrWhiteSpace(value)
                    ? null
                    : _folderFactory.Create(value, string.Empty, string.Empty);
                OnPropertyChanged();
            }
        }

        public bool MoveSourceAfterCopy
        {
            get => SelectedBridge?.MoveSourceAfterCopy ?? false;
            set
            {
                if (SelectedBridge != null && SelectedBridge.MoveSourceAfterCopy != value)
                {
                    SelectedBridge.MoveSourceAfterCopy = value;
                    OnPropertyChanged();
                }
            }
        }

        public string ProfileName
        {
            get => _profileName;
            set
            {
                if (SetProperty(ref _profileName, value))
                    _saveRelayCommand?.RaiseCanExecuteChanged();
            }
        }

        public string EditorTitle => _isNewProfile ? "Create Profile" : "Edit Profile";

        // Criteria for the selected rule
        public ObservableCollection<FilterRuleCriteriaViewModel> RuleCriteria { get; } = new ObservableCollection<FilterRuleCriteriaViewModel>();

        private FilterRuleModel? _selectedRule;
        public FilterRuleModel? SelectedRule
        {
            get => _selectedRule;
            set
            {
                Debug.WriteLine($"SelectedRule setter called: old={_selectedRule?.Name}, new={value?.Name}");
                if (_selectedRule != value)
                {
                    _selectedRule = value;
                    OnPropertyChanged(nameof(SelectedRule));
                    OnPropertyChanged(nameof(SelectedRuleCriteriaWarning));
                    ValidateRules();
                    LoadCriteriaForSelectedRule();

                    // Notify commands that their CanExecute state may have changed
                    _addCriteriaRelayCommand?.RaiseCanExecuteChanged();
                    _removeRuleRelayCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        public string SelectedRuleCriteriaWarning => SelectedRule?.Criterias.Count == 0
            ? "Add at least one criterion to this route. Routes without criteria do not copy files."
            : string.Empty;

        public void ValidateRules()
        {
            _validationErrors.Clear();
            _validationWarnings.Clear();

            foreach (var bridge in Bridges)
            {
                if (bridge.Routes.Count == 0)
                    _validationErrors.Add($"{bridge.CustomName}: add at least one route.");

                foreach (var route in bridge.Routes)
                {
                    if (route.Criterias.Count == 0)
                        _validationErrors.Add($"{bridge.CustomName} / {route.Name}: add at least one criterion; this route currently matches no files.");

                    if (string.IsNullOrWhiteSpace(route.DestinationPath))
                        _validationErrors.Add($"{bridge.CustomName} / {route.Name}: destination folder is required.");
                    else if (!System.IO.Path.IsPathRooted(route.DestinationPath))
                        _validationErrors.Add($"{bridge.CustomName} / {route.Name}: destination must be an absolute path.");

                    if (route.PathMode == FilterRuleModel.DestinationPathMode.DateFolder)
                    {
                        try
                        {
                            if (string.IsNullOrWhiteSpace(route.DateFolderPattern))
                                throw new FormatException();
                            _ = DateTime.Now.ToString(route.DateFolderPattern);
                        }
                        catch (FormatException)
                        {
                            _validationErrors.Add($"{bridge.CustomName} / {route.Name}: date folder pattern is invalid.");
                        }
                    }

                    foreach (var criteria in route.Criterias)
                    {
                        switch (criteria)
                        {
                            case FilterRuleCriteriaModelFileName { UseRegex: true } fileNameCriteria:
                                try { _ = new Regex(fileNameCriteria.Pattern); }
                                catch (ArgumentException)
                                {
                                    _validationErrors.Add($"{bridge.CustomName} / {route.Name}: file-name regular expression is invalid.");
                                }
                                break;
                            case FilterRuleCriteriaModelMinDate minDateCriteria:
                                var maxDate = route.Criterias.OfType<FilterRuleCriteriaModelMaxDate>()
                                    .FirstOrDefault(candidate => candidate.UseCreationTime == minDateCriteria.UseCreationTime);
                                if (maxDate != null && minDateCriteria.MinDate > maxDate.MaxDate)
                                    _validationErrors.Add($"{bridge.CustomName} / {route.Name}: minimum date is later than maximum date.");
                                break;
                            case FilterRuleCriteriaModelExtension { Extensions: var extensions } when string.IsNullOrWhiteSpace(extensions):
                                _validationWarnings.Add($"{bridge.CustomName} / {route.Name}: empty extension criteria do not restrict matching.");
                                break;
                            case FilterRuleCriteriaModelNone when route.Criterias.Count == 1:
                                _validationWarnings.Add($"{bridge.CustomName} / {route.Name}: 'None' criteria match every file.");
                                break;
                        }
                    }
                }

                foreach (var duplicateGroup in bridge.Routes
                    .Where(route => route.Criterias.Count == 1 && route.Criterias[0] is FilterRuleCriteriaModelFileName { UseRegex: false })
                    .GroupBy(route => ((FilterRuleCriteriaModelFileName)route.Criterias[0]).Pattern, StringComparer.OrdinalIgnoreCase)
                    .Where(group => !string.IsNullOrWhiteSpace(group.Key) && group.Count() > 1))
                {
                    _validationWarnings.Add($"{bridge.CustomName}: multiple routes match the exact file-name pattern '{duplicateGroup.Key}'. They may both process the same files.");
                }
            }
        }

        private void AttachBridgeValidation(FolderBridgeModel bridge)
        {
            foreach (var route in bridge.Routes)
                AttachRouteValidation(route);
            bridge.Routes.CollectionChanged += (_, args) =>
            {
                if (args.NewItems != null)
                    foreach (FilterRuleModel route in args.NewItems)
                        AttachRouteValidation(route);
                ValidateRules();
            };
        }

        private void AttachRouteValidation(FilterRuleModel route)
        {
            route.PropertyChanged += (_, _) => ValidateRules();
            route.ItemAdded += (_, args) =>
            {
                args.Item.PropertyChanged += (_, _) => ValidateRules();
                ValidateRules();
            };
            route.ItemRemoved += (_, _) => ValidateRules();
            foreach (var criteria in route.Criterias)
                criteria.PropertyChanged += (_, _) => ValidateRules();
        }

        private FilterRuleCriteriaViewModel? _selectedCriteria;
        public FilterRuleCriteriaViewModel? SelectedCriteria
        {
            get => _selectedCriteria;
            set
            {
                Debug.WriteLine($"SelectedCriteria setter called: old={_selectedCriteria?.Model.GetType().Name}, new={value?.Model.GetType().Name}");
                if (_selectedCriteria != value)
                {
                    _selectedCriteria = value;
                    OnPropertyChanged(nameof(SelectedCriteria));

                    // Notify commands that their CanExecute state may have changed
                    _removeCriteriaRelayCommand?.RaiseCanExecuteChanged();
                }
            }
        }

        // Available criteria types for the add dropdown
        private static readonly string[] CriteriaTypes = ["None", "Extension", "FileName", "MinDate", "MaxDate"];
        public IEnumerable<string> AvailableCriteriaTypes => CriteriaTypes;

        private string _selectedCriteriaType = "Extension";
        public string SelectedCriteriaType
        {
            get => _selectedCriteriaType;
            set
            {
                if (_selectedCriteriaType != value)
                {
                    _selectedCriteriaType = value;
                    OnPropertyChanged(nameof(SelectedCriteriaType));
                }
            }
        }

        public ProfileEditorWindowViewModel(
            ProfileListModel profileListModel,
            ProfileItemModel? profile,
            ILocalizationService localizationService,
            IFileItemModelFactory fileItemFactory,
            IRuleTestInteractionService ruleTestInteractionService)
            : base(localizationService)
        {
            ArgumentNullException.ThrowIfNull(profileListModel);
            _fileItemFactory = fileItemFactory ?? throw new ArgumentNullException(nameof(fileItemFactory));
            _ruleTestInteractionService = ruleTestInteractionService ?? throw new ArgumentNullException(nameof(ruleTestInteractionService));
            _profileListModel = profileListModel;
            _isNewProfile = profile == null;
            _profile = profile ?? new ProfileItemModel(
                (App.ServiceProvider?.GetService(typeof(IIdFactory)) as IIdFactory)?.CreateId() ?? DateTime.UtcNow.Ticks,
                string.Empty);
            _profileName = _profile.Name;

            // Get factories from service provider
            _criteriaFactory = App.ServiceProvider?.GetService(typeof(IFilterRuleCriteriaFactory)) as IFilterRuleCriteriaFactory;
            _criteriaVmFactory = App.ServiceProvider?.GetService(typeof(IFilterRuleCriteriaViewModelFactory)) as IFilterRuleCriteriaViewModelFactory;
            _bridgeFactory = App.ServiceProvider?.GetService(typeof(IFolderBridgeModelFactory)) as IFolderBridgeModelFactory;
            _folderFactory = App.ServiceProvider?.GetService(typeof(IFolderItemModelFactory)) as IFolderItemModelFactory;
            if (App.ServiceProvider?.GetService(typeof(IDatabaseService)) is IDatabaseService databaseService)
                foreach (var location in databaseService.LoadConfiguredLocations())
                    ConfiguredLocations.Add(location);

            _saveRelayCommand = new RelayCommand(_ => Save(), _ => !string.IsNullOrWhiteSpace(ProfileName));
            SaveCommand = _saveRelayCommand;
            AddRuleCommand = new RelayCommand(_ => AddRule(), _ => true);

            _removeRuleRelayCommand = new RelayCommand(_ => RemoveRule(), _ => SelectedRule != null);
            RemoveRuleCommand = _removeRuleRelayCommand;

            _addCriteriaRelayCommand = new RelayCommand(_ => AddCriteria(), _ => SelectedRule != null);
            AddCriteriaCommand = _addCriteriaRelayCommand;

            _removeCriteriaRelayCommand = new RelayCommand(_ => RemoveCriteria(), _ => SelectedCriteria != null);
            RemoveCriteriaCommand = _removeCriteriaRelayCommand;

            CancelCommand = new RelayCommand(_ => Close(), _ => true);
            AddBridgeCommand = new RelayCommand(_ => AddBridge(), _ => _bridgeFactory != null);
            _removeBridgeRelayCommand = new RelayCommand(_ => RemoveBridge(), _ => SelectedBridge != null);
            RemoveBridgeCommand = _removeBridgeRelayCommand;
            _addRouteRelayCommand = new RelayCommand(_ => AddRoute(), _ => SelectedBridge != null);
            AddRouteCommand = _addRouteRelayCommand;
            _browseSourceRelayCommand = new RelayCommand(_ => BrowseSource(), _ => SelectedBridge != null);
            BrowseSourceCommand = _browseSourceRelayCommand;
            _browseDestinationRelayCommand = new RelayCommand(_ => BrowseDestination(), _ => SelectedRule != null);
            BrowseDestinationCommand = _browseDestinationRelayCommand;
            TestSampleFilesCommand = new RelayCommand(_ => OnTestSampleFiles());

            foreach (var bridge in _profile.GetFolderBridges())
            {
                var copy = _bridgeFactory?.Create(bridge.CustomName, CloneFolder(bridge.SourceFolder), null);
                if (copy != null)
                {
                    copy.MoveSourceAfterCopy = bridge.MoveSourceAfterCopy;
                    Bridges.Add(copy);
                }
            }
            foreach (var bridge in Bridges)
                AttachBridgeValidation(bridge);
            SelectedBridge = Bridges.FirstOrDefault();
            ValidateRules();

            if (!_isNewProfile && App.ServiceProvider?.GetService(typeof(IDatabaseService)) is IDatabaseService db)
            {
                Bridges.Clear();
                foreach (var bridge in db.LoadBridgesForProfile(_profile.Id))
                {
                    Bridges.Add(bridge);
                    AttachBridgeValidation(bridge);
                }
                foreach (var bridge in Bridges)
                    foreach (var route in bridge.Routes)
                        _originalRuleIds.Add(route.Id);
                SelectedBridge = Bridges.FirstOrDefault();
            }
        }

        private void OnTestSampleFiles()
        {
            var filePaths = _ruleTestInteractionService.SelectSampleFiles();
            if (filePaths == null || filePaths.Count == 0)
                return;

            _ruleTestInteractionService.ShowResults(EvaluateSampleFiles(filePaths));
        }

        private IReadOnlyList<RuleTestResult> EvaluateSampleFiles(IEnumerable<string> filePaths)
        {
            var results = new List<RuleTestResult>();

            foreach (var filePath in filePaths)
            {
                try
                {
                    var file = _fileItemFactory.Create(filePath);
                    var matchingRoutes = Bridges
                        .SelectMany(bridge => bridge.Routes
                            .Where(route => route.Matches(file))
                            .Select(route => $"{bridge.CustomName} / {route.Name}"))
                        .ToList();

                    results.Add(new RuleTestResult(
                        filePath,
                        matchingRoutes.Count == 0 ? "No match" : "Matched",
                        matchingRoutes.Count == 0 ? "No configured route matched this file." : string.Join("; ", matchingRoutes)));
                }
                catch (Exception exception)
                {
                    results.Add(new RuleTestResult(filePath, "Error", exception.Message));
                }
            }

            return results;
        }

        public void Save()
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
                return;

            ValidateRules();
            if (_validationErrors.Count > 0)
            {
                System.Windows.MessageBox.Show(
                    string.Join(Environment.NewLine, _validationErrors),
                    "Rule Validation Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (App.ServiceProvider?.GetService(typeof(IDatabaseService)) is not IDatabaseService db)
                    throw new InvalidOperationException("Database service is unavailable.");

                _profile.Name = ProfileName.Trim();
                if (!db.SaveDataToProfilesTable(_profile))
                    throw new InvalidOperationException("The profile could not be saved.");

                var currentRoutes = Bridges.SelectMany(bridge => bridge.Routes).ToList();
                foreach (var ruleId in _originalRuleIds.Except(currentRoutes.Select(route => route.Id)))
                    db.DeleteRule(ruleId.ToString());

                foreach (var bridge in Bridges)
                {
                    if (bridge.SourceFolder != null) db.SaveFolder(bridge.SourceFolder);
                }
                db.SaveProfileBridges(_profile.Id, Bridges);
                foreach (var bridge in Bridges)
                    foreach (var route in bridge.Routes)
                        db.SaveBridgeRoute(_profile.Id, bridge, route);

                _profile.ReplaceFolderBridges(Bridges);

                if (_isNewProfile)
                    _profileListModel.AddProfile(_profile);

                _profileListModel.SelectedProfile = _profile;
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                System.Windows.MessageBox.Show(
                    $"The profile could not be saved. {ex.Message}",
                    "Save Profile Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        private FolderItemModel? CloneFolder(FolderItemModel? folder) => folder == null || _folderFactory == null
            ? null
            : _folderFactory.Create(folder.FolderPath, folder.CustomName, folder.CustomDescription);

        public void AddBridge()
        {
            if (_bridgeFactory == null) return;
            var bridge = _bridgeFactory.Create("New Bridge", null, null);
            Bridges.Add(bridge);
            SelectedBridge = bridge;
            _removeBridgeRelayCommand?.RaiseCanExecuteChanged();
            _addRouteRelayCommand?.RaiseCanExecuteChanged();
        }

        public void RemoveBridge()
        {
            if (SelectedBridge == null) return;
            var index = Bridges.IndexOf(SelectedBridge);
            Bridges.Remove(SelectedBridge);
            SelectedBridge = Bridges.Count == 0 ? null : Bridges[Math.Max(0, index - 1)];
            _removeBridgeRelayCommand?.RaiseCanExecuteChanged();
        }

        public void AddRoute()
        {
            if (SelectedBridge == null) return;
            var idFactory = App.ServiceProvider?.GetService(typeof(IIdFactory)) as IIdFactory;
            var route = new FilterRuleModel(idFactory?.CreateId() ?? DateTime.UtcNow.Ticks, "New Route");
            SelectedBridge.AddRule(route);
            Rules.Add(route);
            SelectedRule = route;
        }

        private void BrowseSource()
        {
            if (SelectedBridge == null) return;
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a source folder",
                ShowNewFolderButton = false
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SourcePath = dialog.SelectedPath;
        }

        private void BrowseDestination()
        {
            if (SelectedRule == null) return;
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Select a destination folder",
                ShowNewFolderButton = true
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                SelectedRule.DestinationPath = dialog.SelectedPath;
        }
        public void AddRule()
        {
            try
            {
                var idFactory = App.ServiceProvider?.GetService(typeof(IIdFactory)) as IIdFactory;
                long id = idFactory?.CreateId() ?? DateTime.UtcNow.Ticks;
                var rule = new FilterRuleModel(id, "New Rule");
                Rules.Add(rule);
                SelectedRule = rule;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void RemoveRule()
        {
            if (SelectedRule == null)
                return;

            var toRemove = SelectedRule;
            SelectedBridge?.RemoveRule(toRemove);
            Rules.Remove(toRemove);
            SelectedRule = null;
        }

        private void LoadCriteriaForSelectedRule()
        {
            RuleCriteria.Clear();
            if (SelectedRule == null) return;

            try
            {
                // Create ViewModels for each criteria in the selected rule
                foreach (var criteria in SelectedRule.Criterias)
                {
                    if (_criteriaVmFactory != null)
                    {
                        var vm = _criteriaVmFactory.Create(criteria);
                        RuleCriteria.Add(vm);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public void AddCriteria()
        {
            Debug.WriteLine($"AddCriteria called: SelectedRule={SelectedRule?.Name}, _criteriaFactory={_criteriaFactory != null}");
            if (SelectedRule == null || _criteriaFactory == null)
            {
                Debug.WriteLine("AddCriteria: SelectedRule or _criteriaFactory is null, returning");
                return;
            }

            try
            {
                Debug.WriteLine($"AddCriteria: Creating new criteria of type: {SelectedCriteriaType}");
                FilterRuleCriteriaBaseModel? newCriteria = SelectedCriteriaType switch
                {
                    "None" => _criteriaFactory.CreateNoFilter(),
                    "Extension" => _criteriaFactory.CreateExtensionFilter(""),
                    "FileName" => _criteriaFactory.CreateFileNameFilter("", false),
                    "MinDate" => _criteriaFactory.CreateMinDateFilter(DateTime.Now, false),
                    "MaxDate" => _criteriaFactory.CreateMaxDateFilter(DateTime.Now, false),
                    _ => null
                };

                if (newCriteria != null)
                {
                    Debug.WriteLine($"AddCriteria: Adding criteria to rule");
                    SelectedRule.AddRuleCriteria(newCriteria);

                    // Create ViewModel and add to UI
                    if (_criteriaVmFactory != null)
                    {
                        var vm = _criteriaVmFactory.Create(newCriteria);
                        RuleCriteria.Add(vm);
                        Debug.WriteLine($"AddCriteria: Criteria added to UI. Total criteria: {RuleCriteria.Count}");
                    }
                    OnPropertyChanged(nameof(SelectedRuleCriteriaWarning));
                }
                else
                {
                    Debug.WriteLine($"AddCriteria: newCriteria is null");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AddCriteria Exception: {ex.Message}");
            }
        }

        public void RemoveCriteria()
        {
            Debug.WriteLine($"RemoveCriteria called: SelectedRule={SelectedRule?.Name}, SelectedCriteria={SelectedCriteria?.Model.GetType().Name}");
            if (SelectedRule == null || SelectedCriteria == null)
            {
                Debug.WriteLine($"RemoveCriteria: SelectedRule or SelectedCriteria is null, returning");
                return;
            }

            try
            {
                var criteriaToRemove = SelectedCriteria.Model;
                Debug.WriteLine($"RemoveCriteria: Removing criteria from rule");
                SelectedRule.RemoveRuleCriteria(criteriaToRemove);
                RuleCriteria.Remove(SelectedCriteria);
                OnPropertyChanged(nameof(SelectedRuleCriteriaWarning));
                Debug.WriteLine($"RemoveCriteria: Criteria removed from UI. Total criteria: {RuleCriteria.Count}");
                SelectedCriteria = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"RemoveCriteria Exception: {ex.Message}");
            }
        }

        public void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
    public sealed record ConflictHandlingOption(ProfileItemModel.ConflictHandlingMode Mode, string Label);
}
