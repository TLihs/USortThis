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

        public AboutWindowViewModelFactory(ILocalizationService localizationService)
        {
            _localizationService = localizationService;
        }
        public AboutWindowViewModel Create(AboutWindowModel model)
        {
            return new AboutWindowViewModel(model, _localizationService);
        }
    }
}
