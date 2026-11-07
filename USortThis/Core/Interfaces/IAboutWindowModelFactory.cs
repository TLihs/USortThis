using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Models;
using USortThis.UI.Views;

namespace USortThis.Core.Interfaces
{
    public interface IAboutWindowModelFactory
    {
        /// <summary>Creates the model used to populate the About window.</summary>
        public AboutWindowModel Create();
    }
}
