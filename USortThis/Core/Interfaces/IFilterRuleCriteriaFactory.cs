using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFilterRuleCriteriaFactory
    {
        public FilterRuleCriteriaModelNone CreateNoFilter();
        public FilterRuleCriteriaModelNone LoadFromDatabaseNoFilter(long id);
        public FilterRuleCriteriaModelExtension CreateExtensionFilter(string extensions);
        public FilterRuleCriteriaModelExtension LoadFromDatabaseExtensionFilter(long id, string extensions);
        public FilterRuleCriteriaModelFileName CreateFileNameFilter(string pattern, bool useRegex);
        public FilterRuleCriteriaModelFileName LoadFromDatabaseFileNameFilter(long id, string pattern, bool useRegex);
        public FilterRuleCriteriaModelMinDate CreateMinDateFilter(DateTime minDate, bool useCreationTime);
        public FilterRuleCriteriaModelMinDate LoadFromDatabaseMinDateFilter(long id, DateTime minDate, bool useCreationTime);
        public FilterRuleCriteriaModelMaxDate CreateMaxDateFilter(DateTime maxDate, bool useCreationTime);
        public FilterRuleCriteriaModelMaxDate LoadFromDatabaseMaxDateFilter(long id, DateTime maxDate, bool useCreationTime);
    }
}
