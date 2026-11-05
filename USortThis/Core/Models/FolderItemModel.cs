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
    public class FolderItemModel : ModelBase
    {
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

        public FolderItemModel(long id, string customName, string customDescription, string folderPath) : base(id)
        {
            _folderPath = folderPath;
            _customName = customName;
            _customDescription = customDescription;
            _fileCount = 0;
            _status = FolderStatus.Disconnected;
        }

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
