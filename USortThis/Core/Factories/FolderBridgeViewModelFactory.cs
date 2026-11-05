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
        public FolderBridgeViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        public FolderBridgeViewModel Create(FolderBridgeModel model)
        {
            return new FolderBridgeViewModel(model, _localizationService);
        }
    }
}
