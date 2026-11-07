using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Models;
using USortThis.Core.ViewModels;

namespace USortThis.Core.Interfaces
{
    public interface IAboutWindowViewModelFactory
    {
        /// <summary>Creates an About-window view model for the supplied model.</summary>
        /// <param name="model">The model containing About-window data.</param>
        public AboutWindowViewModel Create(AboutWindowModel model);
    }
}
