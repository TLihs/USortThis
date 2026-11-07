using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFolderBridgeModelFactory
    {
        /// <summary>Creates a bridge connecting the supplied source and destination folders.</summary>
        /// <param name="customName">The user-defined bridge name.</param>
        /// <param name="sourceFolder">The source folder, if configured.</param>
        /// <param name="destinationFolder">The destination folder, if configured.</param>
        public FolderBridgeModel Create(string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder);
        /// <summary>Reconstructs a bridge with its persisted identifier and folder endpoints.</summary>
        /// <param name="id">The persisted bridge identifier.</param>
        /// <param name="customName">The user-defined bridge name.</param>
        /// <param name="sourceFolder">The source folder, if configured.</param>
        /// <param name="destinationFolder">The destination folder, if configured.</param>
        public FolderBridgeModel LoadFromDatabase(long id, string customName, FolderItemModel? sourceFolder, FolderItemModel? destinationFolder);
    }
}
