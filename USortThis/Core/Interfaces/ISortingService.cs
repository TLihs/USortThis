using System.Threading;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface ISortingService
    {
        /// <summary>
        /// Start sorting based on the provided profile. Reports progress as percent (0..100).
        /// </summary>
        Task StartAsync(ProfileItemModel profile, IProgress<int> progress, CancellationToken ct, IProgress<FileEnumerationError>? enumerationErrors = null,
            Func<string, string, Task<ProfileItemModel.ConflictHandlingMode>>? resolveConflict = null);

        Task RetryAsync(FolderBridgeModel bridge, FileTransferDetailModel detail, CancellationToken ct);

        Task<IReadOnlyList<TransferPreviewEntry>> PreviewAsync(ProfileItemModel profile, CancellationToken ct,
            IProgress<FileEnumerationError>? enumerationErrors = null);
    }
}
