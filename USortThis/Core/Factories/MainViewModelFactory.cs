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

        /// <summary>Initializes the factory with localization and profile-list dependencies.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        /// <param name="profileListModel">The shared profile list used by the main view model.</param>
        public MainViewModelFactory(ILocalizationService localizationService, ProfileListModel profileListModel) : base(localizationService)
        {
            _profileListModel = profileListModel;
        }

        /// <summary>Creates the main-window view model and resolves its application services.</summary>
        /// <param name="model">The model used to initialize the view model.</param>
        /// <exception cref="InvalidOperationException">Application services have not been initialized.</exception>
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
