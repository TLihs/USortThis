using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFolderBridgeViewModelFactory
    {
        public FolderBridgeViewModel Create(FolderBridgeModel model);
    }
}
