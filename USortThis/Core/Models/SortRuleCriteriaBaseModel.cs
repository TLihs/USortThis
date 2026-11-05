using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Models
{
    public abstract class SortRuleCriteriaBaseModel : ModelBase
    {
        protected IFileSystemIOService _fileInfoService;

        public SortRuleCriteriaBaseModel(long id, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
        }

        public abstract void ApplyToPath(string filePath);
    }
}
