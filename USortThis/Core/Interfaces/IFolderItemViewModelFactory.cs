using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFolderItemViewModelFactory
    {
        FolderItemViewModel Create(FolderItemModel model);
    }
}