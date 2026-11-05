using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;
using USortThis.Core.Services;

namespace USortThis.Core.Factories
{
    public class MainViewModelFactory : BaseLocalizedViewModelFactory, IMainWindowModelFactory
    {
        private readonly ProfileListModel _profileListModel;

        public MainViewModelFactory(ILocalizationService localizationService, ProfileListModel profileListModel) : base(localizationService)
        {
            _profileListModel = profileListModel;
        }

        public MainWindowViewModel Create(MainWindowModel model)
        {
            var serviceProvider = App.ServiceProvider ?? throw new InvalidOperationException("Application services have not been initialized.");
            return new MainWindowViewModel(model, _profileListModel,
                serviceProvider.GetRequiredService<IDatabaseService>(),
                serviceProvider.GetRequiredService<ConfiguredLocationService>(),
                _localizationService);
        }
    }
}
