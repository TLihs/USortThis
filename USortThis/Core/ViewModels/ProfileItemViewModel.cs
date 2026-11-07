using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;
using static USortThis.Core.ViewModels.FileItemViewModel;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// Represents the view model for a profile item.
    /// </summary>
    public class ProfileItemViewModel : ViewModelBase
    {
        private readonly ProfileItemModel _model;

        /// <summary>
        /// The display name of the profile. This forwards to the underlying model
        /// and is observed via the model's PropertyChanged event in the constructor.
        /// </summary>
    public string Name => _model.Name;
        public double Progress => _model.Progress;
        public ProfileItemModel.ProfileStatus Status => _model.Status;
        public bool IsRunning => Status is ProfileItemModel.ProfileStatus.WaitingForSource
            or ProfileItemModel.ProfileStatus.WaitingForDestination
            or ProfileItemModel.ProfileStatus.WaitingForProfile
            or ProfileItemModel.ProfileStatus.InProgress;
        public ProfileItemModel Profile => _model;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileItemViewModel"/> class.
        /// </summary>
        /// <param name="model">The profile item model.</param>
        /// <param name="localizationService">The localization service.</param>
        public ProfileItemViewModel(ProfileItemModel model, ILocalizationService localizationService) : base(localizationService)
        {
            _model = model;

            _model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProfileItemModel.Name))
                {
                    OnPropertyChanged(nameof(Name));
                }
                else if (e.PropertyName == nameof(ProfileItemModel.Progress))
                {
                    OnPropertyChanged(nameof(Progress));
                }
                else if (e.PropertyName == nameof(ProfileItemModel.Status))
                {
                    OnPropertyChanged(nameof(Status));
                    OnPropertyChanged(nameof(IsRunning));
                }
            };
        }
    }
}
