using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFileItemViewModelFactory
    {
        /// <summary>Creates a file-item view model for the supplied model.</summary>
        /// <param name="model">The file model to expose to the view.</param>
        public FileItemViewModel Create(FileItemModel model);
    }
}
