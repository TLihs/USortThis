using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Defines the base class for sort rule criteria.
    /// </summary>
    public abstract class SortRuleCriteriaBaseModel : ModelBase
    {
        protected IFileSystemIOService _fileInfoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SortRuleCriteriaBaseModel"/> class.
        /// </summary>
        /// <param name="id">The identifier for the sort rule criteria.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
        public SortRuleCriteriaBaseModel(long id, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
        }

        /// <summary>
        /// Applies the sorting criteria to the specified file path.
        /// </summary>
        /// <param name="filePath">The file path to which the sorting criteria should be applied.</param>
        public abstract void ApplyToPath(string filePath);
    }
}
