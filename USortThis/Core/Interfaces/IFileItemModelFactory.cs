using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFileItemModelFactory
    {
        /// <summary>Creates a file model from a file path.</summary>
        /// <param name="path">The path of the file to represent.</param>
        public FileItemModel Create(string path);
        /// <summary>Reconstructs a file model from persisted values.</summary>
        /// <param name="id">The persisted file identifier.</param>
        /// <param name="filePath">The file path.</param>
        /// <param name="fileSize">The file size in bytes.</param>
        /// <param name="creationTime">The file creation time.</param>
        /// <param name="lastWriteTime">The file's last-write time.</param>
        /// <param name="status">The persisted transfer status.</param>
        /// <param name="failedReason">The persisted transfer failure reason.</param>
        public FileItemModel LoadFromDatabase(long id, string filePath, long fileSize, DateTime creationTime, DateTime lastWriteTime, int status, int failedReason);
    }
}
