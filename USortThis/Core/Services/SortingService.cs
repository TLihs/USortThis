using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    public class SortingService : ISortingService
    {
        private sealed class BridgeRunStatistics
        {
            public HashSet<string> CompletedEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
            public HashSet<string> FailedEntries { get; } = new(StringComparer.OrdinalIgnoreCase);
            public long Completed { get; set; }
            public long Failed { get; set; }
            public long Pending { get; set; }
        }

        private readonly IFileSystemIOService _fs;

        public SortingService(IFileSystemIOService fileSystemIOService)
        {
            _fs = fileSystemIOService ?? throw new ArgumentNullException(nameof(fileSystemIOService));
        }

        public async Task<IReadOnlyList<TransferPreviewEntry>> PreviewAsync(ProfileItemModel profile, CancellationToken ct,
            IProgress<FileEnumerationError>? enumerationErrors = null)
        {
            ArgumentNullException.ThrowIfNull(profile);
            var results = new List<TransferPreviewEntry>();

            foreach (var bridge in profile.GetFolderBridges())
            {
                ct.ThrowIfCancellationRequested();
                List<string> files;
                try
                {
                    files = await bridge.GetPendingFilesAsync(_fs, ct, enumerationErrors).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    enumerationErrors?.Report(new FileEnumerationError(DateTime.UtcNow, bridge.SourceFolderPath, exception));
                    continue;
                }

                foreach (var sourcePath in files)
                {
                    ct.ThrowIfCancellationRequested();
                    var fileInfo = _fs.GetFileInfo(sourcePath);
                    if (fileInfo == null) continue;
                    var file = new FileItemModel(0, sourcePath, fileInfo.Length, fileInfo.CreationTimeUtc, fileInfo.LastWriteTimeUtc);

                    foreach (var route in bridge.Routes.Where(route => route.Matches(file)))
                    {
                        if (string.IsNullOrWhiteSpace(route.DestinationPath)) continue;
                        var destination = BuildDestinationPath(bridge, route, sourcePath, fileInfo);
                        if (destination == null) continue;

                        var action = "Copy";
                        var existingKeepBoth = bridge.TransferDetails.FirstOrDefault(detail =>
                            string.Equals(detail.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(Path.GetDirectoryName(detail.DestinationPath), Path.GetDirectoryName(destination), StringComparison.OrdinalIgnoreCase) &&
                            detail.FailureReason.StartsWith("Kept both:", StringComparison.Ordinal));
                        if (existingKeepBoth != null)
                        {
                            destination = existingKeepBoth.DestinationPath;
                            action = "Keep both";
                        }

                        if (_fs.FileExists(destination))
                        {
                            var destinationInfo = _fs.GetFileInfo(destination);
                            var sameFile = destinationInfo?.Length == fileInfo.Length &&
                                _fs.GetLastWriteTime(destination)?.ToUniversalTime() == fileInfo.LastWriteTimeUtc.ToUniversalTime();
                            if (sameFile)
                            {
                                action = "Already up to date";
                            }
                            else
                            {
                                action = existingKeepBoth != null ? "Keep both" : profile.ConflictHandling switch
                                {
                                    ProfileItemModel.ConflictHandlingMode.Skip => "Skip (destination exists)",
                                    ProfileItemModel.ConflictHandlingMode.Overwrite => "Overwrite",
                                    ProfileItemModel.ConflictHandlingMode.KeepBoth => "Keep both",
                                    ProfileItemModel.ConflictHandlingMode.AskEachTime => "Conflict: ask during run",
                                    _ => "Copy"
                                };
                                if (existingKeepBoth == null && profile.ConflictHandling == ProfileItemModel.ConflictHandlingMode.KeepBoth)
                                    destination = GetUniqueDestinationPath(destination);
                            }
                        }

                        results.Add(new TransferPreviewEntry(bridge.CustomName, route.Name, sourcePath, destination, action));
                    }
                }
            }

            return results;
        }

        public async Task RetryAsync(FolderBridgeModel bridge, FileTransferDetailModel detail, CancellationToken ct)
        {
            ArgumentNullException.ThrowIfNull(bridge);
            ArgumentNullException.ThrowIfNull(detail);
            if (detail.Status != FileItemModel.CopyStatus.Failed)
                throw new InvalidOperationException("Only failed transfers can be retried.");
            if (string.Equals(Path.GetFullPath(detail.SourcePath), Path.GetFullPath(detail.DestinationPath), StringComparison.OrdinalIgnoreCase))
                throw new IOException("Source and destination paths must be different.");

            var notificationContext = SynchronizationContext.Current;
            PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.InProgress, 0, string.Empty, notificationContext);
            const int maxAttempts = 3;
            const int delayBetweenAttemptsMs = 500;

            for (var attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    var sourceInfo = _fs.GetFileInfo(detail.SourcePath)
                        ?? throw new FileNotFoundException("Source file could not be accessed.", detail.SourcePath);
                    var sourceLastWrite = _fs.GetLastWriteTime(detail.SourcePath);
                    var fileProgress = new Progress<long>(copiedBytes =>
                    {
                        var percent = sourceInfo.Length == 0 ? 100 : Math.Min(100, copiedBytes * 100d / sourceInfo.Length);
                        PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.InProgress, percent, string.Empty,
                            notificationContext, persist: detail.ShouldPersistProgress(DateTime.UtcNow));
                    });

                    await _fs.CopyAsync(detail.SourcePath, detail.DestinationPath, 0, true, fileProgress, ct).ConfigureAwait(false);
                    if (_fs.GetFileInfo(detail.DestinationPath)?.Length != sourceInfo.Length)
                        throw new IOException($"The copied file '{detail.DestinationPath}' does not match the source size.");

                    if (sourceLastWrite.HasValue)
                    {
                        try { File.SetLastWriteTimeUtc(detail.DestinationPath, sourceLastWrite.Value.ToUniversalTime()); }
                        catch { }
                    }

                    PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.Completed, 100, string.Empty, notificationContext);
                    PublishRetryStatistics(bridge, notificationContext);
                    return;
                }
                catch (OperationCanceledException)
                {
                    PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.Failed, detail.Progress, "Retry cancelled.", notificationContext);
                    PublishRetryStatistics(bridge, notificationContext);
                    throw;
                }
                catch (IOException exception) when (attempt < maxAttempts)
                {
                    PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.InProgress, detail.Progress, exception.Message, notificationContext);
                    await Task.Delay(delayBetweenAttemptsMs, ct).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.Failed, detail.Progress, exception.Message, notificationContext);
                    PublishRetryStatistics(bridge, notificationContext);
                    return;
                }
            }

            PublishTransferDetail(bridge, detail, FileItemModel.CopyStatus.Failed, detail.Progress, "Retry failed after multiple attempts.", notificationContext);
            PublishRetryStatistics(bridge, notificationContext);
        }

        private static void PublishRetryStatistics(FolderBridgeModel bridge, SynchronizationContext? notificationContext)
        {
            void Update() => bridge.UpdateTransferStatistics(
                bridge.TransferDetails.LongCount(item => item.Status is FileItemModel.CopyStatus.Pending or FileItemModel.CopyStatus.InProgress),
                bridge.TransferDetails.LongCount(item => item.Status == FileItemModel.CopyStatus.Completed),
                bridge.TransferDetails.LongCount(item => item.Status == FileItemModel.CopyStatus.Failed));

            if (notificationContext != null && SynchronizationContext.Current != notificationContext)
                notificationContext.Post(_ => Update(), null);
            else
                Update();
        }

        public async Task StartAsync(ProfileItemModel profile, IProgress<int> progress, CancellationToken ct, IProgress<FileEnumerationError>? enumerationErrors = null,
            Func<string, string, Task<ProfileItemModel.ConflictHandlingMode>>? resolveConflict = null)
        {
            ArgumentNullException.ThrowIfNull(profile);
            progress ??= new Progress<int>(_ => { }); // avoid null-checks below

            var notificationContext = SynchronizationContext.Current;
            var statistics = profile.GetFolderBridges().ToDictionary(bridge => bridge, _ => new BridgeRunStatistics());
            foreach (var bridge in statistics.Keys)
                PublishStatistics(bridge, statistics[bridge], notificationContext);

            while (!ct.IsCancellationRequested)
            {
                await ProcessPassAsync(profile, statistics, progress, ct, enumerationErrors, notificationContext, resolveConflict).ConfigureAwait(false);
                if (!ct.IsCancellationRequested)
                    await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
        }

        private async Task ProcessPassAsync(ProfileItemModel profile, Dictionary<FolderBridgeModel, BridgeRunStatistics> statistics, IProgress<int> progress, CancellationToken ct, IProgress<FileEnumerationError>? enumerationErrors, SynchronizationContext? notificationContext,
            Func<string, string, Task<ProfileItemModel.ConflictHandlingMode>>? resolveConflict)
        {
            var bridges = profile.GetFolderBridges() ?? Enumerable.Empty<FolderBridgeModel>();

            var fileEntries = new List<(FolderBridgeModel Bridge, FilterRuleModel Route, string SourcePath, string DestinationPath, string Key, bool Skip, bool KeepBoth)>();

            foreach (var bridge in bridges)
            {
                var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                try
                {
                    var pending = await bridge.GetPendingFilesAsync(_fs, cts.Token, enumerationErrors).ConfigureAwait(false);
                    foreach (var filePath in pending)
                    {
                        var fileInfo = _fs.GetFileInfo(filePath);
                        if (fileInfo == null) continue;
                        var file = new FileItemModel(0, filePath, fileInfo.Length, fileInfo.CreationTimeUtc, fileInfo.LastWriteTimeUtc);
                        foreach (var route in bridge.Routes.Where(route => route.Matches(file)))
                        {
                            if (string.IsNullOrWhiteSpace(route.DestinationPath)) continue;
                            var destination = BuildDestinationPath(bridge, route, filePath, fileInfo);
                            var skip = false;
                            var keepBoth = false;
                            var existingKeepBoth = bridge.TransferDetails.FirstOrDefault(detail =>
                                destination != null &&
                                string.Equals(detail.SourcePath, filePath, StringComparison.OrdinalIgnoreCase) &&
                                string.Equals(Path.GetDirectoryName(detail.DestinationPath), Path.GetDirectoryName(destination), StringComparison.OrdinalIgnoreCase) &&
                                detail.FailureReason.StartsWith("Kept both:", StringComparison.Ordinal));
                            if (existingKeepBoth != null)
                            {
                                destination = existingKeepBoth.DestinationPath;
                                keepBoth = true;
                            }
                            if (destination != null && _fs.FileExists(destination))
                            {
                                var destinationInfo = _fs.GetFileInfo(destination);
                                var sameFile = destinationInfo?.Length == fileInfo.Length &&
                                    _fs.GetLastWriteTime(destination)?.ToUniversalTime() == fileInfo.LastWriteTimeUtc.ToUniversalTime();
                                if (!sameFile)
                                {
                                    var existingDetail = bridge.TransferDetails.FirstOrDefault(detail =>
                                        string.Equals(detail.SourcePath, filePath, StringComparison.OrdinalIgnoreCase) &&
                                        string.Equals(detail.DestinationPath, destination, StringComparison.OrdinalIgnoreCase));
                                    if (existingDetail?.FailureReason == "Skipped because the destination already exists.")
                                    {
                                        skip = true;
                                    }
                                    else
                                    {
                                        var mode = profile.ConflictHandling;
                                        if (mode == ProfileItemModel.ConflictHandlingMode.AskEachTime)
                                            mode = resolveConflict == null
                                                ? ProfileItemModel.ConflictHandlingMode.Skip
                                                : await resolveConflict(filePath, destination).ConfigureAwait(false);

                                        if (mode == ProfileItemModel.ConflictHandlingMode.KeepBoth)
                                        {
                                            destination = GetUniqueDestinationPath(destination);
                                            keepBoth = true;
                                        }
                                        else if (mode == ProfileItemModel.ConflictHandlingMode.Skip)
                                        {
                                            skip = true;
                                        }
                                    }
                                }
                            }
                            var key = $"{route.Id}|{Path.GetFullPath(filePath)}|{fileInfo.Length}|{fileInfo.LastWriteTimeUtc.Ticks}";
                            if (destination != null)
                                fileEntries.Add((bridge, route, filePath, destination, key, skip, keepBoth));
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    try { enumerationErrors?.Report(new FileEnumerationError(DateTime.UtcNow, bridge.SourceFolder?.FolderPath ?? string.Empty, ex)); } catch { }
                    continue;
                }
                finally
                {
                    cts.Dispose();
                }
            }

            long totalBytes = 0;
            var fileSizes = new long[fileEntries.Count];
            var bridgeTotalBytes = statistics.Keys.ToDictionary(bridge => bridge, _ => 0L);
            var bridgeCopiedBytes = statistics.Keys.ToDictionary(bridge => bridge, _ => 0L);
            for (int i = 0; i < fileEntries.Count; i++)
            {
                var fi = _fs.GetFileInfo(fileEntries[i].SourcePath);
                var len = fi?.Length ?? 0;
                fileSizes[i] = len;
                totalBytes += len;
                bridgeTotalBytes[fileEntries[i].Bridge] += len;
            }

            foreach (var bridge in statistics.Keys.Where(bridge => bridgeTotalBytes[bridge] > 0))
                PublishProgress(bridge, 0, notificationContext);

            long totalCopiedBytes = 0;
            var successfulCopiesBySource = new Dictionary<(FolderBridgeModel Bridge, string SourcePath), int>();
            foreach (var bridge in statistics.Keys)
            {
                statistics[bridge].Completed = bridge.TransferDetails.LongCount(detail => detail.Status == FileItemModel.CopyStatus.Completed);
                statistics[bridge].Failed = bridge.TransferDetails.LongCount(detail => detail.Status == FileItemModel.CopyStatus.Failed);
                foreach (var entry in fileEntries.Where(entry => entry.Bridge == bridge))
                {
                    var detail = bridge.TransferDetails.FirstOrDefault(item =>
                        string.Equals(item.SourcePath, entry.SourcePath, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(item.DestinationPath, entry.DestinationPath, StringComparison.OrdinalIgnoreCase));
                    if (detail?.Status == FileItemModel.CopyStatus.Completed)
                        statistics[bridge].CompletedEntries.Add(entry.Key);
                    else if (detail?.Status == FileItemModel.CopyStatus.Failed)
                        statistics[bridge].FailedEntries.Add(entry.Key);
                }
                statistics[bridge].Pending = fileEntries.LongCount(entry => entry.Bridge == bridge);
                PublishStatistics(bridge, statistics[bridge], notificationContext);
            }
            var transferDetails = new Dictionary<(FolderBridgeModel Bridge, string SourcePath, string DestinationPath), FileTransferDetailModel>();
            foreach (var entry in fileEntries)
            {
                var relativePath = GetRelativePath(entry.Bridge, entry.SourcePath);
                var detail = GetOrAddTransferDetail(entry.Bridge, entry.SourcePath, entry.DestinationPath, relativePath, notificationContext);
                transferDetails[(entry.Bridge, entry.SourcePath, entry.DestinationPath)] = detail;
                if (detail.Status != FileItemModel.CopyStatus.Completed)
                    PublishTransferDetail(entry.Bridge, detail, FileItemModel.CopyStatus.Pending, detail.Progress,
                        entry.KeepBoth ? $"Kept both: {Path.GetFileName(entry.DestinationPath)}" : string.Empty, notificationContext);
            }
            const int maxAttempts = 3;
            const int delayBetweenAttemptsMs = 500;
            var cancellationRequested = false;

            for (int i = 0; i < fileEntries.Count; i++)
            {
                if (ct.IsCancellationRequested) break;

                var (bridge, route, sourceFullPath, destFullPath, entryKey, skip, keepBoth) = fileEntries[i];
                bool routeCopySucceeded = false;
                var transferDetail = transferDetails[(bridge, sourceFullPath, destFullPath)];
                if (skip)
                {
                    PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Completed, 100,
                        "Skipped because the destination already exists.", notificationContext);
                    if (statistics[bridge].CompletedEntries.Add(entryKey))
                        statistics[bridge].Completed++;
                    statistics[bridge].Pending = Math.Max(0, statistics[bridge].Pending - 1);
                    PublishStatistics(bridge, statistics[bridge], notificationContext);
                    totalCopiedBytes += fileSizes[i];
                    continue;
                }
                PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.InProgress, 0,
                    keepBoth ? $"Kept both: {Path.GetFileName(destFullPath)}" : string.Empty, notificationContext);

                // Per-file retry loop
                for (int attempt = 1; attempt <= maxAttempts; attempt++)
                {
                    try
                    {
                        ct.ThrowIfCancellationRequested();

                        var sourceInfo = _fs.GetFileInfo(sourceFullPath);
                        if (sourceInfo == null)
                        {
                            // treat as skipped / failed - advance counters conservatively
                            PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Failed, transferDetail.Progress, "Source file could not be accessed.", notificationContext);
                            totalCopiedBytes += fileSizes[i];
                            break;
                        }

                        long sourceLength = sourceInfo.Length;
                        DateTime? sourceLastWrite = _fs.GetLastWriteTime(sourceFullPath);

                        long destExistingLength = 0;
                        DateTime? destLastWrite = null;
                        if (_fs.FileExists(destFullPath))
                        {
                            var destInfo = _fs.GetFileInfo(destFullPath);
                            if (destInfo != null)
                            {
                                destExistingLength = destInfo.Length;
                                destLastWrite = _fs.GetLastWriteTime(destFullPath);
                            }
                        }

                        if (destExistingLength > sourceLength ||
                            (destExistingLength > 0 && (!sourceLastWrite.HasValue || !destLastWrite.HasValue ||
                                sourceLastWrite.Value.ToUniversalTime() != destLastWrite.Value.ToUniversalTime())))
                        {
                            destExistingLength = 0;
                            destLastWrite = null;
                        }

                        // If destination already matches source (size + last write), skip
                        if (destExistingLength == sourceLength && sourceLastWrite.HasValue && destLastWrite.HasValue &&
                            sourceLastWrite.Value.ToUniversalTime() == destLastWrite.Value.ToUniversalTime())
                        {
                            routeCopySucceeded = true;
                            PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Completed, 100,
                                keepBoth ? $"Kept both: {Path.GetFileName(destFullPath)}" : string.Empty, notificationContext);
                            totalCopiedBytes += sourceLength;
                            bridgeCopiedBytes[bridge] += sourceLength;
                            PublishProgress(bridge, bridgeCopiedBytes[bridge] * 100d / bridgeTotalBytes[bridge], notificationContext);
                            var percentAfterSkip = totalBytes == 0 ? 100 : (int)(totalCopiedBytes * 100 / totalBytes);
                            progress.Report(percentAfterSkip);
                            break;
                        }

                        // Ensure destination directory exists (best-effort)
                        try
                        {
                            var parent = Path.GetDirectoryName(destFullPath);
                            if (!string.IsNullOrWhiteSpace(parent) && !_fs.DirectoryExists(parent))
                            {
                                Directory.CreateDirectory(parent);
                            }
                        }
                        catch
                        {
                            throw;
                        }

                        // Prepare base for progress aggregation for this file
                        var baseTotalCopiedForThisFile = totalCopiedBytes;

                        // Per-file progress handler: maps file bytes -> overall percent
                        var fileProgress = new Progress<long>(reportedTotalBytesForFile =>
                        {
                            // reportedTotalBytesForFile is absolute bytes written to destination (including start offset)
                            var fileBytesCopiedSoFar = Math.Max(0, reportedTotalBytesForFile - destExistingLength);
                            var overallBytesSoFar = baseTotalCopiedForThisFile + Math.Min(sourceLength, fileBytesCopiedSoFar);
                            // update shared running total so next file starts from this value
                            totalCopiedBytes = overallBytesSoFar;
                            var bridgeBytesSoFar = bridgeCopiedBytes[bridge] + Math.Min(sourceLength, fileBytesCopiedSoFar);
                            PublishProgress(bridge, bridgeBytesSoFar * 100d / bridgeTotalBytes[bridge], notificationContext);
                            var pct = totalBytes == 0 ? 100 : (int)(overallBytesSoFar * 100 / totalBytes);
                            progress.Report(Math.Min(100, Math.Max(0, pct)));
                            PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.InProgress,
                                sourceLength == 0 ? 0 : fileBytesCopiedSoFar * 100d / sourceLength,
                                keepBoth ? $"Kept both: {Path.GetFileName(destFullPath)}" : string.Empty,
                                notificationContext, persist: transferDetail.ShouldPersistProgress(DateTime.UtcNow));
                        });

                        // Use the filesystem abstraction to perform the copy (supports resume, progress, cancellation)
                        await _fs.CopyAsync(
                            sourceFullPath,
                            destFullPath,
                            startPosition: destExistingLength,
                            ensureDirectory: true,
                            progress: fileProgress,
                            cancellationToken: ct).ConfigureAwait(false);

                        if (_fs.GetFileInfo(destFullPath)?.Length != sourceLength)
                            throw new IOException($"The copied file '{destFullPath}' does not match the source size.");

                        // After successful copy, ensure counters reflect full file copied
                        totalCopiedBytes = baseTotalCopiedForThisFile + sourceLength;
                        bridgeCopiedBytes[bridge] += sourceLength;
                        PublishProgress(bridge, bridgeCopiedBytes[bridge] * 100d / bridgeTotalBytes[bridge], notificationContext);
                        var percentFinal = totalBytes == 0 ? 100 : (int)(totalCopiedBytes * 100 / totalBytes);
                        progress.Report(Math.Min(100, Math.Max(0, percentFinal)));

                        // Optionally preserve last-write time if available
                        try
                        {
                            if (sourceLastWrite.HasValue)
                                File.SetLastWriteTimeUtc(destFullPath, sourceLastWrite.Value.ToUniversalTime());
                        }
                        catch { /* ignore */ }

                        routeCopySucceeded = true;
                        PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Completed, 100,
                            keepBoth ? $"Kept both: {Path.GetFileName(destFullPath)}" : string.Empty, notificationContext);
                        // successful copy -> break retry loop
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        cancellationRequested = true;
                        PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Pending, transferDetail.Progress, string.Empty, notificationContext);
                        break;
                    }
                    catch (UnauthorizedAccessException exception)
                    {
                        // unrecoverable for this file -> stop retrying
                        try { enumerationErrors?.Report(new FileEnumerationError(DateTime.UtcNow, sourceFullPath, new UnauthorizedAccessException($"Access denied copying '{sourceFullPath}' to '{destFullPath}'."))); } catch { }
                        PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Failed, transferDetail.Progress, exception.Message, notificationContext);
                        break;
                    }
                    catch (IOException exception) when (attempt < maxAttempts)
                    {
                        PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Pending, transferDetail.Progress, exception.Message, notificationContext);
                        // transient io error -> wait and retry
                        await Task.Delay(delayBetweenAttemptsMs, ct).ConfigureAwait(false);
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // unknown error -> stop retrying for this file
                        try { enumerationErrors?.Report(new FileEnumerationError(DateTime.UtcNow, sourceFullPath, ex)); } catch { }
                        PublishTransferDetail(bridge, transferDetail, FileItemModel.CopyStatus.Failed, transferDetail.Progress, ex.Message, notificationContext);
                        break;
                    }
                } // attempts

                if (routeCopySucceeded)
                {
                    var sourceKey = (bridge, sourceFullPath);
                    successfulCopiesBySource.TryGetValue(sourceKey, out var successfulCount);
                    successfulCopiesBySource[sourceKey] = successfulCount + 1;
                    if (statistics[bridge].CompletedEntries.Add(entryKey))
                        statistics[bridge].Completed++;
                    if (statistics[bridge].FailedEntries.Remove(entryKey))
                        statistics[bridge].Failed--;
                }
                else if (!cancellationRequested && statistics[bridge].FailedEntries.Add(entryKey))
                    statistics[bridge].Failed++;

                statistics[bridge].Pending = Math.Max(0, statistics[bridge].Pending - 1);
                PublishStatistics(bridge, statistics[bridge], notificationContext);

                if (cancellationRequested || ct.IsCancellationRequested)
                    break;
            } // files

            // Once a copy pass has completed, finish verified Move deletions even if Stop was requested.
            foreach (var bridge in bridges.Where(bridge => bridge.MoveSourceAfterCopy))
            {
                var bridgeEntries = fileEntries.Where(entry => entry.Bridge == bridge).GroupBy(entry => entry.SourcePath, StringComparer.OrdinalIgnoreCase);
                foreach (var sourceGroup in bridgeEntries)
                {
                    var destinationPaths = sourceGroup.Select(entry => entry.DestinationPath).ToList();
                    var sourceInfo = _fs.GetFileInfo(sourceGroup.Key);
                    var allDestinationsVerified = sourceInfo != null && sourceGroup.All(entry =>
                    {
                        var destinationInfo = _fs.GetFileInfo(entry.DestinationPath);
                        var destinationLastWrite = _fs.GetLastWriteTime(entry.DestinationPath);
                        var sourceLastWrite = _fs.GetLastWriteTime(sourceGroup.Key);
                        return destinationInfo?.Length == sourceInfo.Length &&
                            destinationLastWrite.HasValue && sourceLastWrite.HasValue &&
                            destinationLastWrite.Value.ToUniversalTime() == sourceLastWrite.Value.ToUniversalTime();
                    });
                    var destinationIsUnique = !fileEntries.Any(entry => entry.Bridge == bridge &&
                        !string.Equals(entry.SourcePath, sourceGroup.Key, StringComparison.OrdinalIgnoreCase) &&
                        destinationPaths.Contains(entry.DestinationPath, StringComparer.OrdinalIgnoreCase));
                    var destinationDiffersFromSource = destinationPaths.All(destinationPath =>
                        !string.Equals(Path.GetFullPath(destinationPath), Path.GetFullPath(sourceGroup.Key), StringComparison.OrdinalIgnoreCase));

                    if (successfulCopiesBySource.TryGetValue((bridge, sourceGroup.Key), out var successfulCount) &&
                        successfulCount == sourceGroup.Count() && allDestinationsVerified && destinationIsUnique &&
                        destinationDiffersFromSource && _fs.FileExists(sourceGroup.Key))
                    {
                        try { File.Delete(sourceGroup.Key); }
                        catch (Exception ex)
                        {
                            try { enumerationErrors?.Report(new FileEnumerationError(DateTime.UtcNow, sourceGroup.Key, ex)); } catch { }
                        }
                    }
                }
            }

            if (cancellationRequested || ct.IsCancellationRequested)
            {
                foreach (var bridge in statistics.Keys)
                {
                    statistics[bridge].Pending = 0;
                    PublishStatistics(bridge, statistics[bridge], notificationContext);
                }
                throw new OperationCanceledException(ct);
            }

            // Final progress report
            progress.Report(100);
        }

        private static void PublishStatistics(FolderBridgeModel bridge, BridgeRunStatistics statistics, SynchronizationContext? notificationContext)
        {
            void Update() => bridge.UpdateTransferStatistics(statistics.Pending, statistics.Completed, statistics.Failed);
            if (notificationContext != null && SynchronizationContext.Current != notificationContext)
                notificationContext.Post(_ => Update(), null);
            else
                Update();
        }

        private static void PublishProgress(FolderBridgeModel bridge, double value, SynchronizationContext? notificationContext)
        {
            void Update() => bridge.Progress = Math.Clamp(value, 0, 100);
            if (notificationContext != null && SynchronizationContext.Current != notificationContext)
                notificationContext.Post(_ => Update(), null);
            else
                Update();
        }

        private static FileTransferDetailModel GetOrAddTransferDetail(FolderBridgeModel bridge, string sourcePath, string destinationFilePath, string relativePath, SynchronizationContext? notificationContext)
        {
            FileTransferDetailModel? detail = null;
            void GetOrAdd()
            {
                detail = bridge.TransferDetails.FirstOrDefault(item =>
                    string.Equals(item.SourcePath, sourcePath, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(item.DestinationPath, destinationFilePath, StringComparison.OrdinalIgnoreCase));
                if (detail == null)
                {
                    detail = new FileTransferDetailModel(sourcePath, destinationFilePath, relativePath);
                    bridge.TransferDetails.Add(detail);
                }
            }

            if (notificationContext != null && SynchronizationContext.Current != notificationContext)
                notificationContext.Send(_ => GetOrAdd(), null);
            else
                GetOrAdd();
            return detail!;
        }

        private static void PublishTransferDetail(FolderBridgeModel bridge, FileTransferDetailModel detail, FileItemModel.CopyStatus status, double progress, string failureReason, SynchronizationContext? notificationContext, bool persist = true)
        {
            void Update()
            {
                detail.Update(status, progress, failureReason);
                if (persist)
                    bridge.PersistTransferDetail(detail);
            }
            if (notificationContext != null && SynchronizationContext.Current != notificationContext)
                notificationContext.Post(_ => Update(), null);
            else
                Update();
        }

        private static string GetRelativePath(FolderBridgeModel bridge, string sourcePath)
        {
            var sourceRoot = bridge.SourceFolder?.FolderPath;
            if (string.IsNullOrWhiteSpace(sourceRoot) || !IsUnderDirectory(sourcePath, sourceRoot))
                sourceRoot = bridge.DroppedPaths.Where(Directory.Exists).FirstOrDefault(path => IsUnderDirectory(sourcePath, path));
            return string.IsNullOrWhiteSpace(sourceRoot) ? Path.GetFileName(sourcePath) : Path.GetRelativePath(sourceRoot, sourcePath);
        }

        private string? BuildDestinationPath(FolderBridgeModel bridge, FilterRuleModel route, string sourcePath, FileInfo fileInfo)
        {
            try
            {
                var sourceRoot = bridge.SourceFolder?.FolderPath;
                if (sourceRoot == null || !IsUnderDirectory(sourcePath, sourceRoot))
                    sourceRoot = bridge.DroppedPaths
                        .Where(_fs.DirectoryExists)
                        .FirstOrDefault(path => IsUnderDirectory(sourcePath, path));
                var relative = sourceRoot == null
                    ? Path.GetFileName(sourcePath)
                    : Path.GetRelativePath(sourceRoot, sourcePath);
                var destinationDirectory = route.DestinationPath;
                if (route.PathMode == FilterRuleModel.DestinationPathMode.DateFolder)
                {
                    var date = route.UseCreationTime ? fileInfo.CreationTime : fileInfo.LastWriteTime;
                    var dateParts = route.DateFolderPattern.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries)
                        .Select(part => SanitizePathSegment(date.ToString(part)));
                    foreach (var datePart in dateParts)
                        destinationDirectory = Path.Combine(destinationDirectory, datePart);
                }

                if (route.PathMode == FilterRuleModel.DestinationPathMode.PreserveRelativePath &&
                    sourceRoot != null && !string.IsNullOrWhiteSpace(relative))
                    destinationDirectory = Path.Combine(destinationDirectory, Path.GetDirectoryName(relative) ?? string.Empty);

                return Path.Combine(destinationDirectory, Path.GetFileName(sourcePath));
            }
            catch (Exception ex)
            {
                try { System.Diagnostics.Debug.WriteLine(ex.Message); } catch { }
                return null;
            }
        }

        private string GetUniqueDestinationPath(string destinationPath)
        {
            var directory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
            var fileName = Path.GetFileNameWithoutExtension(destinationPath);
            var extension = Path.GetExtension(destinationPath);
            var candidate = Path.Combine(directory, $"{fileName} (copy){extension}");
            var copyNumber = 2;
            while (_fs.FileExists(candidate))
                candidate = Path.Combine(directory, $"{fileName} (copy {copyNumber++}){extension}");
            return candidate;
        }

        private static string SanitizePathSegment(string segment)
        {
            foreach (var invalid in Path.GetInvalidFileNameChars())
                segment = segment.Replace(invalid, '-');
            return segment;
        }

        private static bool IsUnderDirectory(string filePath, string directoryPath)
        {
            var relativePath = Path.GetRelativePath(Path.GetFullPath(directoryPath), Path.GetFullPath(filePath));
            return !Path.IsPathRooted(relativePath) && relativePath != ".." &&
                !relativePath.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
        }
    }
}
