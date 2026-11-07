using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFilterRuleCriteriaViewModelFactory
    {
        /// <summary>Creates a view model for the supplied filter criterion.</summary>
        /// <param name="model">The criterion to expose to the view.</param>
        public FilterRuleCriteriaViewModel Create(FilterRuleCriteriaBaseModel model);
    }
}
