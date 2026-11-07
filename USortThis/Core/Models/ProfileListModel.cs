using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class ProfileListModel : ListModelBase<ProfileItemModel>
    {
        private readonly List<ProfileItemModel> _profiles;
        private ProfileItemModel? _selectedProfile = null;

        // Expose a read-only view for consumers (ViewModel) to enumerate initial items
        public IReadOnlyList<ProfileItemModel> Profiles => _profiles.AsReadOnly();
        public ProfileItemModel? SelectedProfile
        {
            get => _selectedProfile;
            set
            {
                if (_selectedProfile != value)
                {
                    _selectedProfile = value;
                    OnPropertyChanged(nameof(SelectedProfile));
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileListModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the profile list.</param>
        public ProfileListModel(long id) : base(id)
        {
            _profiles = new List<ProfileItemModel>();
        }

        /// <summary>
        /// Adds a profile to the list.
        /// </summary>
        /// <param name="model">The profile model to add.</param>
        public void AddProfile(ProfileItemModel model)
        {
            try
            {
                _profiles.Add(model);
                OnItemAdded(model);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Removes a profile from the list.
        /// </summary>
        /// <param name="model">The profile model to remove.</param>
        public void RemoveProfile(ProfileItemModel model)
        {
            try
            {
                if (_profiles.Remove(model))
                    OnItemRemoved(model);
            }
            catch
            {

            }
        }
    }
}
