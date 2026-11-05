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
        public ProfileItemModelFactory(IIdFactory idFactory) : base(idFactory)
        {
        }

        public ProfileItemModel Create(string profileName)
        {
            return new(_idFactory.CreateId(), profileName);
        }

        public ProfileItemModel LoadFromDatabase(long id, string profileName)
        {
            return new(id, profileName);
        }
    }
}
