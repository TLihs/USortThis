using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IProfileItemViewModelFactory
    {
        ProfileItemViewModel Create(ProfileItemModel model);
    }
}