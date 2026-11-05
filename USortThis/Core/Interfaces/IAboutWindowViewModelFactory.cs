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
        public AboutWindowViewModel Create(AboutWindowModel model);
    }
}
