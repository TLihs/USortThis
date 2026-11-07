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

        /// <summary>Retries a previously failed transfer for a bridge.</summary>
        /// <param name="bridge">The bridge associated with the transfer.</param>
        /// <param name="detail">The transfer record to retry.</param>
        /// <param name="ct">The token used to cancel the retry.</param>
        Task RetryAsync(FolderBridgeModel bridge, FileTransferDetailModel detail, CancellationToken ct);

        /// <summary>Builds a preview of the transfers that would be performed for a profile.</summary>
        /// <param name="profile">The profile whose routes are evaluated.</param>
        /// <param name="ct">The token used to cancel preview generation.</param>
        /// <param name="enumerationErrors">Receives errors encountered while enumerating files.</param>
        /// <returns>The proposed transfer entries.</returns>
        Task<IReadOnlyList<TransferPreviewEntry>> PreviewAsync(ProfileItemModel profile, CancellationToken ct,
            IProgress<FileEnumerationError>? enumerationErrors = null);
    }
}
