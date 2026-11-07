using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Interfaces
{
    public interface IIdFactory
    {
        /// <summary>Creates a unique numeric identifier.</summary>
        long CreateId();
        /// <summary>Creates a unique identifier represented as a string.</summary>
        string CreateIdString();
    }
}
