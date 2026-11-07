using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;
using USortThis.UI.Views;

namespace USortThis.Core.Factories
{
    public class MainWindowFactory
    {
        private readonly IMainWindowModelFactory _viewModelFactory;

        /// <summary>Initializes the factory with the main-window view-model factory.</summary>
        /// <param name="viewModelFactory">The factory used to create the window's view model.</param>
        public MainWindowFactory(IMainWindowModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

        /// <summary>Creates the main window, assigns its view model, and connects close requests.</summary>
        public MainWindow Create()
        {
            var model = new MainWindowModel();
            var vm = _viewModelFactory.Create(model);
            var window = new MainWindow
            {
                DataContext = vm
            };

            // Subscribe so the ViewModel can request the window to close (MVVM-safe)
            if (vm is MainWindowViewModel mainVm)
            {
                mainVm.RequestClose += (s, e) =>
                {
                    // If window isn't already closed
                    if (window.IsLoaded)
                        window.Close();
                };
            }

            return window;
        }
    }
}
