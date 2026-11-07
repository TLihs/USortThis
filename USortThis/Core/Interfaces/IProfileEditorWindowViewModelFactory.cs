using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IProfileEditorWindowViewModelFactory
    {
        /// <summary>Creates a profile-editor view model for a profile list and optional profile.</summary>
        /// <param name="model">The profile list model associated with the editor.</param>
        /// <param name="profile">The profile to edit, or null to create a new profile.</param>
        ProfileEditorWindowViewModel Create(ProfileListModel model, ProfileItemModel? profile = null);
    }
}