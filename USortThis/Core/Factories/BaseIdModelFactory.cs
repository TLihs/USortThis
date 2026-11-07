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

        /// <summary>Initializes the base factory with the identifier generator used by derived factories.</summary>
        /// <param name="idFactory">The source of model identifiers.</param>
        /// <exception cref="ArgumentNullException"><paramref name="idFactory"/> is null.</exception>
        protected BaseIdModelFactory(IIdFactory idFactory)
        {
            _idFactory = idFactory ?? throw new ArgumentNullException(nameof(idFactory));
        }
    }
}
