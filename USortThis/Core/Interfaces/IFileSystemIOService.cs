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
        /// <summary>Determines whether a file exists at the specified path.</summary>
        /// <param name="path">The file path to check.</param>
        public bool FileExists(string path);
        /// <summary>Determines whether a directory exists at the specified path.</summary>
        /// <param name="path">The directory path to check.</param>
        public bool DirectoryExists(string path);
        /// <summary>Gets a file's creation time, or null when it cannot be read.</summary>
        /// <param name="path">The file path to inspect.</param>
        public DateTime? GetCreationTime(string path);
        /// <summary>Gets a file's last-write time, or null when it cannot be read.</summary>
        /// <param name="path">The file path to inspect.</param>
        public DateTime? GetLastWriteTime(string path);
        /// <summary>Gets a file's size in bytes, or null when it cannot be read.</summary>
        /// <param name="path">The file path to inspect.</param>
        public long? GetFileSize(string path);
        /// <summary>Gets the directory portion of a path, if present.</summary>
        /// <param name="path">The path to inspect.</param>
        public string? GetDirectoryName(string path);
        /// <summary>Gets the extension of a path.</summary>
        /// <param name="path">The path to inspect.</param>
        public string? GetExtension(string path);
        /// <summary>Gets the final file-name component of a path.</summary>
        /// <param name="path">The path to inspect.</param>
        public string? GetFileName(string path);
        /// <summary>Gets the final file-name component without its extension.</summary>
        /// <param name="path">The path to inspect.</param>
        public string? GetFileNameWithoutExtension(string path);
        /// <summary>Determines whether a path has the specified extension.</summary>
        /// <param name="path">The path to inspect.</param>
        /// <param name="extension">The extension to look for.</param>
        public bool HasExtension(string path, string extension);
        /// <summary>Determines whether the current process can access a file.</summary>
        /// <param name="path">The file path to check.</param>
        public bool CanAccessFile(string path);
        /// <summary>Gets file metadata, or null when it cannot be retrieved.</summary>
        /// <param name="path">The file path to inspect.</param>
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

        /// <summary>Copies a file asynchronously, optionally creating its destination directory and reporting progress.</summary>
        /// <param name="sourceFullPath">The full path of the source file.</param>
        /// <param name="destFullPath">The full path of the destination file.</param>
        /// <param name="startPosition">The source offset from which copying begins.</param>
        /// <param name="ensureDirectory">Whether to create the destination directory when needed.</param>
        /// <param name="progress">Receives the number of bytes copied, when supplied.</param>
        /// <param name="cancellationToken">The token used to cancel the copy.</param>
        Task CopyAsync(string sourceFullPath, string destFullPath, long startPosition, bool ensureDirectory, IProgress<long>? progress, CancellationToken cancellationToken);
    }
}
