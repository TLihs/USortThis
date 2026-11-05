using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace USortThis.Core.Interfaces
{
    public interface IFileSystemIOService
    {
        public bool FileExists(string path);
        public bool DirectoryExists(string path);
        public DateTime? GetCreationTime(string path);
        public DateTime? GetLastWriteTime(string path);
        public long? GetFileSize(string path);
        public string? GetDirectoryName(string path);
        public string? GetExtension(string path);
        public string? GetFileName(string path);
        public string? GetFileNameWithoutExtension(string path);
        public bool HasExtension(string path, string extension);
        public bool CanAccessFile(string path);
        public FileInfo? GetFileInfo(string path);

        /// <summary>
        /// Enumerate files in a directory. Returns an empty sequence if directory doesn't exist
        /// or on error. Defaults to top-level files and '*' pattern.
        /// </summary>
        IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Asynchronously enumerate files in a directory. Use with `await foreach`.
        /// Returns an empty sequence on error or when directory doesn't exist.
        /// Honors cancellation via the <paramref name="cancellationToken"/>.
        /// </summary>
        IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, IProgress<USortThis.Core.Models.FileEnumerationError>? errors = null, CancellationToken cancellationToken = default);

        Task CopyAsync(string sourceFullPath, string destFullPath, long startPosition, bool ensureDirectory, IProgress<long>? progress, CancellationToken cancellationToken);
    }
}
