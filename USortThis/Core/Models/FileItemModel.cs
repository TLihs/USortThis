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
    /// <summary>
    /// Represents a file item with properties for file handling and transfer status.
    /// </summary>
    public class FileItemModel : ModelBase
    {
        /// <summary>
        /// Enum representing the status of a copy operation.
        /// </summary>
        public enum CopyStatus
        {
            Pending,
            InProgress,
            Completed,
            Failed
        }

        /// <summary>
        /// Enum representing the reasons for a copy operation failure.
        /// </summary>
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

        /// <summary>
        /// Converts the <see cref="CopyStatus"/> enum to a string representation.
        /// </summary>
        /// <param name="status">The copy status.</param>
        /// <returns>A string representing the copy status.</returns>
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

        /// <summary>
        /// Converts the <see cref="FailedReason"/> enum to a string representation.
        /// </summary>
        /// <param name="reason">The failure reason.</param>
        /// <returns>A string representing the failure reason.</returns>
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

        /// <summary>
        /// Gets or sets the file path.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the file creation time.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the last write time of the file.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the file size.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the progress of the copy operation.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the status of the copy operation.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the reason for a copy operation failure.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="FileItemModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the model.</param>
        /// <param name="filePath">The path of the file.</param>
        /// <param name="fileSize">The size of the file.</param>
        /// <param name="createTime">The creation time of the file.</param>
        /// <param name="lastWriteTime">The last write time of the file.</param>
        /// <param name="copyStatus">The initial copy status of the file.</param>
        /// <param name="failedReason">The initial failure reason, if any.</param>
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
