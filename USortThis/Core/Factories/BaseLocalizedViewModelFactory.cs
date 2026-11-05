using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Factories
{
    public abstract class BaseLocalizedViewModelFactory
    {
        protected readonly ILocalizationService _localizationService;

        protected BaseLocalizedViewModelFactory(ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        }
    }
}
