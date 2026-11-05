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
        public ProfileItemViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        public ProfileItemViewModel Create(ProfileItemModel model)
        {
            return new ProfileItemViewModel(model, _localizationService);
        }
    }
}
