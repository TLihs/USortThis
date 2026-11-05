using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Interfaces
{
    public interface IIdFactory
    {
        long CreateId();
        string CreateIdString();
    }
}
