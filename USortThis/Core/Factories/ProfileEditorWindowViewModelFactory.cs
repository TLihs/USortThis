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
        private readonly IFileItemModelFactory _fileItemModelFactory;
        private readonly IRuleTestInteractionService _ruleTestInteractionService;

        /// <summary>Initializes the factory with the dependencies required by profile-editor view models.</summary>
        /// <param name="localizationService">The service that supplies localized strings.</param>
        /// <param name="databaseService">The service used to persist profile changes.</param>
        /// <param name="fileItemModelFactory">The factory used to create sample-file models.</param>
        /// <param name="ruleTestInteractionService">The service used for rule-test dialogs.</param>
        public ProfileEditorWindowViewModelFactory(
            ILocalizationService localizationService,
            IDatabaseService databaseService,
            IFileItemModelFactory fileItemModelFactory,
            IRuleTestInteractionService ruleTestInteractionService)
        {
            _localizationService = localizationService;
            _databaseService = databaseService;
            _fileItemModelFactory = fileItemModelFactory;
            _ruleTestInteractionService = ruleTestInteractionService;
        }

        /// <summary>Creates a profile-editor view model for a profile list and optional profile.</summary>
        /// <param name="model">The profile list model associated with the editor.</param>
        /// <param name="profile">The profile to edit, or null to create a new profile.</param>
        public ProfileEditorWindowViewModel Create(ProfileListModel model, ProfileItemModel? profile = null)
        {
            return new ProfileEditorWindowViewModel(model, profile, _localizationService, _fileItemModelFactory, _ruleTestInteractionService);
        }
    }
}
