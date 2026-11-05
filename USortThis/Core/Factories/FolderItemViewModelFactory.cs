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
        public FolderItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        public FolderItemViewModel Create(FolderItemModel model)
        {
            return new FolderItemViewModel(model, _localizationService);
        }
    }
}
