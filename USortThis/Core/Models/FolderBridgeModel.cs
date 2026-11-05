using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Services;
using static USortThis.Core.Models.FileItemModel;

namespace USortThis.Core.Models
{
    public class FolderBridgeModel : ModelBase
    {
        private readonly ObservableCollection<FileItemModel> _pendingFiles = [];
        private readonly ObservableCollection<FileItemModel> _completedFiles = [];
        private readonly ObservableCollection<FileItemModel> _failedFiles = [];
        private readonly List<FilterRuleModel> _filterRules = [];
        private readonly IFileSystemIOService _fileInfoService;
        private readonly IDatabaseService _databaseService;
        private double _progress = 0;
        private long _pendingTransfers;
        private long _completedTransfers;
        private long _failedTransfers;
        private string _customName = string.Empty;
        private CancellationTokenSource _cancellationTokenSource = new();

        private FolderItemModel? _sourceFolder;
        private FolderItemModel? _destinationFolder;
        public FolderItemModel? SourceFolder
        {
            get => _sourceFolder;
            set
            {
                if (_sourceFolder == value) return;
                if (_sourceFolder != null)
                    _sourceFolder.PropertyChanged -= OnFolderPropertyChanged;
                _sourceFolder = value;
                if (_sourceFolder != null)
                    _sourceFolder.PropertyChanged += OnFolderPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(SourceFolderPath));
            }
        }
        public FolderItemModel? DestinationFolder
        {
            get => _destinationFolder;
            set
            {
                if (_destinationFolder == value) return;
                if (_destinationFolder != null)
                    _destinationFolder.PropertyChanged -= OnFolderPropertyChanged;
                _destinationFolder = value;
                if (_destinationFolder != null)
                    _destinationFolder.PropertyChanged += OnFolderPropertyChanged;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DestinationFolderPath));
            }
        }
        public ObservableCollection<FilterRuleModel> Routes { get; } = [];
        public ObservableCollection<FileTransferDetailModel> TransferDetails { get; } = [];
        public bool MoveSourceAfterCopy { get; set; }
        public ObservableCollection<string> DroppedPaths { get; } = [];
        public long PendingTransfers => Interlocked.Read(ref _pendingTransfers);
        public long CompletedTransfers => Interlocked.Read(ref _completedTransfers);
        public long FailedTransfers => Interlocked.Read(ref _failedTransfers);
        public long TotalTransfers => PendingTransfers + CompletedTransfers + FailedTransfers;
        public string SourceFolderPath => SourceFolder?.FolderPath ?? string.Empty;
        public string SourceDescription => string.IsNullOrWhiteSpace(SourceFolderPath)
            ? "Drop files or folders here"
            : SourceFolderPath;
        public string DestinationFolderPath => DestinationFolder?.FolderPath ?? string.Empty;
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

        public FolderBridgeModel(long id, string customName, FolderItemModel? source, FolderItemModel? destination,
            IFileSystemIOService fileInfoService, IDatabaseService databaseService) : base(id)
        {
            SourceFolder = source;
            DestinationFolder = destination;
            CustomName = customName;
            _fileInfoService = fileInfoService;
            _databaseService = databaseService;

            _pendingFiles.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(PendingTransfers));
            };
            _completedFiles.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(CompletedTransfers));
            };
            _failedFiles.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(FailedTransfers));
            };
        }

        private void OnFolderPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(FolderItemModel.FolderPath)) return;
            if (sender == SourceFolder)
            {
                OnPropertyChanged(nameof(SourceFolderPath));
                OnPropertyChanged(nameof(SourceDescription));
            }
            if (sender == DestinationFolder) OnPropertyChanged(nameof(DestinationFolderPath));
        }

        public IReadOnlyList<FilterRuleModel> GetFilterRules() => _filterRules.AsReadOnly();

        public void ResetTransferStatistics() => UpdateTransferStatistics(0, 0, 0);

        public void UpdateTransferStatistics(long pending, long completed, long failed)
        {
            if (Interlocked.Exchange(ref _pendingTransfers, pending) != pending)
                OnPropertyChanged(nameof(PendingTransfers));
            if (Interlocked.Exchange(ref _completedTransfers, completed) != completed)
                OnPropertyChanged(nameof(CompletedTransfers));
            if (Interlocked.Exchange(ref _failedTransfers, failed) != failed)
                OnPropertyChanged(nameof(FailedTransfers));
            OnPropertyChanged(nameof(TotalTransfers));
        }

        public void RestoreTransferHistory(IEnumerable<FileTransferDetailModel> details)
        {
            TransferDetails.Clear();
            foreach (var detail in details)
                TransferDetails.Add(detail);
            UpdateTransferStatistics(
                TransferDetails.LongCount(detail => detail.Status == CopyStatus.Pending || detail.Status == CopyStatus.InProgress),
                TransferDetails.LongCount(detail => detail.Status == CopyStatus.Completed),
                TransferDetails.LongCount(detail => detail.Status == CopyStatus.Failed));
        }

        public void PersistTransferDetail(FileTransferDetailModel detail)
        {
            try
            {
                _databaseService.SaveTransferHistory(Id, detail);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save transfer history for bridge {Id}: {exception.Message}");
            }
        }

        /// <summary>
        /// Enumerate pending files from the SourceFolder and apply this bridge's filter rules.
        /// Returns a list of file paths that matched. This method is cancellation-aware and
        /// reports enumeration errors via the optional <paramref name="errors"/> reporter.
        /// Semantics: if there are no rules the file is included. If there are rules, all
        /// rules must match for the file to be included (AND across rules). Each rule itself
        /// evaluates its criteria with AND semantics.
        /// </summary>
        public async Task<List<string>> GetPendingFilesAsync(IFileSystemIOService fs, CancellationToken cancellationToken, IProgress<FileEnumerationError>? errors = null)
        {
            var result = new List<string>();
            if (fs is null) return result;

            var sourceDir = SourceFolder?.FolderPath;
            var sourcePaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(sourceDir))
            {
                await foreach (var path in fs.EnumerateFilesAsync(sourceDir, searchOption: SearchOption.AllDirectories, errors: errors, cancellationToken: cancellationToken))
                    sourcePaths.Add(path);
            }

            var newDroppedPaths = DroppedPaths.ToList();
            foreach (var droppedFile in newDroppedPaths.Where(fs.FileExists))
                sourcePaths.Add(droppedFile);
            foreach (var droppedDirectory in newDroppedPaths.Where(fs.DirectoryExists))
            {
                await foreach (var path in fs.EnumerateFilesAsync(droppedDirectory, searchOption: SearchOption.AllDirectories, errors: errors, cancellationToken: cancellationToken))
                    sourcePaths.Add(path);
            }

            sourcePaths = sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            if (sourcePaths.Count == 0) return result;

            try
            {
                foreach (var filePath in sourcePaths)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var fi = fs.GetFileInfo(filePath);
                    var fileSize = fi?.Length ?? 0;
                    var creation = fi?.CreationTimeUtc ?? DateTime.MinValue;
                    var lastWrite = fi?.LastWriteTimeUtc ?? DateTime.MinValue;

                    var fileItem = new FileItemModel(0, filePath, fileSize, creation, lastWrite);

                    if (Routes.Any(route => route.Matches(fileItem)))
                        result.Add(filePath);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                try { errors?.Report(new FileEnumerationError(DateTime.UtcNow, sourceDir ?? string.Empty, ex)); } catch { }
            }

            return result;
        }

        public void AddRule(FilterRuleModel rule)
        {
            if (!Routes.Contains(rule))
            {
                Routes.Add(rule);
                _filterRules.Add(rule);
            }
        }

        public void RemoveRule(FilterRuleModel rule)
        {
            if (Routes.Remove(rule))
            {
                _filterRules.Remove(rule);
            }
        }

        public void AddPendingFile(FileItemModel item)
        {
            item.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(FileItemModel.Status))
                {
                    if (item.Status == CopyStatus.Completed)
                    {
                        lock (_pendingFiles)
                            _pendingFiles.Remove(item);
                        lock (_completedFiles)
                            _completedFiles.Add(item);
                    }
                    else if (item.Status == CopyStatus.Failed)
                    {
                        lock (_pendingFiles)
                            _pendingFiles.Remove(item);
                        lock (_failedFiles)
                            _failedFiles.Add(item);
                    }
                }
                else if (e.PropertyName == nameof(FileItemModel.Progress))
                {

                }
            };
        }

        public async void StartTransfersAsync()
        {
            while (_pendingFiles.Count > 0)
            {
                var item = _pendingFiles.First();

                CancellationToken token = _cancellationTokenSource.Token;
                await TransferFileAsync(item, token);
                if (token.IsCancellationRequested)
                    break;

                switch (item.Status)
                {
                    case CopyStatus.Completed:
                        lock (_pendingFiles)
                            _pendingFiles.Remove(item);
                        lock (_completedFiles)
                            _completedFiles.Add(item);
                        break;
                    case CopyStatus.Failed:
                        lock (_pendingFiles)
                            _pendingFiles.Remove(item);
                        lock (_failedFiles)
                            _failedFiles.Add(item);
                        break;
                }
            }
            _cancellationTokenSource.TryReset();
        }

        public void StopTransfer()
        {
            _cancellationTokenSource.Cancel();
        }

        private Task TransferFileAsync(FileItemModel item, CancellationToken cancellationToken)
        {
            return Task.Run(async () =>
            {
                const int maxAttempts = 3;
                const int delayBetweenAttempts = 1000;
                const int bufferSize = 81920; // 80 KB

                var fileName = _fileInfoService.GetFileName(item.FilePath);
                if (fileName == null)
                {
                    item.Status = CopyStatus.Failed;
                    item.FailureReason = FailedReason.FileNotFound;
                    _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
                    return;
                }

                // TODO: Consider adding a check to ensure that the source and destination folders are not the same, as this could lead to unexpected behavior or data loss.
                // TODO: Handle null SourceFolder when file is dropped directly into the queue without a source folder. In this case, the source path should be taken from the file's path itself.
                var sourceFolder = SourceFolder;
                var destinationFolder = DestinationFolder;
                if (destinationFolder == null)
                {
                    item.Status = CopyStatus.Failed;
                    item.FailureReason = FailedReason.Unknown;
                    _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
                    return;
                }
                if (sourceFolder == null && !DroppedPaths.Contains(item.FilePath))
                {
                    item.Status = CopyStatus.Failed;
                    item.FailureReason = FailedReason.Unknown;
                    _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
                    return;
                }
                var sourceFilePath = sourceFolder == null ? item.FilePath : Path.Combine(sourceFolder.FolderPath, fileName);
                var destinationFilePath = Path.Combine(destinationFolder.FolderPath, fileName);

                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        if (cancellationToken.IsCancellationRequested)
                        {
                            item.Status = CopyStatus.Failed;
                            item.FailureReason = FailedReason.Cancelled;
                            _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
                            return;
                        }

                        var sourceFileInfo = _fileInfoService.GetFileInfo(sourceFilePath);
                        if (sourceFileInfo == null)
                        {
                            item.Status = CopyStatus.Failed;
                            item.FailureReason = FailedReason.Unknown;
                            continue;
                        }
                        long sourceFileSize = sourceFileInfo.Length;
                        DateTime sourceFileLastWriteTime = sourceFileInfo.LastWriteTimeUtc;
                        long destinationFileSize = 0;
                        // Check if destination file exists and get its size for resuming
                        if (File.Exists(destinationFilePath))
                        {
                            var destinationFileInfo = _fileInfoService.GetFileInfo(destinationFilePath);
                            if (destinationFileInfo == null)
                            {
                                item.Status = CopyStatus.Failed;
                                item.FailureReason = FailedReason.Unknown;
                                continue;
                            }

                            destinationFileSize = destinationFileInfo.Length;
                            DateTime destinationFileLastWriteTime = destinationFileInfo.LastWriteTimeUtc;
                            if (sourceFileSize == destinationFileSize && sourceFileLastWriteTime <= destinationFileLastWriteTime)
                            {
                                // File already fully copied
                                item.Status = CopyStatus.Completed;
                                item.FailureReason = FailedReason.None;
                                return;
                            }
                            if (sourceFileLastWriteTime != destinationFileLastWriteTime)
                            {
                                // Source file has been modified since last copy, cannot resume
                                item.Status = CopyStatus.Failed;
                                item.FailureReason = FailedReason.ExistingDestinationFileNewer;
                                return;
                            }
                            if (sourceFileSize < destinationFileSize)
                            {
                                // Destination file is larger than source file, cannot resume
                                item.Status = CopyStatus.Failed;
                                item.FailureReason = FailedReason.ExistingDestinationFileBigger;
                                return;
                            }
                        }

                        using (FileStream sourceStream = new(sourceFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true))
                        {
                            using FileStream destinationStream = new(destinationFilePath, FileMode.Append, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
                            sourceStream.Seek(destinationFileSize, SeekOrigin.Begin);
                            byte[] buffer = new byte[bufferSize];
                            long totalBytes = sourceStream.Length;
                            long totalBytesCopied = 0;
                            int bytesRead;
                            while ((bytesRead = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                if (cancellationToken.IsCancellationRequested)
                                {
                                    item.Status = CopyStatus.Failed;
                                    item.FailureReason = FailedReason.Cancelled;
                                    _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
                                    return;
                                }
                                destinationStream.Write(buffer, 0, bytesRead);
                                totalBytesCopied += bytesRead;
                                int progress = (int)(totalBytesCopied * 100 / totalBytes);
                                item.Progress = progress;
                            }
                        }

                        item.Status = CopyStatus.Completed;
                        item.FailureReason = FailedReason.None;
                        break;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        item.Status = CopyStatus.Failed;
                        item.FailureReason = FailedReason.AccessDenied;
                    }
                    catch (FileNotFoundException)
                    {
                        item.Status = CopyStatus.Failed;
                        item.FailureReason = FailedReason.FileNotFound;
                    }
                    catch (IOException)
                    {
                    }
                    catch (Exception)
                    {
                        item.Status = CopyStatus.Failed;
                        item.FailureReason = FailedReason.Unknown;
                    }

                    if (attempt < maxAttempts)
                        await Task.Delay(delayBetweenAttempts, cancellationToken).ConfigureAwait(false);
                }
                _databaseService.UpdateCopyQueueRecordStatus(item.Id, (int)item.Status, (int)item.FailureReason);
            }, cancellationToken);
        }
    }
}
