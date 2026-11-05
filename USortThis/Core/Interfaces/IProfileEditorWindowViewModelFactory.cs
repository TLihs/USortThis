using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IProfileEditorWindowViewModelFactory
    {
        ProfileEditorWindowViewModel Create(ProfileListModel model, ProfileItemModel? profile = null);
    }
}