using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Factories
{
    public abstract class BaseIdModelFactory
    {
        protected readonly IIdFactory _idFactory;

        protected BaseIdModelFactory(IIdFactory idFactory)
        {
            _idFactory = idFactory ?? throw new ArgumentNullException(nameof(idFactory));
        }
    }
}
