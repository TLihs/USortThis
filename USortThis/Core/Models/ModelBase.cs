using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Data;

namespace USortThis.Core.Models
{
    public abstract class ModelBase : NotifyBase
    {
        public long Id { get; }

        public ModelBase(long id)
        {
            Id = id;
        }
    }
}
