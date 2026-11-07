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
    public class FileItemViewModelFactory : BaseLocalizedViewModelFactory, IFileItemViewModelFactory
    {
        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public FileItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>Creates a file-item view model for the supplied model.</summary>
        /// <param name="model">The file model to expose to the view.</param>
        public FileItemViewModel Create(FileItemModel model)
        {
            return new FileItemViewModel(model, _localizationService);
        }
    }
}
