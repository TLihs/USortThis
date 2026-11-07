using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Factories
{
    public class AboutWindowViewModelFactory : IAboutWindowViewModelFactory
    {
        private readonly ILocalizationService _localizationService;

        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public AboutWindowViewModelFactory(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        /// <summary>Creates an About-window view model for the supplied model.</summary>
        /// <param name="model">The model containing About-window data.</param>
        public AboutWindowViewModel Create(AboutWindowModel model)
        {
            return new AboutWindowViewModel(model, _localizationService);
        }
    }
}
