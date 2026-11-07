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

        /// <summary>Initializes the base factory with the localization service used by derived factories.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        /// <exception cref="ArgumentNullException"><paramref name="localizationService"/> is null.</exception>
        protected BaseLocalizedViewModelFactory(ILocalizationService localizationService)
        {
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
        }
    }
}
