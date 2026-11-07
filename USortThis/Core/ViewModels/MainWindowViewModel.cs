using System;
using System.Windows.Input;
using System.Collections.ObjectModel;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.Core.Commands;
using USortThis.Core.Factories;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using USortThis.Core.Services;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// The main view model for the application, orchestrating profile management, sorting operations, and application commands.
    /// </summary>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly MainWindowModel _model;
        private readonly ProfileListModel _profileListModel;
        private readonly IDatabaseService _databaseService;
        private readonly ConfiguredLocationService _configuredLocationService;
        private ProfileItemModel? _observedProfile;
        private sealed class ProfileRun
        {
            public required CancellationTokenSource Cancellation { get; init; }
            public TaskCompletionSource<bool> Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        }
        private readonly object _profileRunsLock = new();
        private readonly Dictionary<ProfileItemModel, ProfileRun> _profileRuns = [];

        // ViewModels should not directly manipulate Views or Application lifecycle.
        // Expose an event the View can subscribe to when it needs to be closed.
        /// <summary>
        /// Occurs when the request to close the application is raised.
        /// </summary>
        public event EventHandler? RequestClose;

        /// <summary>
        /// The command to close the application.
        /// </summary>
        public ICommand CloseApplicationCommand { get; }
        /// <summary>
        /// The command to show the about dialog.
        /// </summary>
        public ICommand ShowAboutCommand { get; }

        /// <summary>
        /// The command to open the settings.
        /// </summary>
        public ICommand OpenSettingsCommand { get; }

        /// <summary>
        /// The command to create a new profile.
        /// </summary>
        public ICommand CreateNewProfileCommand { get; }
        /// <summary>
        /// The command to edit the selected profile.
        /// </summary>
        public ICommand EditProfileCommand { get; }
        /// <summary>
        /// The command to delete the selected profile.
        /// </summary>
        public ICommand DeleteProfileCommand { get; }
        /// <summary>
        /// The command to import a profile.
        /// </summary>
        public ICommand ImportProfileCommand { get; }
        /// <summary>
        /// The command to export the selected profile.
        /// </summary>
        public ICommand ExportProfileCommand { get; }
        /// <summary>
        /// The command to preview the selected profile.
        /// </summary>
        public ICommand PreviewProfileCommand { get; }
        /// <summary>
        /// The command to select a profile.
        /// </summary>
        public ICommand SelectProfileCommand { get; }

        // New commands for sorting
        /// <summary>
        /// The command to start sorting with the active profile.
        /// </summary>
        public ICommand StartSortCommand { get; }
        /// <summary>
        /// The command to stop sorting with the active profile.
        /// </summary>
        public ICommand StopSortCommand { get; }
        /// <summary>
        /// The command to start sorting a specific profile.
        /// </summary>
        public ICommand StartProfileSortCommand { get; }
        /// <summary>
        /// The command to stop sorting a specific profile.
        /// </summary>
        public ICommand StopProfileSortCommand { get; }
        /// <summary>
        /// The command to retry transferring a failed file.
        /// </summary>
        public ICommand RetryFailedTransferCommand { get; }

        private int _sortProgress;
        /// <summary>
        /// The progress of the current sorting operation, represented as a percentage (0-100).
        /// </summary>
        public int SortProgress
        {
            get => _sortProgress;
            private set => SetProperty(ref _sortProgress, value);
        }

        // New: collection for enumeration errors surfaced to the UI
        /// <summary>
        /// Collection of errors encountered during file enumeration, for display in the UI.
        /// </summary>
        public ObservableCollection<FileEnumerationError> EnumerationErrors { get; } = new ObservableCollection<FileEnumerationError>();
        /// <summary>
        /// Collection of folder bridge items for the active profile.
        /// </summary>
        public ObservableCollection<FolderBridgeModel> BridgeItems { get; } = [];
        /// <summary>
        /// Collection of location items for configured locations.
        /// </summary>
        public ObservableCollection<LocationListEntry> LocationItems { get; } = [];

        private FolderBridgeModel? _selectedBridge;
        /// <summary>
        /// The currently selected bridge item.
        /// </summary>
        public FolderBridgeModel? SelectedBridge
        {
            get => _selectedBridge;
            set
            {
                if (!SetProperty(ref _selectedBridge, value)) return;
                SelectedTransferDetail = null;
                (RetryFailedTransferCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        private FileTransferDetailModel? _selectedTransferDetail;
        /// <summary>
        /// The currently selected file transfer detail.
        /// </summary>
        public FileTransferDetailModel? SelectedTransferDetail
        {
            get => _selectedTransferDetail;
            set
            {
                if (_selectedTransferDetail == value) return;
                if (_selectedTransferDetail != null)
                    _selectedTransferDetail.PropertyChanged -= OnSelectedTransferDetailPropertyChanged;
                _selectedTransferDetail = value;
                if (_selectedTransferDetail != null)
                    _selectedTransferDetail.PropertyChanged += OnSelectedTransferDetailPropertyChanged;
                OnPropertyChanged();
                (RetryFailedTransferCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// The name of the active profile.
        /// </summary>
        public string ActiveProfile => _model.ActiveProfile?.Name ?? string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
        /// </summary>
        /// <param name="model">The main window model.</param>
        /// <param name="profileListModel">The profile list model.</param>
        /// <param name="databaseService">The database service.</param>
        /// <param name="configuredLocationService">The configured location service.</param>
        /// <param name="localizationService">The localization service.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the arguments are null.</exception>
        public MainWindowViewModel(MainWindowModel model, ProfileListModel profileListModel, IDatabaseService databaseService, ConfiguredLocationService configuredLocationService, ILocalizationService localizationService) : base(localizationService)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _profileListModel = profileListModel ?? throw new ArgumentNullException(nameof(profileListModel));
            _databaseService = databaseService ?? throw new ArgumentNullException(nameof(databaseService));
            _configuredLocationService = configuredLocationService ?? throw new ArgumentNullException(nameof(configuredLocationService));
            RefreshLocationItems();

            _model.PropertyChanged += (s, e) =>
            {
                SubscribePropertyChanged(e, nameof(ActiveProfile), nameof(MainWindowModel.ActiveProfile));
            };
            _profileListModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProfileListModel.SelectedProfile))
                {
                    _model.ActiveProfile = _profileListModel.SelectedProfile;
                    TrackActiveProfile();
                    RefreshBridgeItems();
                    OnPropertyChanged(nameof(ActiveProfile));
                    RaiseCanExecuteChanged();
                }
            };

            _model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowModel.ActiveProfile))
                {
                    TrackActiveProfile();
                    RefreshBridgeItems();
                }
            };

            _model.ActiveProfile = _profileListModel.SelectedProfile;
            TrackActiveProfile();
            RefreshBridgeItems();
            CloseApplicationCommand = new RelayCommand(_ => OnCloseApplication(), _ => true);
            ShowAboutCommand = new RelayCommand(_ => OnShowAbout(), _ => true);
            OpenSettingsCommand = new RelayCommand(_ => OnOpenSettings(), _ => true);
            CreateNewProfileCommand = new RelayCommand(_ => OnCreateNewProfile(), _ => true);
            EditProfileCommand = new RelayCommand(_ => OnEditProfile(), _ => _profileListModel.SelectedProfile != null);
            DeleteProfileCommand = new RelayCommand(_ => OnDeleteProfile(), _ => _profileListModel.SelectedProfile != null);
            ImportProfileCommand = new RelayCommand(_ => OnImportProfile());
            ExportProfileCommand = new RelayCommand(_ => OnExportProfile(), _ => _profileListModel.SelectedProfile != null);
            PreviewProfileCommand = new RelayCommand(async _ => await OnPreviewProfileAsync(), _ => _model.ActiveProfile != null && !_profileRuns.ContainsKey(_model.ActiveProfile));
            SelectProfileCommand = new RelayCommand(_ => OnSelectProfile(), _ => true);

            StartSortCommand = new RelayCommand(async _ => await OnStartSortAsync(), _ => _model.ActiveProfile != null && !_profileRuns.ContainsKey(_model.ActiveProfile));
            StopSortCommand = new RelayCommand(_ => OnStopSort(_model.ActiveProfile), _ => _model.ActiveProfile != null && _profileRuns.ContainsKey(_model.ActiveProfile));
            StartProfileSortCommand = new RelayCommand(async profile => await OnStartSortAsync(profile as ProfileItemModel),
                profile => profile is ProfileItemModel target && !_profileRuns.ContainsKey(target));
            StopProfileSortCommand = new RelayCommand(profile => OnStopSort(profile as ProfileItemModel),
                profile => profile is ProfileItemModel target && _profileRuns.ContainsKey(target));
            RetryFailedTransferCommand = new RelayCommand(async _ => await OnRetryFailedTransferAsync(SelectedTransferDetail),
                _ => SelectedTransferDetail is FileTransferDetailModel transfer &&
                    transfer.Status == FileItemModel.CopyStatus.Failed &&
                    _model.ActiveProfile is ProfileItemModel activeProfile &&
                    !_profileRuns.ContainsKey(activeProfile) &&
                    SelectedBridge != null &&
                    activeProfile.GetFolderBridges().Contains(SelectedBridge) &&
                    SelectedBridge.TransferDetails.Contains(transfer));
        }

        /// <summary>
        /// Previews the active profile's sorting behavior without making any changes.
        /// </summary>
        /// <remarks>
        /// Displays a modal window showing the files that would be sorted, along with any errors that would occur.
        /// </remarks>
        private async Task OnPreviewProfileAsync()
        {
            var profile = _model.ActiveProfile;
            if (profile == null || App.ServiceProvider is null)
                return;

            try
            {
                var sortingService = ActivatorUtilities.CreateInstance<SortingService>(App.ServiceProvider);
                var errors = new List<FileEnumerationError>();
                var errorsProgress = new Progress<FileEnumerationError>(error => errors.Add(error));
                var entries = await sortingService.PreviewAsync(profile, CancellationToken.None, errorsProgress);
                var window = new UI.Views.TransferPreviewWindow(profile, entries, errors)
                {
                    Owner = System.Windows.Application.Current?.MainWindow
                };
                window.ShowDialog();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"The dry-run preview could not be generated. {exception.Message}",
                    "Preview Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Imports a profile from a file.
        /// </summary>
        /// <remarks>
        /// Opens a file dialog to select a profile file, and imports the profile, making it the active profile.
        /// </remarks>
        private void OnImportProfile()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Import profile",
                Filter = "USortThis profile (*.usortprofile.json)|*.usortprofile.json|JSON files (*.json)|*.json",
                CheckFileExists = true
            };
            if (dialog.ShowDialog(System.Windows.Application.Current?.MainWindow) != true)
                return;

            try
            {
                if (App.ServiceProvider?.GetService(typeof(IIdFactory)) is not IIdFactory idFactory ||
                    App.ServiceProvider.GetService(typeof(IFolderBridgeModelFactory)) is not IFolderBridgeModelFactory bridgeFactory ||
                    App.ServiceProvider.GetService(typeof(IFolderItemModelFactory)) is not IFolderItemModelFactory folderFactory ||
                    App.ServiceProvider.GetService(typeof(IFilterRuleCriteriaFactory)) is not IFilterRuleCriteriaFactory criteriaFactory)
                    throw new InvalidOperationException("Profile import services are unavailable.");

                var importer = new ProfileImportExportService(idFactory, bridgeFactory, folderFactory, criteriaFactory, _databaseService);
                var profile = importer.Import(dialog.FileName);
                _profileListModel.AddProfile(profile);
                _profileListModel.SelectedProfile = profile;
                _model.ActiveProfile = profile;
                TrackActiveProfile();
                RefreshBridgeItems();
                OnPropertyChanged(nameof(ActiveProfile));
                RaiseCanExecuteChanged();
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"The profile could not be imported. {exception.Message}",
                    "Import Profile Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exports the selected profile to a file.
        /// </summary>
        /// <remarks>
        /// Opens a save file dialog to select a location for the profile file, and exports the profile.
        /// </remarks>
        private void OnExportProfile()
        {
            var profile = _profileListModel.SelectedProfile;
            if (profile == null)
                return;

            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = "Export profile",
                Filter = "USortThis profile (*.usortprofile.json)|*.usortprofile.json|JSON files (*.json)|*.json",
                DefaultExt = ".usortprofile.json",
                AddExtension = true,
                FileName = $"{string.Join("_", profile.Name.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries))}.usortprofile.json"
            };
            if (dialog.ShowDialog(System.Windows.Application.Current?.MainWindow) != true)
                return;

            try
            {
                if (App.ServiceProvider?.GetService(typeof(IIdFactory)) is not IIdFactory idFactory ||
                    App.ServiceProvider.GetService(typeof(IFolderBridgeModelFactory)) is not IFolderBridgeModelFactory bridgeFactory ||
                    App.ServiceProvider.GetService(typeof(IFolderItemModelFactory)) is not IFolderItemModelFactory folderFactory ||
                    App.ServiceProvider.GetService(typeof(IFilterRuleCriteriaFactory)) is not IFilterRuleCriteriaFactory criteriaFactory)
                    throw new InvalidOperationException("Profile export services are unavailable.");

                var exporter = new ProfileImportExportService(idFactory, bridgeFactory, folderFactory, criteriaFactory, _databaseService);
                exporter.Export(profile, dialog.FileName);
            }
            catch (Exception exception)
            {
                System.Windows.MessageBox.Show(
                    $"The profile could not be exported. {exception.Message}",
                    "Export Profile Failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Retries the failed transfer for the selected file transfer detail.
        /// </summary>
        /// <remarks>
        /// Attempts to re-initiate the transfer for a file that previously failed to transfer.
        /// </remarks>
        /// <param name="detail">The file transfer detail for the transfer to retry.</param>
        private async Task OnRetryFailedTransferAsync(FileTransferDetailModel? detail)
        {
            var bridge = SelectedBridge;
            if (detail is null || bridge is null || App.ServiceProvider is null)
                return;

            try
            {
                var sortingService = ActivatorUtilities.CreateInstance<SortingService>(App.ServiceProvider);
                await sortingService.RetryAsync(bridge, detail, CancellationToken.None);
            }
            catch (Exception exception)
            {
                EnumerationErrors.Add(new FileEnumerationError(DateTime.UtcNow, detail.SourcePath, exception));
            }
            finally
            {
                (RetryFailedTransferCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Handles the property changed event for the selected transfer detail.
        /// </summary>
        /// <remarks>
        /// Updates the command state for the retry failed transfer command based on the transfer detail status.
        /// </remarks>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event args.</param>
        private void OnSelectedTransferDetailPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileTransferDetailModel.Status))
                (RetryFailedTransferCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Refreshes the list of location items from the configured locations and connected devices.
        /// </summary>
        public void RefreshLocationItems()
        {
            LocationItems.Clear();
            foreach (var device in DeviceService.InterfaceDevices)
                LocationItems.Add(new LocationListEntry(device));
            foreach (var location in _databaseService.LoadConfiguredLocations())
            {
                location.IsConnected = _configuredLocationService.TestConnection(location).Success;
                LocationItems.Add(new LocationListEntry(location));
            }
        }

        /// <summary>
        /// Opens the dialog to add a new configured location.
        /// </summary>
        public void AddConfiguredLocation()
        {
            var dialog = new UI.Views.ConfiguredLocationDialog(_configuredLocationService) { Owner = System.Windows.Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
                RefreshLocationItems();
        }

        /// <summary>
        /// Opens the dialog to edit an existing configured location.
        /// </summary>
        /// <param name="location">The configured location to edit.</param>
        public void EditConfiguredLocation(ConfiguredLocationModel location)
        {
            var dialog = new UI.Views.ConfiguredLocationDialog(_configuredLocationService, location) { Owner = System.Windows.Application.Current.MainWindow };
            if (dialog.ShowDialog() == true)
                RefreshLocationItems();
        }

        /// <summary>
        /// Removes a configured location.
        /// </summary>
        /// <param name="location">The configured location to remove.</param>
        public void RemoveConfiguredLocation(ConfiguredLocationModel location)
        {
            if (System.Windows.MessageBox.Show($"Remove '{location.Label}' from configured locations?", "Remove location", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;
            _configuredLocationService.Delete(location);
            RefreshLocationItems();
        }

        /// <summary>
        /// Tests the connection to a configured location and updates its connection status.
        /// </summary>
        /// <param name="location">The configured location to test.</param>
        /// <returns>A message indicating the result of the connection test.</returns>
        public string TestConfiguredLocation(ConfiguredLocationModel location)
        {
            var result = _configuredLocationService.TestConnection(location);
            location.IsConnected = result.Success;
            return result.Message;
        }

        /// <summary>
        /// Selects a bridge and makes it the currently active bridge.
        /// </summary>
        /// <param name="bridge">The bridge to select.</param>
        public void SelectBridge(FolderBridgeModel bridge)
        {
            if (BridgeItems.Contains(bridge))
                SelectedBridge = bridge;
        }

        private void RefreshBridgeItems()
        {
            var previouslySelected = SelectedBridge;
            BridgeItems.Clear();
            if (_model.ActiveProfile == null) return;
            foreach (var bridge in _model.ActiveProfile.GetFolderBridges())
                BridgeItems.Add(bridge);
            SelectedBridge = previouslySelected != null && BridgeItems.Contains(previouslySelected)
                ? previouslySelected
                : BridgeItems.FirstOrDefault();
        }

        private void TrackActiveProfile()
        {
            if (_observedProfile != null)
            {
                _observedProfile.ItemAdded -= ActiveProfileBridgeChanged;
                _observedProfile.ItemRemoved -= ActiveProfileBridgeChanged;
            }

            _observedProfile = _model.ActiveProfile;
            if (_observedProfile != null)
            {
                _observedProfile.ItemAdded += ActiveProfileBridgeChanged;
                _observedProfile.ItemRemoved += ActiveProfileBridgeChanged;
            }
        }

        private void ActiveProfileBridgeChanged(object? sender, ListItemEventArgs<FolderBridgeModel> e) => RefreshBridgeItems();

        /// <summary>
        /// Closes the application, raising the <see cref="RequestClose"/> event.
        /// </summary>
        private void OnCloseApplication()
        {
            // Instead of calling Application.Current.Shutdown() here,
            // raise an event so the View can decide how to close (Close, Shutdown, prompt user, etc.).
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        // Pseudocode / plan (detailed):
        // 1. Ensure App.ServiceProvider is available; throw if null.
        // 2. Try to resolve IAboutWindowViewModelFactory and IAboutWindowModelFactory from DI.
        // 3. If both factories are available:
        //    a. Create the about model via IAboutWindowModelFactory.Create().
        //    b. Create the about view model via IAboutWindowViewModelFactory.Create(model).
        // 4. Create an instance of the AboutWindow view (USortThis.UI.Views.AboutWindow).
        // 5. If we created a view model, assign it to AboutWindow.DataContext.
        // 6. Set AboutWindow.Owner to the current application main window if available.
        // 7. Call ShowDialog() to display the AboutWindow modally.
        // 8. Use ServiceProvider.GetService where possible so behavior degrades gracefully if factories are not registered.
        /// <summary>
        /// Shows the about dialog, displaying information about the application and version.
        /// </summary>
        private void OnShowAbout()
        {
            if (App.ServiceProvider is null)
                throw new InvalidOperationException("ServiceProvider is not initialized.");

            // Attempt to resolve factories from DI
            var aboutVmFactory = App.ServiceProvider.GetService(typeof(IAboutWindowViewModelFactory)) as dynamic;
            var aboutModelFactory = App.ServiceProvider.GetService(typeof(IAboutWindowModelFactory)) as dynamic;

            object? aboutViewModel = null;

            try
            {
                if (aboutModelFactory is not null && aboutVmFactory is not null)
                {
                    // Create model and view model using factories if available
                    var aboutModel = aboutModelFactory.Create();
                    aboutViewModel = aboutVmFactory.Create(aboutModel);
                }
            }
            catch (Exception)
            {
                // If factories throw or aren't compatible, fall back to showing the view without a DataContext.
                aboutViewModel = null;
            }

            // Create and configure the AboutWindow view
            var aboutWindow = new USortThis.UI.Views.AboutWindow();

            if (aboutViewModel is not null)
            {
                aboutWindow.DataContext = aboutViewModel;
            }

            // Set owner to current main window if available (keeps window modal to app)
            var currentMain = System.Windows.Application.Current?.MainWindow;
            if (currentMain is not null && !ReferenceEquals(currentMain, aboutWindow))
            {
                aboutWindow.Owner = currentMain;
            }

            // Show the about window modally
            aboutWindow.ShowDialog();
        }

        /// <summary>
        /// Opens the settings dialog or window.
        /// </summary>
        private void OnOpenSettings()
        {
            // Implementation for opening settings
        }

        /// <summary>
        /// Creates a new profile and opens the profile editor.
        /// </summary>
        private void OnCreateNewProfile()
        {
            OpenProfileEditor(createNewProfile: true);
        }

        /// <summary>
        /// Edits the selected profile by opening it in the profile editor.
        /// </summary>
        private void OnEditProfile()
        {
            if (_profileListModel.SelectedProfile != null)
                OpenProfileEditor();
        }

        /// <summary>
        /// Deletes the currently selected profile after user confirmation.
        /// </summary>
        private void OnDeleteProfile()
        {
            var profile = _profileListModel.SelectedProfile;
            if (profile == null)
                return;

            var result = System.Windows.MessageBox.Show(
                $"Delete profile '{profile.Name}' and its associated rules? This cannot be undone.",
                "Delete Profile",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result != System.Windows.MessageBoxResult.Yes)
                return;

            if (App.ServiceProvider?.GetService(typeof(IDatabaseService)) is not IDatabaseService databaseService)
                return;

            try
            {
                databaseService.DeleteProfile(profile.Id.ToString());
                var profiles = _profileListModel.Profiles;
                var nextProfile = profiles.FirstOrDefault(candidate => !ReferenceEquals(candidate, profile));
                _profileListModel.RemoveProfile(profile);
                _profileListModel.SelectedProfile = nextProfile;
                _model.ActiveProfile = nextProfile;

                OnPropertyChanged(nameof(ActiveProfile));
                RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"The profile could not be deleted. {ex.Message}",
                    "Delete Profile Failed",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Selects the specified profile as the active profile.
        /// </summary>
        private void OnSelectProfile()
        {
            _model.ActiveProfile = _profileListModel.SelectedProfile;
        }

        private void OpenProfileEditor(bool createNewProfile = false)
        {
            if (App.ServiceProvider?.GetService(typeof(IProfileEditorWindowModelFactory)) is not IProfileEditorWindowModelFactory editorFactory)
                return;

            var targetProfile = createNewProfile ? null : _profileListModel.SelectedProfile;
            var editor = editorFactory.Create(_profileListModel, targetProfile);
            editor.Owner = System.Windows.Application.Current?.MainWindow;
            editor.ShowDialog();

            _model.ActiveProfile = _profileListModel.SelectedProfile;
            OnPropertyChanged(nameof(ActiveProfile));
            RaiseCanExecuteChanged();
        }

        private async Task OnStartSortAsync(ProfileItemModel? profile = null)
        {
            profile ??= _model.ActiveProfile;
            if (profile is null || _profileRuns.ContainsKey(profile)) return;
            if (App.ServiceProvider is null) throw new InvalidOperationException("ServiceProvider is not initialized.");

            var sortingService = ActivatorUtilities.CreateInstance<SortingService>(App.ServiceProvider);
            var run = new ProfileRun
            {
                Cancellation = new CancellationTokenSource()
            };
            lock (_profileRunsLock)
            {
                if (_profileRuns.ContainsKey(profile)) return;
                var waitForCopy = false;
                foreach (var active in _profileRuns.Keys)
                {
                    var conflict = ProfilePathConflictChecker.Check(profile, active);
                    if (conflict.Kind == ProfileConflictKind.Conflict && active.Status != ProfileItemModel.ProfileStatus.WaitingForProfile)
                    {
                        System.Windows.MessageBox.Show(conflict.Message, "Profile Path Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                    waitForCopy |= conflict.Kind == ProfileConflictKind.WaitForCopyBeforeMove;
                }
                if (waitForCopy)
                    profile.Status = ProfileItemModel.ProfileStatus.WaitingForProfile;
                _profileRuns.Add(profile, run);
            }

            if (profile.Status != ProfileItemModel.ProfileStatus.WaitingForProfile)
                profile.Status = ProfileItemModel.ProfileStatus.WaitingForSource;
            RaiseCanExecuteChanged();

            // progress for UI percent
            var progress = new Progress<int>(p => SortProgress = p);

            // errors reporter that dispatches to the ObservableCollection on UI thread
            var errorsReporter = new Progress<FileEnumerationError>(err =>
            {
                try
                {
                    // Ensure we mutate the collection on the UI thread
                    var dispatcher = System.Windows.Application.Current?.Dispatcher;
                    if (dispatcher is not null)
                    {
                        dispatcher.Invoke(() => EnumerationErrors.Add(err));
                    }
                    else
                    {
                        // Fallback if no dispatcher (unit tests)
                        EnumerationErrors.Add(err);
                    }
                }
                catch
                {
                    // swallow any logging errors to avoid breaking the copy operation
                }
            });

            try
            {
                while (!run.Cancellation.IsCancellationRequested)
                {
                    KeyValuePair<ProfileItemModel, ProfileRun>[] activeRuns;
                    lock (_profileRunsLock)
                        activeRuns = _profileRuns.Where(entry => !ReferenceEquals(entry.Key, profile)).ToArray();

                    var activeCopyRuns = new List<ProfileRun>();
                    foreach (var active in activeRuns)
                    {
                        var conflict = ProfilePathConflictChecker.Check(profile, active.Key);
                        if (conflict.Kind == ProfileConflictKind.Conflict && active.Key.Status != ProfileItemModel.ProfileStatus.WaitingForProfile)
                        {
                            System.Windows.MessageBox.Show(conflict.Message, "Profile Path Conflict", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        if (conflict.Kind == ProfileConflictKind.WaitForCopyBeforeMove)
                            activeCopyRuns.Add(active.Value);
                    }

                    if (activeCopyRuns.Count > 0)
                    {
                        profile.Status = ProfileItemModel.ProfileStatus.WaitingForProfile;
                        await Task.WhenAny(activeCopyRuns.Select(active => active.Completion.Task));
                        if (run.Cancellation.IsCancellationRequested)
                            break;
                        continue;
                    }

                    lock (_profileRunsLock)
                    {
                        if (_profileRuns.Where(entry => !ReferenceEquals(entry.Key, profile))
                            .Any(entry => ProfilePathConflictChecker.Check(profile, entry.Key).Kind != ProfileConflictKind.None))
                            continue;
                        profile.Status = ProfileItemModel.ProfileStatus.InProgress;
                        break;
                    }
                }

                if (!run.Cancellation.IsCancellationRequested)
                {
                    await RunProfileAsync(profile, sortingService, run, progress, errorsReporter);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                EnumerationErrors.Add(new FileEnumerationError(DateTime.UtcNow, profile.Name, ex));
            }
            finally
            {
                lock (_profileRunsLock)
                    _profileRuns.Remove(profile);
                run.Cancellation.Dispose();
                profile.Status = ProfileItemModel.ProfileStatus.Idle;
                run.Completion.TrySetResult(true);
                RaiseCanExecuteChanged();
            }
        }

        private async Task RunProfileAsync(ProfileItemModel profile, ISortingService sortingService, ProfileRun run, IProgress<int> progress, IProgress<FileEnumerationError> errorsReporter)
        {
            await sortingService.StartAsync(profile, progress, run.Cancellation.Token, errorsReporter, ResolveTransferConflictAsync);
        }

        private Task<ProfileItemModel.ConflictHandlingMode> ResolveTransferConflictAsync(string sourcePath, string destinationPath)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null)
                return Task.FromResult(ProfileItemModel.ConflictHandlingMode.Skip);

            return dispatcher.InvokeAsync(() =>
            {
                var result = System.Windows.MessageBox.Show(
                    System.Windows.Application.Current?.MainWindow,
                    $"A different file already exists at the destination.\n\nSource: {sourcePath}\nDestination: {destinationPath}\n\nYes: overwrite\nNo: skip\nCancel: keep both",
                    "Destination Conflict",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Warning);

                return result switch
                {
                    MessageBoxResult.Yes => ProfileItemModel.ConflictHandlingMode.Overwrite,
                    MessageBoxResult.Cancel => ProfileItemModel.ConflictHandlingMode.KeepBoth,
                    _ => ProfileItemModel.ConflictHandlingMode.Skip
                };
            }).Task;
        }

        private void OnStopSort()
        {
            foreach (var run in _profileRuns.Values)
                run.Cancellation.Cancel();
        }

        private void OnStopSort(ProfileItemModel? profile)
        {
            if (profile == null) return;
            lock (_profileRunsLock)
                if (_profileRuns.TryGetValue(profile, out var run))
                    run.Cancellation.Cancel();
        }

        private void RaiseCanExecuteChanged()
        {
            (StartSortCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopSortCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StartProfileSortCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (StopProfileSortCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (RetryFailedTransferCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (EditProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (DeleteProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (ExportProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (PreviewProfileCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }
}
