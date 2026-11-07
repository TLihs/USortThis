using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Factories
{
    public class FilterRuleCriteriaFactory : BaseIdModelFactory, IFilterRuleCriteriaFactory
    {
        private IFileSystemIOService _fileInfoService;

        /// <summary>Initializes the factory with identifier and file-system services.</summary>
        /// <param name="idFactory">The source of criterion identifiers.</param>
        /// <param name="fileInfoService">The service used by criteria to inspect files.</param>
        public FilterRuleCriteriaFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
        }

        /// <summary>Creates a criterion that does not filter files.</summary>
        public FilterRuleCriteriaModelNone CreateNoFilter()
        {
            return new FilterRuleCriteriaModelNone(_idFactory.CreateId());
        }

        /// <summary>Reconstructs a no-filter criterion with its persisted identifier.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        public FilterRuleCriteriaModelNone LoadFromDatabaseNoFilter(long id)
        {
            return new FilterRuleCriteriaModelNone(id);
        }

        /// <summary>Creates a criterion that matches the supplied file extensions.</summary>
        /// <param name="extensions">The extensions to match.</param>
        public FilterRuleCriteriaModelExtension CreateExtensionFilter(string extensions)
        {
            return new FilterRuleCriteriaModelExtension(_idFactory.CreateId(), extensions, _fileInfoService);
        }

        /// <summary>Reconstructs an extension criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="extensions">The extensions to match.</param>
        public FilterRuleCriteriaModelExtension LoadFromDatabaseExtensionFilter(long id, string extensions)
        {
            return new FilterRuleCriteriaModelExtension(id, extensions, _fileInfoService);
        }

        /// <summary>Creates a criterion that matches file names using a pattern.</summary>
        /// <param name="pattern">The file-name pattern to match.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression.</param>
        public FilterRuleCriteriaModelFileName CreateFileNameFilter(string pattern, bool useRegex)
        {
            return new FilterRuleCriteriaModelFileName(_idFactory.CreateId(), pattern, useRegex, _fileInfoService);
        }

        /// <summary>Reconstructs a file-name criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="pattern">The file-name pattern to match.</param>
        /// <param name="useRegex">Whether the pattern is a regular expression.</param>
        public FilterRuleCriteriaModelFileName LoadFromDatabaseFileNameFilter(long id, string pattern, bool useRegex)
        {
            return new FilterRuleCriteriaModelFileName(id, pattern, useRegex, _fileInfoService);
        }

        /// <summary>Creates a criterion requiring a date on or after the supplied threshold.</summary>
        /// <param name="minDate">The earliest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMinDate CreateMinDateFilter(DateTime minDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMinDate(_idFactory.CreateId(), minDate, useCreationTime, _fileInfoService);
        }

        /// <summary>Reconstructs a minimum-date criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="minDate">The earliest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMinDate LoadFromDatabaseMinDateFilter(long id, DateTime minDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMinDate(id, minDate, useCreationTime, _fileInfoService);
        }

        /// <summary>Creates a criterion requiring a date on or before the supplied threshold.</summary>
        /// <param name="maxDate">The latest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMaxDate CreateMaxDateFilter(DateTime maxDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMaxDate(_idFactory.CreateId(), maxDate, useCreationTime, _fileInfoService);
        }

        /// <summary>Reconstructs a maximum-date criterion from persisted values.</summary>
        /// <param name="id">The persisted criterion identifier.</param>
        /// <param name="maxDate">The latest accepted date.</param>
        /// <param name="useCreationTime">Whether to compare creation time rather than last-write time.</param>
        public FilterRuleCriteriaModelMaxDate LoadFromDatabaseMaxDateFilter(long id, DateTime maxDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMaxDate(id, maxDate, useCreationTime, _fileInfoService);
        }
    }
}
