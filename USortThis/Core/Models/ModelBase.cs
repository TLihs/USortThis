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

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelBase"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the model.</param>
        public ModelBase(long id)
        {
            Id = id;
        }
    }
}
