using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// ViewModel for the list of profiles.
    /// </summary>
    public class ProfileListViewModel : ViewModelBase
    {
        // map model instance -> viewmodel so removals are straightforward
        private readonly Dictionary<ProfileItemModel, ProfileItemViewModel> _map = [];
        private readonly ProfileListModel _model;

        /// <summary>
        /// Gets the items.
        /// </summary>
        public ObservableCollection<ProfileItemViewModel> Items { get; } = [];

        /// <summary>
        /// Gets or sets the selected item.
        /// </summary>
        public ProfileItemViewModel? SelectedItem
        {
            get => Items.FirstOrDefault(i => i.Profile == _model.SelectedProfile);
            set
            {
                if (value != null && value.Profile != _model.SelectedProfile)
                {
                    _model.SelectedProfile = value.Profile;
                    OnPropertyChanged(nameof(SelectedItem));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileListViewModel"/> class.
        /// </summary>
        /// <param name="model">The profile list model.</param>
        /// <param name="profileItemVmFactory">The factory to create profile item view models.</param>
        /// <param name="localizationService">The localization service.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="model"/>, <paramref name="profileItemVmFactory"/>,
        /// or <paramref name="localizationService"/> is null.
        /// </exception>
        public ProfileListViewModel(ProfileListModel model, IProfileItemViewModelFactory profileItemVmFactory, ILocalizationService localizationService)
            : base(localizationService)
        {
            ArgumentNullException.ThrowIfNull(model);
            ArgumentNullException.ThrowIfNull(profileItemVmFactory);

            // populate existing items
            foreach (var m in model.Profiles)
            {
                var vm = profileItemVmFactory.Create(m);
                Items.Add(vm);
                _map[m] = vm;
            }

            // keep synchronized with the model
            model.ItemAdded += (s, e) =>
            {
                var vm = profileItemVmFactory.Create(e.Item);
                Items.Add(vm);
                _map[e.Item] = vm;
            };

            model.ItemRemoved += (s, e) =>
            {
                if (_map.TryGetValue(e.Item, out var existingVm))
                {
                    Items.Remove(existingVm);
                    _map.Remove(e.Item);
                    OnPropertyChanged(nameof(SelectedItem));
                }
            };

            model.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ProfileListModel.SelectedProfile))
                    OnPropertyChanged(nameof(SelectedItem));
            };

            _model = model;
        }
    }
}
