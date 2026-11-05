using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IFileItemViewModelFactory
    {
        public FileItemViewModel Create(FileItemModel model);
    }
}
