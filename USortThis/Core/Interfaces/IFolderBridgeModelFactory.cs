using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFolderBridgeModelFactory
    {
        public FolderBridgeModel Create(string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder);
        public FolderBridgeModel LoadFromDatabase(long id, string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder);
    }
}
