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

        public FilterRuleCriteriaFactory(IIdFactory idFactory, IFileSystemIOService fileInfoService) : base(idFactory)
        {
            _fileInfoService = fileInfoService;
        }

        public FilterRuleCriteriaModelNone CreateNoFilter()
        {
            return new FilterRuleCriteriaModelNone(_idFactory.CreateId());
        }

        public FilterRuleCriteriaModelNone LoadFromDatabaseNoFilter(long id)
        {
            return new FilterRuleCriteriaModelNone(id);
        }

        public FilterRuleCriteriaModelExtension CreateExtensionFilter(string extensions)
        {
            return new FilterRuleCriteriaModelExtension(_idFactory.CreateId(), extensions, _fileInfoService);
        }

        public FilterRuleCriteriaModelExtension LoadFromDatabaseExtensionFilter(long id, string extensions)
        {
            return new FilterRuleCriteriaModelExtension(id, extensions, _fileInfoService);
        }

        public FilterRuleCriteriaModelFileName CreateFileNameFilter(string pattern, bool useRegex)
        {
            return new FilterRuleCriteriaModelFileName(_idFactory.CreateId(), pattern, useRegex, _fileInfoService);
        }

        public FilterRuleCriteriaModelFileName LoadFromDatabaseFileNameFilter(long id, string pattern, bool useRegex)
        {
            return new FilterRuleCriteriaModelFileName(id, pattern, useRegex, _fileInfoService);
        }

        public FilterRuleCriteriaModelMinDate CreateMinDateFilter(DateTime minDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMinDate(_idFactory.CreateId(), minDate, useCreationTime, _fileInfoService);
        }

        public FilterRuleCriteriaModelMinDate LoadFromDatabaseMinDateFilter(long id, DateTime minDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMinDate(id, minDate, useCreationTime, _fileInfoService);
        }

        public FilterRuleCriteriaModelMaxDate CreateMaxDateFilter(DateTime maxDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMaxDate(_idFactory.CreateId(), maxDate, useCreationTime, _fileInfoService);
        }

        public FilterRuleCriteriaModelMaxDate LoadFromDatabaseMaxDateFilter(long id, DateTime maxDate, bool useCreationTime)
        {
            return new FilterRuleCriteriaModelMaxDate(id, maxDate, useCreationTime, _fileInfoService);
        }
    }
}
