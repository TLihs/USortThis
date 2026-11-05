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

        public MainWindowFactory(IMainWindowModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }

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
