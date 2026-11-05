using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using USortThis.Core.Services;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Factories
{
    public class ProfileEditorWindowViewModelFactory : IProfileEditorWindowViewModelFactory
    {
        private readonly ILocalizationService _localizationService;
        private readonly IDatabaseService _databaseService;

        public ProfileEditorWindowViewModelFactory(ILocalizationService localizationService, IDatabaseService databaseService)
        {
            _localizationService = localizationService;
            _databaseService = databaseService;
        }

        public ProfileEditorWindowViewModel Create(ProfileListModel model, ProfileItemModel? profile = null)
        {
            return new ProfileEditorWindowViewModel(model, profile, _localizationService);
        }
    }
}
