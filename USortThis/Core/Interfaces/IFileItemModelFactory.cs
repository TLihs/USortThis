using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFileItemModelFactory
    {
        public FileItemModel Create(string path);
        public FileItemModel LoadFromDatabase(long id, string filePath, long fileSize, DateTime creationTime, DateTime lastWriteTime, int status, int failedReason);
    }
}
