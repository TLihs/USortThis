using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static USortThis.Core.Models.FileItemModel;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents a model for a folder item in the USortThis application.
    /// </summary>
    public class FolderItemModel : ModelBase
    {
        /// <summary>
        /// Represents the status of the folder.
        /// </summary>
        public enum FolderStatus
        {
            Available,
            Disconnected
        }
        
        private string _folderPath;
        private string _customName;
        private string _customDescription;
        private FolderStatus _status;
        private long _fileCount;

        /// <summary>
        /// Gets or sets the path of the folder.
        /// </summary>
        public string FolderPath
        {
            get => _folderPath;
            set
            {
                if (_folderPath != value)
                {
                    _folderPath = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets or sets the custom name of the folder.
        /// </summary>
        public string CustomName
        {
            get => _customName;
            set
            {
                if (_customName != value)
                {
                    _customName = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets or sets the custom description of the folder.
        /// </summary>
        public string CustomDescription
        {
            get => _customDescription;
            set
            {
                if (_customDescription != value)
                {
                    _customDescription = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets or sets the status of the folder.
        /// </summary>
        public FolderStatus Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }
        /// <summary>
        /// Gets or sets the file count in the folder.
        /// </summary>
        public long FileCount
        {
            get => _fileCount;
            set
            {
                if (value != _fileCount)
                {
                    _fileCount = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FolderItemModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the folder item.</param>
        /// <param name="customName">The custom name of the folder.</param>
        /// <param name="customDescription">The custom description of the folder.</param>
        /// <param name="folderPath">The path of the folder.</param>
        public FolderItemModel(long id, string customName, string customDescription, string folderPath) : base(id)
        {
            _folderPath = folderPath;
            _customName = customName;
            _customDescription = customDescription;
            _fileCount = 0;
            _status = FolderStatus.Disconnected;
        }

        /// <summary>
        /// Converts the folder status to its string representation.
        /// </summary>
        /// <param name="status">The folder status.</param>
        /// <returns>A string representing the folder status.</returns>
        public static string StatusToString(FolderStatus status)
        {
            return status switch
            {
                FolderStatus.Available => "Available",
                FolderStatus.Disconnected => "Disconnected",
                _ => "Unknown"
            };
        }
    }
}
