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
    public class FolderBridgeViewModelFactory : BaseLocalizedViewModelFactory, IFolderBridgeViewModelFactory
    {
        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public FolderBridgeViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>Creates a bridge view model for the supplied bridge.</summary>
        /// <param name="model">The bridge to expose to the view.</param>
        public FolderBridgeViewModel Create(FolderBridgeModel model)
        {
            return new FolderBridgeViewModel(model, _localizationService);
        }
    }
}
