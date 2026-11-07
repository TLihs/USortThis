using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.UI.Views;

namespace USortThis.Core.Factories
{
    public class ProfileEditorWindowFactory : IProfileEditorWindowModelFactory
    {
        private readonly IProfileEditorWindowViewModelFactory _viewModelFactory;

        /// <summary>Initializes the factory with the profile-editor view-model factory.</summary>
        /// <param name="viewModelFactory">The factory used to create the editor's view model.</param>
        public ProfileEditorWindowFactory(IProfileEditorWindowViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }
        
        /// <summary>Creates a profile editor window and assigns a view model for the requested profile.</summary>
        /// <param name="model">The profile list model associated with the editor.</param>
        /// <param name="profile">The profile to edit, or null to create a new profile.</param>
        public ProfileEditorWindow Create(ProfileListModel model, ProfileItemModel? profile = null)
        {
            var vm = _viewModelFactory.Create(model, profile);
            var window = new ProfileEditorWindow
            {
                DataContext = vm
            };

            return window;
        }
    }
}
