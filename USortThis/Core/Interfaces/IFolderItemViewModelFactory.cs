using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFolderItemViewModelFactory
    {
        /// <summary>Creates a folder-item view model for the supplied folder.</summary>
        /// <param name="model">The folder to expose to the view.</param>
        FolderItemViewModel Create(FolderItemModel model);
    }
}