using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Services
{
    /// <summary>
    /// WinFileSystemIOService provides an implementation of IFileSystemIOService using the Windows file system APIs.
    /// </summary>
    public class WinFileSystemIOService : IFileSystemIOService
    {
        /// <summary>
        /// Determines whether the file exists at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>True if the file exists; otherwise, false.</returns>
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        /// <summary>
        /// Determines whether the directory exists at the specified path.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <returns>True if the directory exists; otherwise, false.</returns>
        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        /// <summary>
        /// Gets the creation time of the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The creation time of the file; null if the file does not exist or an error occurs.</returns>
        public DateTime? GetCreationTime(string path)
        {
            try
            {
                return File.GetCreationTime(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the last write time of the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The last write time of the file; null if the file does not exist or an error occurs.</returns>
        public DateTime? GetLastWriteTime(string path)
        {
            try
            {
                return File.GetLastWriteTime(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the size of the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The size of the file in bytes; null if the file does not exist or an error occurs.</returns>
        public long? GetFileSize(string path)
        {
            try
            {
                return new FileInfo(path).Length;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the directory name of the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The directory name; null if the path is invalid or an error occurs.</returns>
        public string? GetDirectoryName(string path)
        {
            try
            {
                return Path.GetDirectoryName(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the extension of the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file extension without the leading dot; empty string if the file has no extension; null if an error occurs.</returns>
        public string? GetExtension(string path)
        {
            try
            {
                var ext = Path.GetExtension(path);
                if (string.IsNullOrWhiteSpace(ext))
                    return string.Empty;
                return ext.TrimStart('.').ToLowerInvariant();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the file name with extension from the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name with extension; null if the path is invalid or an error occurs.</returns>
        public string? GetFileName(string path)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the file name without extension from the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>The file name without extension; null if the path is invalid or an error occurs.</returns>
        public string? GetFileNameWithoutExtension(string path)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Determines whether the file has the specified extension.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <param name="extension">The extension to check for.</param>
        /// <returns>True if the file has the specified extension; otherwise, false.</returns>
        public bool HasExtension(string path, string extension)
        {
            var ext = GetExtension(path);
            if (ext == null)
                return false;

            return ext.Equals(extension, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Determines whether the file at the specified path can be accessed.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>True if the file can be accessed; otherwise, false.</returns>
        public bool CanAccessFile(string path)
        {
            try
            {
                using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets file information for the file at the specified path.
        /// </summary>
        /// <param name="path">The file path.</param>
        /// <returns>A FileInfo object containing file information; null if the path is invalid or an error occurs.</returns>
        public FileInfo? GetFileInfo(string path)
        {
            try
            {
                var fileInfo = new FileInfo(path);
                return fileInfo;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Enumerates the files in the directory at the specified path.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="searchPattern">The search pattern (optional).</param>
        /// <param name="searchOption">The search option (optional).</param>
        /// <returns>An enumerable collection of file paths.</returns>
        public IEnumerable<string> EnumerateFiles(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return Enumerable.Empty<string>();

            try
            {
                // Use Directory.EnumerateFiles to avoid creating a full list in memory.
                return Directory.EnumerateFiles(path, searchPattern, searchOption);
            }
            catch
            {
                // On any IO/permission errors return empty enumerable so callers can continue safely.
                return Enumerable.Empty<string>();
            }
        }

        /// <summary>
        /// Asynchronously enumerates the files in the directory at the specified path.
        /// </summary>
        /// <param name="path">The directory path.</param>
        /// <param name="searchPattern">The search pattern (optional).</param>
        /// <param name="searchOption">The search option (optional).</param>
        /// <param name="errors">A progress reporter for file enumeration errors (optional).</param>
        /// <param name="cancellationToken">A cancellation token (optional).</param>
        /// <returns>An async enumerable collection of file paths.</returns>
        public async IAsyncEnumerable<string> EnumerateFilesAsync(string path, string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly, IProgress<USortThis.Core.Models.FileEnumerationError>? errors = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
                yield break;

            IEnumerable<string> files;
            try
            {
                if (!Directory.Exists(path))
                    yield break;

                // Use Directory.EnumerateFiles to avoid allocating large arrays.
                files = Directory.EnumerateFiles(path, searchPattern, searchOption);
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is System.Security.SecurityException || ex is PathTooLongException)
            {
                try { errors?.Report(new USortThis.Core.Models.FileEnumerationError(DateTime.UtcNow, path, ex)); } catch { }
                yield break;
            }
            catch (Exception ex)
            {
                try { errors?.Report(new USortThis.Core.Models.FileEnumerationError(DateTime.UtcNow, path, ex)); } catch { }
                yield break;
            }

            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Yield the file path to the caller. Any exceptions occurring during the consumer's processing
                // will be observed by the consumer; we only report enumeration-related issues here.
                yield return file;

                // Give the caller scheduling point and opportunity to observe cancellation
                await Task.Yield();
            }
        }

        /// <summary>
        /// Asynchronously copies a file from the source path to the destination path, starting at the specified position.
        /// </summary>
        /// <param name="sourceFullPath">The source file path.</param>
        /// <param name="destFullPath">The destination file path.</param>
        /// <param name="startPosition">The position in the source file to start copying from.</param>
        /// <param name="ensureDirectory">Whether to ensure the destination directory exists.</param>
        /// <param name="progress">A progress reporter for the copy operation (optional).</param>
        /// <param name="cancellationToken">A cancellation token (optional).</param>
        /// <exception cref="FileNotFoundException">Thrown when the source file does not exist.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown when the destination directory does not exist and ensureDirectory is true.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when startPosition is less than 0 or greater than the source file length.</exception>
        public async Task CopyAsync(string sourceFullPath, string destFullPath, long startPosition, bool ensureDirectory, IProgress<long>? progress, CancellationToken cancellationToken)
        {
            // Ensure the source file exists
            if (!File.Exists(sourceFullPath))
            {
                throw new FileNotFoundException("Source file not found.", sourceFullPath);
            }

            // Ensure the destination directory exists if required
            if (ensureDirectory)
            {
                var destDir = Path.GetDirectoryName(destFullPath);
                if (!string.IsNullOrWhiteSpace(destDir) && !Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }
            }

            using var sourceStream = new FileStream(sourceFullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            using var destStream = new FileStream(destFullPath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            if (startPosition < 0 || startPosition > sourceStream.Length)
                throw new ArgumentOutOfRangeException(nameof(startPosition));

            sourceStream.Seek(startPosition, SeekOrigin.Begin);
            destStream.SetLength(startPosition);
            destStream.Seek(startPosition, SeekOrigin.Begin);

            byte[] buffer = new byte[81920]; // 80 KB buffer
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
            {
                await destStream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                progress?.Report(destStream.Position);
            }
        }
    }
}
