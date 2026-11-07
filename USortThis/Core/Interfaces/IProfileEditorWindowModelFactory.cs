using USortThis.Core.Models;
using USortThis.UI.Views;

namespace USortThis.Core.Interfaces
{
    public interface IProfileEditorWindowModelFactory
    {
        /// <summary>Creates the profile editor window for a profile list and optional profile.</summary>
        /// <param name="model">The profile list model associated with the editor.</param>
        /// <param name="profile">The profile to edit, or null to create a new profile.</param>
        public ProfileEditorWindow Create(ProfileListModel model, ProfileItemModel? profile = null);
    }
}
