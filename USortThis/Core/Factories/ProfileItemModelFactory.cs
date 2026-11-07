using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Factories
{
    public class ProfileItemModelFactory : BaseIdModelFactory, IProfileItemModelFactory
    {
        /// <summary>Initializes the factory with the identifier generator used for new profiles.</summary>
        /// <param name="idFactory">The source of profile identifiers.</param>
        public ProfileItemModelFactory(IIdFactory idFactory) : base(idFactory)
        {
        }

        /// <summary>Creates a new profile model with a generated identifier.</summary>
        /// <param name="profileName">The profile's display name.</param>
        public ProfileItemModel Create(string profileName)
        {
            return new(_idFactory.CreateId(), profileName);
        }

        /// <summary>Reconstructs a profile model from persisted values.</summary>
        /// <param name="id">The persisted profile identifier.</param>
        /// <param name="profileName">The profile's display name.</param>
        public ProfileItemModel LoadFromDatabase(long id, string profileName)
        {
            return new(id, profileName);
        }
    }
}
