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
    public class WinFileSystemIOService : IFileSystemIOService
    {
        public bool FileExists(string path)
        {
            return File.Exists(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

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

        public bool HasExtension(string path, string extension)
        {
            var ext = GetExtension(path);
            if (ext == null)
                return false;

            return ext.Equals(extension, StringComparison.InvariantCultureIgnoreCase);
        }

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
