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
    /// <summary>
    /// The FolderBridgeModel class is responsible for managing the transfer of files between a source and a destination folder.
    /// It handles the routing of files based on defined filter rules and maintains the transfer statistics.
    /// </summary>
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
        /// <summary>
        /// Gets or sets the source folder for the transfer.
        /// </summary>
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
        /// <summary>
        /// Gets or sets the destination folder for the transfer.
        /// </summary>
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
        /// <summary>
        /// The collection of routing rules applied to files in this folder bridge.
        /// </summary>
        public ObservableCollection<FilterRuleModel> Routes { get; } = [];
        /// <summary>
        /// Details of the file transfer operations.
        /// </summary>
        public ObservableCollection<FileTransferDetailModel> TransferDetails { get; } = [];
        /// <summary>
        /// Indicates whether to move the source files to the destination after copying.
        /// </summary>
        public bool MoveSourceAfterCopy { get; set; }
        /// <summary>
        /// The collection of file paths that have been dropped for transfer.
        /// </summary>
        public ObservableCollection<string> DroppedPaths { get; } = [];
        /// <summary>
        /// The number of transfers pending.
        /// </summary>
        public long PendingTransfers => Interlocked.Read(ref _pendingTransfers);
        /// <summary>
        /// The number of transfers completed.
        /// </summary>
        public long CompletedTransfers => Interlocked.Read(ref _completedTransfers);
        /// <summary>
        /// The number of transfers that have failed.
        /// </summary>
        public long FailedTransfers => Interlocked.Read(ref _failedTransfers);
        /// <summary>
        /// The total number of transfers (pending, completed, and failed).
        /// </summary>
        public long TotalTransfers => PendingTransfers + CompletedTransfers + FailedTransfers;
        /// <summary>
        /// The file system path of the source folder.
        /// </summary>
        public string SourceFolderPath => SourceFolder?.FolderPath ?? string.Empty;
        /// <summary>
        /// A description of the source, used for display purposes.
        /// </summary>
        public string SourceDescription => string.IsNullOrWhiteSpace(SourceFolderPath)
            ? "Drop files or folders here"
            : SourceFolderPath;
        /// <summary>
        /// The file system path of the destination folder.
        /// </summary>
        public string DestinationFolderPath => DestinationFolder?.FolderPath ?? string.Empty;
        /// <summary>
        /// The progress of the transfer operation, as a percentage.
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
        /// A custom name for the folder bridge, used for identification.
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
        /// Initializes a new instance of the <see cref="FolderBridgeModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for this folder bridge.</param>
        /// <param name="customName">A custom name for the folder bridge.</param>
        /// <param name="source">The source folder for file transfers.</param>
        /// <param name="destination">The destination folder for file transfers.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
        /// <param name="databaseService">The database service for storing transfer history.</param>
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

        /// <summary>
        /// Retrieves the filter rules applied to this folder bridge.
        /// </summary>
        /// <returns>A read-only list of filter rules.</returns>
        public IReadOnlyList<FilterRuleModel> GetFilterRules() => _filterRules.AsReadOnly();

        /// <summary>
        /// Resets the transfer statistics, setting pending, completed, and failed transfers to zero.
        /// </summary>
        public void ResetTransferStatistics() => UpdateTransferStatistics(0, 0, 0);

        /// <summary>
        /// Updates the transfer statistics for pending, completed, and failed transfers.
        /// </summary>
        /// <param name="pending">The new pending transfers count.</param>
        /// <param name="completed">The new completed transfers count.</param>
        /// <param name="failed">The new failed transfers count.</param>
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

        /// <summary>
        /// Restores the transfer history from a collection of file transfer details.
        /// </summary>
        /// <param name="details">The transfer details to restore.</param>
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

        /// <summary>
        /// Persists a single transfer detail to the database.
        /// </summary>
        /// <param name="detail">The transfer detail to persist.</param>
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
        /// Enumerates pending files from the SourceFolder and applies this bridge's filter rules.
        /// Returns a list of file paths that matched. This method is cancellation-aware and
        /// reports enumeration errors via the optional <paramref name="errors"/> reporter.
        /// Semantics: if there are no rules the file is included. If there are rules, all
        /// rules must match for the file to be included (AND across rules). Each rule itself
        /// evaluates its criteria with AND semantics.
        /// </summary>
        /// <param name="fs">The file system service to use for file enumeration.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <param name="errors">An optional progress reporter for file enumeration errors.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a list of pending file paths.</returns>
        /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
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

        /// <summary>
        /// Adds a new routing rule to the folder bridge.
        /// </summary>
        /// <param name="rule">The routing rule to add.</param>
        public void AddRule(FilterRuleModel rule)
        {
            if (!Routes.Contains(rule))
            {
                Routes.Add(rule);
                _filterRules.Add(rule);
            }
        }

        /// <summary>
        /// Removes a routing rule from the folder bridge.
        /// </summary>
        /// <param name="rule">The routing rule to remove.</param>
        public void RemoveRule(FilterRuleModel rule)
        {
            if (Routes.Remove(rule))
            {
                _filterRules.Remove(rule);
            }
        }

        /// <summary>
        /// Adds a file to the pending files collection for transfer.
        /// </summary>
        /// <param name="item">The file item to add to pending transfer.</param>
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

        /// <summary>
        /// Starts the asynchronous transfer of pending files to the destination.
        /// </summary>
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

        /// <summary>
        /// Stops the ongoing file transfer operation.
        /// </summary>
        public void StopTransfer()
        {
            _cancellationTokenSource.Cancel();
        }

        /// <summary>
        /// Transfers a file from the source to the destination asynchronously.
        /// </summary>
        /// <param name="item">The file item representing the file to transfer.</param>
        /// <param name="cancellationToken">A cancellation token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous transfer operation.</returns>
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
