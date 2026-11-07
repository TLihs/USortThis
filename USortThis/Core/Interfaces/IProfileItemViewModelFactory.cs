using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IProfileItemViewModelFactory
    {
        /// <summary>Creates a profile-item view model for the supplied profile.</summary>
        /// <param name="model">The profile to expose to the view.</param>
        ProfileItemViewModel Create(ProfileItemModel model);
    }
}