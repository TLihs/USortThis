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
    public class FolderItemViewModelFactory : BaseLocalizedViewModelFactory, IFolderItemViewModelFactory
    {
        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public FolderItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>Creates a folder-item view model for the supplied folder.</summary>
        /// <param name="model">The folder to expose to the view.</param>
        public FolderItemViewModel Create(FolderItemModel model)
        {
            return new FolderItemViewModel(model, _localizationService);
        }
    }
}
