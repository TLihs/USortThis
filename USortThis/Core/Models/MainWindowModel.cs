using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class MainWindowModel : ModelBase
    {
        private ProfileItemModel? _activeProfile;

        public ProfileItemModel? ActiveProfile
        {
            get => _activeProfile;
            set
            {
                if (_activeProfile != value)
                {
                    _activeProfile = value;
                    OnPropertyChanged(nameof(ActiveProfile));
                }
            }
        }

        public MainWindowModel() : base(0)
        {

        }
    }
}
