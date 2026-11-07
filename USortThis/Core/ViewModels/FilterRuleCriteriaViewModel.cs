using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// ViewModel for filter rule criteria.
    /// </summary>
    public class FilterRuleCriteriaViewModel : ViewModelBase
    {
        /// <summary>
        /// Gets the model.
        /// </summary>
        public FilterRuleCriteriaBaseModel Model { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleCriteriaViewModel"/> class.
        /// </summary>
        /// <param name="model">The filter rule criteria model.</param>
        /// <param name="localizationService">The localization service.</param>
        public FilterRuleCriteriaViewModel(FilterRuleCriteriaBaseModel model, ILocalizationService localizationService) : base(localizationService)
        {
            Model = model;
        }
    }
}
