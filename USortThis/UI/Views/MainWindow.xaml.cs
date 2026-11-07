using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using USortThis.UI.Views;
using System.ComponentModel;
using System.Collections.Specialized;
using Microsoft.Win32;
using System.IO;
using System.Security.Cryptography;
using System.Reflection;
using System.Threading.Tasks;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;
using System.Diagnostics;
using USortThis.Core.Interfaces;
using USortThis.Core.Services;

namespace USortThis.UI.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();

            // Subscribe to DataContext changes so we can attach/detach ViewModel events.
            DataContextChanged += MainWindow_DataContextChanged;
        }

        /// <summary>
        /// Handles the click event for the Add Location button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AddLocation_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.AddConfiguredLocation();
        }

        /// <summary>
        /// Handles the click event for the Edit Location button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void EditLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ConfiguredLocationModel location } && DataContext is MainWindowViewModel viewModel)
                viewModel.EditConfiguredLocation(location);
        }

        /// <summary>
        /// Handles the click event for the Remove Location button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RemoveLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ConfiguredLocationModel location } && DataContext is MainWindowViewModel viewModel)
                viewModel.RemoveConfiguredLocation(location);
        }

        /// <summary>
        /// Handles the click event for the Test Location button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void TestLocation_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement { DataContext: ConfiguredLocationModel location } && DataContext is MainWindowViewModel viewModel)
                System.Windows.MessageBox.Show(this, viewModel.TestConfiguredLocation(location), "Test location", MessageBoxButton.OK,
                    location.IsConnected ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }

        /// <summary>
        /// Handles changes to the DataContext, updating the ViewModel and device list as necessary.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void MainWindow_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.Print(nameof(MainWindow) + "::MainWindow_DataContextChanged(..)");
            if (e.OldValue is MainWindowViewModel oldVm)
            {
                oldVm.RequestClose -= OnViewModelRequestClose;
                DeviceService.InterfaceDevices.CollectionChanged -= DeviceList_CollectionChanged;
            }

            if (e.NewValue is MainWindowViewModel newVm)
            {
                newVm.RequestClose += OnViewModelRequestClose;
                DeviceService.InterfaceDevices.CollectionChanged += DeviceList_CollectionChanged;
                newVm.RefreshLocationItems();

                try
                {
                    var serviceProvider = App.ServiceProvider;
                    if (serviceProvider != null && (ListControl_Profiles.DataContext == null || ListControl_Profiles.DataContext is MainWindowViewModel))
                    {
                        if (serviceProvider.GetService(typeof(ProfileListModel)) is ProfileListModel profileListModel)
                        {
                            IProfileItemViewModelFactory? vmFactory = serviceProvider.GetService(typeof(IProfileItemViewModelFactory)) as IProfileItemViewModelFactory;
                            ILocalizationService? loc = serviceProvider.GetService(typeof(ILocalizationService)) as ILocalizationService;
                            if (vmFactory != null && loc != null)
                            {
                                var profileListVm = new ProfileListViewModel(profileListModel, vmFactory, loc);
                                ListControl_Profiles.DataContext = profileListVm;
                            }
                            else
                            {
                                Debug.Print("Warning: Failed to resolve dependencies for ProfileListViewModel. Profiles may not display correctly. vmFactory={0}, loc={1}", vmFactory, loc);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Failed to bind profile list: " + ex.Message);
                }
            }
        }

        /// <summary>
        /// Updates the device list when the underlying collection changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DeviceList_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (DataContext is MainWindowViewModel viewModel)
                viewModel.RefreshLocationItems();
        }

        /// <summary>
        /// Closes the application when requested by the ViewModel.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void OnViewModelRequestClose(object? sender, EventArgs e)
        {
            Dispatcher.Invoke(() => (System.Windows.Application.Current as App)?.ExitApplication());
        }

        /// <summary>
        /// Handles the click event for the Save menu item, saving profiles asynchronously.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private async void Menu_Save_Click(object sender, RoutedEventArgs e)
        {
            //try
            //{
            //    await Dispatcher.InvokeAsync(() => SetStatus("Saving profiles ...", 0));

            //    // Save all profiles (async) and notify user when done
            //    ProfileItemModel.SaveAllProfilesToDatabase();

            //    await Dispatcher.InvokeAsync(() => SetStatus("Profiles saved.", 4000));
            //}
            //catch (Exception ex)
            //{
            //    SetStatus("Failed to save profiles: " + ex.Message, 8000);
            //}
        }

        /// <summary>
        /// Handles the click event for the Exit menu item, closing the application.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Menu_Exit_Click(object sender, RoutedEventArgs e)
        {
            // Prefer the ViewModel command which triggers the RequestClose event
            // so the View can handle closing consistently (OnViewModelRequestClose).
            if (DataContext is MainWindowViewModel vm && vm.CloseApplicationCommand?.CanExecute(null) == true)
            {
                vm.CloseApplicationCommand.Execute(null);
            }
            else
            {
                // Fallback to closing the Window if no VM/command is available
                Close();
            }
        }

        /// <summary>
        /// Handles the click event for the Undo menu item, triggering an undo operation if implemented.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Menu_Undo_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Undo is not implemented.", 3000);
        }

        /// <summary>
        /// Handles the click event for the Redo menu item, triggering a redo operation if implemented.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Menu_Redo_Click(object sender, RoutedEventArgs e)
        {
            SetStatus("Redo is not implemented.", 3000);
        }

        /// <summary>
        /// Handles the click event for the Generate Test File menu item, opening the test file generator dialog.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Menu_GenerateTestFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TestFileGeneratorWindow { Owner = this };
            dialog.ShowDialog();
        }

        /// <summary>
        /// Handles the click event for the About menu item, displaying the About dialog if available.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            //var asm = Assembly.GetEntryAssembly();
            //var name = asm?.GetName().Name ?? "USortThis";
            //var version = asm?.GetName().Version?.ToString() ?? "?";
            //SetStatus($"{name} - version {version}.", 6000);
            if (DataContext is MainWindowViewModel vm && vm.ShowAboutCommand?.CanExecute(null) == true)
            {
                vm.ShowAboutCommand.Execute(null);
            }
        }

        /// <summary>
        /// Updates the status bar content in a non-blocking fashion, optionally clearing it after a timeout.
        /// </summary>
        /// <param name="message">The status message to display.</param>
        /// <param name="timeoutMs">The timeout in milliseconds after which to clear the message (optional).</param>
        private void SetStatus(string message, int timeoutMs = 5000)
        {
            try
            {
                if (Dispatcher.CheckAccess())
                    StatusBar_State.Content = message;
                else
                    Dispatcher.Invoke(() => StatusBar_State.Content = message);

                if (timeoutMs > 0)
                {
                    var captured = message;
                    Task.Run(async () =>
                    {
                        await Task.Delay(timeoutMs).ConfigureAwait(false);
                        try
                        {
                            if (Dispatcher.CheckAccess())
                            {
                                if (StatusBar_State.Content?.ToString() == captured)
                                    StatusBar_State.Content = "Ready";
                            }
                            else
                            {
                                Dispatcher.Invoke(() =>
                                {
                                    if (StatusBar_State.Content?.ToString() == captured)
                                        StatusBar_State.Content = "Ready";
                                });
                            }
                        }
                        catch { }
                    });
                }
            }
            catch { }
        }
    }
}
