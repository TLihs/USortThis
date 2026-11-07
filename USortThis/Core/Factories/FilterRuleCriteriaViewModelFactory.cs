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
    public class FilterRuleCriteriaViewModelFactory : BaseLocalizedViewModelFactory, IFilterRuleCriteriaViewModelFactory
    {
        /// <summary>Initializes the factory with the localization service used by created view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        public FilterRuleCriteriaViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>Creates a view model for the supplied filter criterion.</summary>
        /// <param name="model">The criterion to expose to the view.</param>
        public FilterRuleCriteriaViewModel Create(FilterRuleCriteriaBaseModel model)
        {
            return new FilterRuleCriteriaViewModel(model, _localizationService);
        }
    }
}
