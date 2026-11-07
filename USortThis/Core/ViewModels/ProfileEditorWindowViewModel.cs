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
    /// <summary>
    /// ViewModel for editing profiles in the application.
    /// </summary>
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

        /// <summary>
        /// Command to save the profile.
        /// </summary>
        public ICommand SaveCommand { get; }
        /// <summary>
        /// Command to add a new rule.
        /// </summary>
        public ICommand AddRuleCommand { get; }
        /// <summary>
        /// Command to remove an existing rule.
        /// </summary>
        public ICommand RemoveRuleCommand { get; }
        /// <summary>
        /// Command to add new criteria to the selected rule.
        /// </summary>
        public ICommand AddCriteriaCommand { get; private set; }
        /// <summary>
        /// Command to remove criteria from the selected rule.
        /// </summary>
        public ICommand RemoveCriteriaCommand { get; private set; }
        /// <summary>
        /// Command to cancel editing and close the editor.
        /// </summary>
        public ICommand CancelCommand { get; }
        /// <summary>
        /// Command to add a new bridge.
        /// </summary>
        public ICommand AddBridgeCommand { get; }
        /// <summary>
        /// Command to remove an existing bridge.
        /// </summary>
        public ICommand RemoveBridgeCommand { get; }
        /// <summary>
        /// Command to add a route to the selected bridge.
        /// </summary>
        public ICommand AddRouteCommand { get; }
        /// <summary>
        /// Command to browse and select a source folder.
        /// </summary>
        public ICommand BrowseSourceCommand { get; }
        /// <summary>
        /// Command to browse and select a destination folder.
        /// </summary>
        public ICommand BrowseDestinationCommand { get; }
        /// <summary>
        /// Command to test sample files against the profile rules.
        /// </summary>
        public ICommand TestSampleFilesCommand { get; }

        // Keep references to RelayCommands so we can call RaiseCanExecuteChanged
        private RelayCommand? _addCriteriaRelayCommand;
        private RelayCommand? _removeCriteriaRelayCommand;
        private RelayCommand? _removeRuleRelayCommand;
        private RelayCommand? _removeBridgeRelayCommand;
        private RelayCommand? _addRouteRelayCommand;
        private RelayCommand? _browseSourceRelayCommand;
        private RelayCommand? _browseDestinationRelayCommand;

        /// <summary>
        /// Event triggered when the editor requests to close.
        /// </summary>
        public event EventHandler? RequestClose;

        // Rules for selected profile
        /// <summary>
        /// Collection of rules for the selected profile.
        /// </summary>
        public ObservableCollection<FilterRuleModel> Rules { get; } = new ObservableCollection<FilterRuleModel>();
        /// <summary>
        /// Bridges configured for the profile.
        /// </summary>
        public ObservableCollection<FolderBridgeModel> Bridges { get; } = [];
        /// <summary>
        /// Locations configured in the profile.
        /// </summary>
        public ObservableCollection<ConfiguredLocationModel> ConfiguredLocations { get; } = [];
        /// <summary>
        /// Validation errors found in the profile configuration.
        /// </summary>
        public ObservableCollection<string> ValidationErrors => _validationErrors;
        /// <summary>
        /// Validation warnings found in the profile configuration.
        /// </summary>
        public ObservableCollection<string> ValidationWarnings => _validationWarnings;
        /// <summary>
        /// Available options for handling conflicts when file exists in the destination.
        /// </summary>
        public IReadOnlyList<ConflictHandlingOption> ConflictHandlingOptions { get; } =
        [
            new(ProfileItemModel.ConflictHandlingMode.Skip, "Skip"),
            new(ProfileItemModel.ConflictHandlingMode.Overwrite, "Overwrite"),
            new(ProfileItemModel.ConflictHandlingMode.KeepBoth, "Keep both"),
            new(ProfileItemModel.ConflictHandlingMode.AskEachTime, "Ask each time")
        ];

        /// <summary>
        /// Conflict handling mode for the profile.
        /// </summary>
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
        /// <summary>
        /// Selected source location for the profile.
        /// </summary>
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
        /// <summary>
        /// Selected destination location for the profile.
        /// </summary>
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
        /// <summary>
        /// Selected bridge for the profile.
        /// </summary>
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

        /// <summary>
        /// Custom name for the selected bridge.
        /// </summary>
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

        /// <summary>
        /// Source folder path for the selected bridge.
        /// </summary>
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

        /// <summary>
        /// Indicates if files should be moved after copying in the source bridge.
        /// </summary>
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

        /// <summary>
        /// Name of the profile being edited or created.
        /// </summary>
        public string ProfileName
        {
            get => _profileName;
            set
            {
                if (SetProperty(ref _profileName, value))
                    _saveRelayCommand?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Title displayed in the editor window.
        /// </summary>
        public string EditorTitle => _isNewProfile ? "Create Profile" : "Edit Profile";

        // Criteria for the selected rule
        /// <summary>
        /// Collection of criteria for the selected rule.
        /// </summary>
        public ObservableCollection<FilterRuleCriteriaViewModel> RuleCriteria { get; } = new ObservableCollection<FilterRuleCriteriaViewModel>();

        private FilterRuleModel? _selectedRule;
        /// <summary>
        /// Selected rule for the profile.
        /// </summary>
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

        /// <summary>
        /// Warning message for the selected rule criteria.
        /// </summary>
        public string SelectedRuleCriteriaWarning => SelectedRule?.Criterias.Count == 0
            ? "Add at least one criterion to this route. Routes without criteria do not copy files."
            : string.Empty;

        /// <summary>
        /// Validates the rules and bridges in the profile, updating validation errors and warnings.
        /// </summary>
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
        /// <summary>
        /// Selected criteria for the rule.
        /// </summary>
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
        /// <summary>
        /// Available types of criteria that can be added to a rule.
        /// </summary>
        public IEnumerable<string> AvailableCriteriaTypes => CriteriaTypes;

        private string _selectedCriteriaType = "Extension";
        /// <summary>
        /// Currently selected type of criteria for addition to a rule.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileEditorWindowViewModel"/> class.
        /// </summary>
        /// <param name="profileListModel">The profile list model.</param>
        /// <param name="profile">The profile item model, can be null for a new profile.</param>
        /// <param name="localizationService">The localization service.</param>
        /// <param name="fileItemFactory">The file item model factory.</param>
        /// <param name="ruleTestInteractionService">The rule test interaction service.</param>
        /// <exception cref="ArgumentNullException">Thrown when required services are null.</exception>
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

        /// <summary>
        /// Opens a file dialog to select sample files and tests them against the profile rules.
        /// </summary>
        private void OnTestSampleFiles()
        {
            var filePaths = _ruleTestInteractionService.SelectSampleFiles();
            if (filePaths == null || filePaths.Count == 0)
                return;

            _ruleTestInteractionService.ShowResults(EvaluateSampleFiles(filePaths));
        }

        /// <summary>
        /// Evaluates the selected sample files against the profile rules.
        /// </summary>
        /// <param name="filePaths">The file paths of the samples to test.</param>
        /// <returns>A list of rule test results.</returns>
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

        /// <summary>
        /// Saves the profile, including its rules and bridges, to the database.
        /// </summary>
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

        /// <summary>
        /// Adds a new bridge to the profile.
        /// </summary>
        public void AddBridge()
        {
            if (_bridgeFactory == null) return;
            var bridge = _bridgeFactory.Create("New Bridge", null, null);
            Bridges.Add(bridge);
            SelectedBridge = bridge;
            _removeBridgeRelayCommand?.RaiseCanExecuteChanged();
            _addRouteRelayCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Removes the selected bridge from the profile.
        /// </summary>
        public void RemoveBridge()
        {
            if (SelectedBridge == null) return;
            var index = Bridges.IndexOf(SelectedBridge);
            Bridges.Remove(SelectedBridge);
            SelectedBridge = Bridges.Count == 0 ? null : Bridges[Math.Max(0, index - 1)];
            _removeBridgeRelayCommand?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Adds a new route to the selected bridge.
        /// </summary>
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
        /// <summary>
        /// Adds a new rule to the profile.
        /// </summary>
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

        /// <summary>
        /// Removes the selected rule from the profile.
        /// </summary>
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

        /// <summary>
        /// Adds a criteria to the selected rule.
        /// </summary>
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

        /// <summary>
        /// Removes the selected criteria from the rule.
        /// </summary>
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

        /// <summary>
        /// Closes the editor, discarding any unsaved changes.
        /// </summary>
        public void Close()
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
    /// <summary>
    /// Represents a conflict handling option for file operations.
    /// </summary>
    public sealed record ConflictHandlingOption(ProfileItemModel.ConflictHandlingMode Mode, string Label);
}
