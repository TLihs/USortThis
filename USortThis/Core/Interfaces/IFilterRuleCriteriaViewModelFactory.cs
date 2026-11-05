using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFilterRuleCriteriaViewModelFactory
    {
        public FilterRuleCriteriaViewModel Create(FilterRuleCriteriaBaseModel model);
    }
}
