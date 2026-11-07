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
    public class ProfileItemViewModelFactory : BaseLocalizedViewModelFactory, IProfileItemViewModelFactory
    {
        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public ProfileItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>Creates a profile-item view model for the supplied profile.</summary>
        /// <param name="model">The profile to expose to the view.</param>
        public ProfileItemViewModel Create(ProfileItemModel model)
        {
            return new ProfileItemViewModel(model, _localizationService);
        }
    }
}
