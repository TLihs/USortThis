using System;
using System.IO;
using USortThis.Core.Data;

namespace USortThis.Core.Models
{
    public sealed class FileTransferDetailModel : NotifyBase
    {
        private FileItemModel.CopyStatus _status;
        private double _progress;
        private string _failureReason = string.Empty;
        private DateTime _lastUpdatedUtc;
        private DateTime _lastProgressPersistedUtc = DateTime.MinValue;

        public string SourcePath { get; }
        public string DestinationPath { get; }
        public string FileName { get; }
        public string RelativePath { get; }
        public string DestinationFolderPath { get; }

        public FileItemModel.CopyStatus Status
        {
            get => _status;
            private set => SetProperty(ref _status, value);
        }

        public bool ShouldPersistProgress(DateTime utcNow)
        {
            if (utcNow - _lastProgressPersistedUtc < TimeSpan.FromSeconds(2))
                return false;
            _lastProgressPersistedUtc = utcNow;
            return true;
        }

        public double Progress
        {
            get => _progress;
            private set => SetProperty(ref _progress, value);
        }

        public string FailureReason
        {
            get => _failureReason;
            private set => SetProperty(ref _failureReason, value);
        }

        public DateTime LastUpdatedUtc
        {
            get => _lastUpdatedUtc;
            private set => SetProperty(ref _lastUpdatedUtc, value);
        }

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

        public void Update(FileItemModel.CopyStatus status, double progress, string failureReason, DateTime? lastUpdatedUtc = null)
        {
            Status = status;
            Progress = Math.Clamp(progress, 0, 100);
            FailureReason = failureReason;
            LastUpdatedUtc = lastUpdatedUtc ?? DateTime.UtcNow;
        }
    }
}
