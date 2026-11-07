using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IFolderItemModelFactory
    {
        /// <summary>Creates a folder model from its path and user-defined details.</summary>
        /// <param name="folderPath">The folder's path.</param>
        /// <param name="customName">The user-defined folder name.</param>
        /// <param name="customDescription">The user-defined folder description.</param>
        public FolderItemModel Create(string folderPath, string customName, string customDescription);
        /// <summary>Reconstructs a folder model from persisted values.</summary>
        /// <param name="id">The persisted folder identifier.</param>
        /// <param name="folderPath">The folder's path.</param>
        /// <param name="customName">The user-defined folder name.</param>
        /// <param name="customDescription">The user-defined folder description.</param>
        public FolderItemModel LoadFromDatabase(long id, string folderPath, string customName, string customDescription);
    }
}
