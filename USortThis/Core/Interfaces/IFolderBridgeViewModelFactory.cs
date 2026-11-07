using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFolderBridgeViewModelFactory
    {
        /// <summary>Creates a bridge view model for the supplied bridge.</summary>
        /// <param name="model">The bridge to expose to the view.</param>
        public FolderBridgeViewModel Create(FolderBridgeModel model);
    }
}
