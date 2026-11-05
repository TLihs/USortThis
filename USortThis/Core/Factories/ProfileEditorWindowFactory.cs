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

        public ProfileEditorWindowFactory(IProfileEditorWindowViewModelFactory viewModelFactory)
        {
            _viewModelFactory = viewModelFactory;
        }
        
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
