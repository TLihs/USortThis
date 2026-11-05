using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    public class FilterRuleCriteriaViewModel : ViewModelBase
    {
        public FilterRuleCriteriaBaseModel Model { get; }

        public FilterRuleCriteriaViewModel(FilterRuleCriteriaBaseModel model, ILocalizationService localizationService) : base(localizationService)
        {
            Model = model;
        }
    }
}
