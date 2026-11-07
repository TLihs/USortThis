using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFilterRuleCriteriaFactory
    {
        /// <summary>Creates a criterion that does not filter files.</summary>
        public FilterRuleCriteriaModelNone CreateNoFilter();
        /// <summary>Reconstructs a no-filter criterion with its persisted identifier.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        public FilterRuleCriteriaModelNone LoadFromDatabaseNoFilter(long id);
        /// <summary>Creates a criterion that matches the supplied file extensions.</summary>
        /// <param name="extensions">The extensions to match.</param>
        public FilterRuleCriteriaModelExtension CreateExtensionFilter(string extensions);
        /// <summary>Reconstructs an extension criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="extensions">The extensions to match.</param>
        public FilterRuleCriteriaModelExtension LoadFromDatabaseExtensionFilter(long id, string extensions);
        /// <summary>Creates a criterion that matches file names using a pattern.</summary>
        /// <param name="pattern">The file-name pattern to match.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression.</param>
        public FilterRuleCriteriaModelFileName CreateFileNameFilter(string pattern, bool useRegex);
        /// <summary>Reconstructs a file-name criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="pattern">The file-name pattern to match.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression.</param>
        public FilterRuleCriteriaModelFileName LoadFromDatabaseFileNameFilter(long id, string pattern, bool useRegex);
        /// <summary>Creates a criterion requiring a date on or after the supplied threshold.</summary>
        /// <param name="minDate">The earliest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMinDate CreateMinDateFilter(DateTime minDate, bool useCreationTime);
        /// <summary>Reconstructs a minimum-date criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="minDate">The earliest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMinDate LoadFromDatabaseMinDateFilter(long id, DateTime minDate, bool useCreationTime);
        /// <summary>Creates a criterion requiring a date on or before the supplied threshold.</summary>
        /// <param name="maxDate">The latest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMaxDate CreateMaxDateFilter(DateTime maxDate, bool useCreationTime);
        /// <summary>Reconstructs a maximum-date criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="maxDate">The latest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMaxDate LoadFromDatabaseMaxDateFilter(long id, DateTime maxDate, bool useCreationTime);
    }
}
