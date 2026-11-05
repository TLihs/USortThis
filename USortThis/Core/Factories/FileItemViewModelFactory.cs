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
        public FileItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        public FileItemViewModel Create(FileItemModel model)
        {
            return new FileItemViewModel(model, _localizationService);
        }
    }
}
