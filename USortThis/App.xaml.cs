using Microsoft.Extensions.DependencyInjection;
using System;
using System.Configuration;
using System.Data;
using System.Threading.Tasks;
using System.Windows;
using System.Diagnostics;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;
using USortThis.Core.Data;
using USortThis.Core.Factories;
using USortThis.Core.Interfaces;
using USortThis.Core.Services;
using USortThis.UI.Views;

namespace USortThis
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        public static IServiceProvider? ServiceProvider { get; private set; }
        private Forms.NotifyIcon? _trayIcon;
        private bool _isExiting;

        internal bool IsExiting => _isExiting;
        
        protected override async void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ILocalizationService, IniLocalizationService>();
            services.AddSingleton<IFileSystemIOService, WinFileSystemIOService>();
            services.AddSingleton<WindowsCredentialStore>();
            services.AddSingleton<ConfiguredLocationService>();
            // Id factory used by model factories
            services.AddSingleton<IIdFactory, MonotonicTickIdFactory>();

            // ViewModel / UI factories
            services.AddSingleton<IFilterRuleCriteriaViewModelFactory, FilterRuleCriteriaViewModelFactory>();
            services.AddSingleton<IFileItemViewModelFactory, FileItemViewModelFactory>();
            services.AddSingleton<IFolderItemViewModelFactory, FolderItemViewModelFactory>();
            services.AddSingleton<IFolderBridgeViewModelFactory, FolderBridgeViewModelFactory>();
            services.AddSingleton<IProfileItemViewModelFactory, ProfileItemViewModelFactory>();
            services.AddSingleton<IProfileEditorWindowViewModelFactory, ProfileEditorWindowViewModelFactory>();
            services.AddSingleton<IProfileEditorWindowModelFactory, ProfileEditorWindowFactory>();
            services.AddSingleton<IAboutWindowModelFactory, AboutWindowModelFactory>();
            services.AddSingleton<IAboutWindowViewModelFactory, AboutWindowViewModelFactory>();

            services.AddSingleton<IFilterRuleCriteriaFactory, FilterRuleCriteriaFactory>();
            services.AddSingleton<IFileItemModelFactory, FileItemModelFactory>();
            services.AddSingleton<IFolderItemModelFactory, FolderItemModelFactory>();
            services.AddSingleton<IFolderBridgeModelFactory, FolderBridgeModelFactory>();
            services.AddSingleton<IProfileItemModelFactory, ProfileItemModelFactory>();

            services.AddTransient<ISortingService, SortingService>();

            // Shared model that holds profiles so multiple views/viewmodels can interact with the same data
            services.AddSingleton(provider => new USortThis.Core.Models.ProfileListModel(provider.GetRequiredService<USortThis.Core.Interfaces.IIdFactory>().CreateId()));

            services.AddSingleton<IDatabaseService, DatabaseService>();
            services.AddTransient<IMainWindowModelFactory, MainViewModelFactory>();
            services.AddTransient<MainWindowFactory>();

            // Build provider
            ServiceProvider = services.BuildServiceProvider();

            try
            {
                var db = App.ServiceProvider.GetRequiredService<IDatabaseService>();
                await db.InitializeAsync();
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to initialize application: {ex.Message}");
                System.Windows.MessageBox.Show(
                    $"USortThis could not initialize its database and will close.\n\n{ex.Message}",
                    "Database initialization failed",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown();
                return;
            }

            base.OnStartup(e);

            MainWindow mainWindow = App.ServiceProvider.GetRequiredService<MainWindowFactory>().Create();
            mainWindow.Closing += MainWindow_Closing;
            mainWindow.StateChanged += MainWindow_StateChanged;
            CreateTrayIcon(mainWindow);
            mainWindow.Show();

            try
            {
                await CopyQueueFactory.InitializeAsync();
                CopyQueueFactory.StartCopyQueue();
            }
            catch (Exception ex)
            {
                Debug.Print($"Failed to initialize application: {ex.Message}");
            }
        }

        private void CreateTrayIcon(Window mainWindow)
        {
            var menu = new Forms.ContextMenuStrip();
            var openItem = new Forms.ToolStripMenuItem("Open USortThis");
            openItem.Click += (_, _) => ShowMainWindow(mainWindow);
            var exitItem = new Forms.ToolStripMenuItem("Exit");
            exitItem.Click += (_, _) => ExitApplication();
            menu.Items.Add(openItem);
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add(exitItem);

            _trayIcon = new Forms.NotifyIcon
            {
                Icon = Drawing.SystemIcons.Application,
                Text = "USortThis",
                ContextMenuStrip = menu,
                Visible = true
            };
            _trayIcon.DoubleClick += (_, _) => ShowMainWindow(mainWindow);
        }

        private void MainWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_isExiting)
                return;

            e.Cancel = true;
            if (sender is Window window)
                window.Hide();
        }

        private void MainWindow_StateChanged(object? sender, EventArgs e)
        {
            if (sender is Window { WindowState: WindowState.Minimized } window)
            {
                window.Hide();
                window.WindowState = WindowState.Normal;
            }
        }

        private void ShowMainWindow(Window window)
        {
            if (!window.IsVisible)
                window.Show();
            if (window.WindowState == WindowState.Minimized)
                window.WindowState = WindowState.Normal;
            window.Activate();
        }

        internal void ExitApplication()
        {
            if (_isExiting)
                return;

            _isExiting = true;
            _trayIcon?.Dispose();
            _trayIcon = null;
            Shutdown();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            base.OnExit(e);
        }
    }

}
