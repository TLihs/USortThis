using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents the main window model.
    /// </summary>
    public class MainWindowModel : ModelBase
    {
        private ProfileItemModel? _activeProfile;

        /// <summary>
        /// Gets or sets the active profile.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindowModel"/> class.
        /// </summary>
        public MainWindowModel() : base(0)
        {

        }
    }
}
