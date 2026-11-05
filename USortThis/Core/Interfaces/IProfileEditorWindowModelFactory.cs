using USortThis.Core.Models;
using USortThis.UI.Views;

namespace USortThis.Core.Interfaces
{
    public interface IProfileEditorWindowModelFactory
    {
        public ProfileEditorWindow Create(ProfileListModel model, ProfileItemModel? profile = null);
    }
}
