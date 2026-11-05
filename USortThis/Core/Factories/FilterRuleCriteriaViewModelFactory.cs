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
        public FilterRuleCriteriaViewModelFactory(ILocalizationService localizationService) : base(localizationService)
        {
        }

        public FilterRuleCriteriaViewModel Create(FilterRuleCriteriaBaseModel model)
        {
            return new FilterRuleCriteriaViewModel(model, _localizationService);
        }
    }
}
