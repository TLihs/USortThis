using USortThis.Core.Models;

namespace USortThis.Core.Interfaces
{
    public interface IProfileItemModelFactory
    {
        /// <summary>Creates a new profile model with the supplied name.</summary>
        /// <param name="profileName">The profile's display name.</param>
        public ProfileItemModel Create(string profileName);
        /// <summary>Reconstructs a profile model from persisted values.</summary>
        /// <param name="id">The persisted profile identifier.</param>
        /// <param name="profileName">The profile's display name.</param>
        public ProfileItemModel LoadFromDatabase(long id, string profileName);
    }
}
