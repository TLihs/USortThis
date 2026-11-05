using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.UI.Views.Controls;

namespace USortThis.Core.Models
{
    public class FileItemModel : ModelBase
    {
        public enum CopyStatus
        {
            Pending,
            InProgress,
            Completed,
            Failed
        }

        public enum FailedReason
        {
            None,
            AccessDenied,
            ExistingDestinationFileNewer,
            ExistingDestinationFileBigger,
            FileNotFound,
            Cancelled,
            Unknown
        }

        public static string StatusToString(CopyStatus status)
        {
            return status switch
            {
                CopyStatus.Pending => "Pending",
                CopyStatus.InProgress => "InProgress",
                CopyStatus.Completed => "Completed",
                CopyStatus.Failed => "Failed",
                _ => "Unknown"
            };
        }

        public static string FailedReasonToString(FailedReason reason)
        {
            return reason switch
            {
                FailedReason.None => "NoError",
                FailedReason.AccessDenied => "AccessDenied",
                FailedReason.ExistingDestinationFileNewer => "ExistingDestinationFileNewer",
                FailedReason.ExistingDestinationFileBigger => "ExistingDestinationFileBigger",
                FailedReason.FileNotFound => "FileNotFound",
                FailedReason.Cancelled => "Cancelled",
                FailedReason.Unknown => "UnknownError",
                _ => "UnknownError"
            };
        }

        private string _filePath;
        private long _fileSize;
        private DateTime _creationTime;
        private DateTime _lastWriteTime;
        private double _progress;
        private CopyStatus _copyStatus;
        private FailedReason _failedReason;

        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged();
                }
            }
        }
        public DateTime CreationTime
        {
            get => _creationTime;
            set
            {
                if (_creationTime != value)
                {
                    _creationTime = value;
                    OnPropertyChanged();
                }
            }
        }
        public DateTime LastWriteTime
        {
            get => _lastWriteTime;
            set
            {
                if (value != _lastWriteTime)
                {
                    _lastWriteTime = value;
                    OnPropertyChanged();
                }
            }
        }
        public long FileSize
        {
            get => _fileSize;
            set
            {
                if (value != _fileSize)
                {
                    _fileSize = value;
                    OnPropertyChanged();
                }
            }
        }
        public double Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    OnPropertyChanged();
                }
            }
        }
        public CopyStatus Status
        {
            get => _copyStatus;
            set
            {
                if (_copyStatus != value)
                {
                    _copyStatus = value;
                    OnPropertyChanged();
                }
            }
        }

        public FailedReason FailureReason
        {
            get => _failedReason;
            set
            {
                if (_failedReason != value)
                {
                    _failedReason = value;
                    OnPropertyChanged();
                }
            }
        }

        public FileItemModel(long id, string filePath, long fileSize, DateTime createTime, DateTime lastWriteTime,
            CopyStatus copyStatus = CopyStatus.Pending, FailedReason failedReason = FailedReason.None) : base(id)
        {
            _filePath = filePath;
            _fileSize = fileSize;
            _creationTime = createTime;
            _lastWriteTime = lastWriteTime;
            _copyStatus = copyStatus;
            _failedReason = failedReason;
        }
    }
}
