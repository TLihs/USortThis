using System;
using System.IO;
using USortThis.Core.Data;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents the details of a file transfer, including its progress, status, and any error messages
    /// associated with the transfer.
    /// </summary>
    public sealed class FileTransferDetailModel : NotifyBase
    {
        private FileItemModel.CopyStatus _status;
        private double _progress;
        private string _failureReason = string.Empty;
        private DateTime _lastUpdatedUtc;
        private DateTime _lastProgressPersistedUtc = DateTime.MinValue;

        /// <summary>
        /// Gets the source path of the file being transferred.
        /// </summary>
        public string SourcePath { get; }

        /// <summary>
        /// Gets the destination path of the file transfer.
        /// </summary>
        public string DestinationPath { get; }

        /// <summary>
        /// Gets the file name of the file being transferred.
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Gets the relative path of the file being transferred.
        /// </summary>
        public string RelativePath { get; }

        /// <summary>
        /// Gets the destination folder path where the file is being transferred.
        /// </summary>
        public string DestinationFolderPath { get; }

        /// <summary>
        /// Gets the current status of the file transfer.
        /// </summary>
        public FileItemModel.CopyStatus Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        /// <summary>
        /// Determines whether the progress of the file transfer should be persisted, based on the time elapsed
        /// since the last persist operation.
        /// </summary>
        /// <param name="utcNow">The current UTC time.</param>
        /// <returns><c>true</c> if the progress should be persisted; otherwise, <c>false</c>.</returns>
        public bool ShouldPersistProgress(DateTime utcNow)
        {
            if (utcNow - _lastProgressPersistedUtc < TimeSpan.FromSeconds(2))
                return false;
            _lastProgressPersistedUtc = utcNow;
            return true;
        }

        /// <summary>
        /// Gets the current progress of the file transfer, as a percentage.
        /// </summary>
        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        /// <summary>
        /// Gets the reason for failure if the file transfer has failed.
        /// </summary>
        public string FailureReason
        {
            get => _failureReason;
            private set => SetProperty(ref _failureReason, value);
        }

        /// <summary>
        /// Gets the last updated time of the file transfer details.
        /// </summary>
        public DateTime LastUpdatedUtc
        {
            get => _lastUpdatedUtc;
            private set => SetProperty(ref _lastUpdatedUtc, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileTransferDetailModel"/> class with the specified
        /// source and destination paths, relative path, status, progress, failure reason, and last updated time.
        /// </summary>
        /// <param name="sourcePath">The source path of the file being transferred.</param>
        /// <param name="destinationFilePath">The destination file path of the transfer.</param>
        /// <param name="relativePath">The relative path of the file being transferred.</param>
        /// <param name="status">The initial status of the file transfer. Default is <see cref="FileItemModel.CopyStatus.Pending"/>.</param>
        /// <param name="progress">The initial progress of the file transfer. Default is 0.</param>
        /// <param name="failureReason">The initial failure reason, if any. Default is an empty string.</param>
        /// <param name="lastUpdatedUtc">The initial last updated time. Default is null, which sets the value to the current UTC time.</param>
        public FileTransferDetailModel(string sourcePath, string destinationFilePath, string relativePath,
            FileItemModel.CopyStatus status = FileItemModel.CopyStatus.Pending, double progress = 0,
            string failureReason = "", DateTime? lastUpdatedUtc = null)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationFilePath;
            FileName = Path.GetFileName(sourcePath);
            RelativePath = relativePath;
            DestinationFolderPath = Path.GetDirectoryName(destinationFilePath) ?? string.Empty;
            Update(status, progress, failureReason, lastUpdatedUtc);
        }

        /// <summary>
        /// Updates the file transfer details with the specified status, progress, failure reason, and last updated time.
        /// </summary>
        /// <param name="status">The new status of the file transfer.</param>
        /// <param name="progress">The new progress of the file transfer.</param>
        /// <param name="failureReason">The new failure reason, if any.</param>
        /// <param name="lastUpdatedUtc">The new last updated time. Default is null, which sets the value to the current UTC time.</param>
        public void Update(FileItemModel.CopyStatus status, double progress, string failureReason, DateTime? lastUpdatedUtc = null)
        {
            Status = status;
            Progress = Math.Clamp(progress, 0, 100);
            FailureReason = failureReason;
            LastUpdatedUtc = lastUpdatedUtc ?? DateTime.UtcNow;
        }
    }
}
