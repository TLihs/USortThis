using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFolderItemModelFactory
    {
        public FolderItemModel Create(string folderPath, string customName, string customDescription);
        public FolderItemModel LoadFromDatabase(long id, string folderPath, string customName, string customDescription);
    }
}
