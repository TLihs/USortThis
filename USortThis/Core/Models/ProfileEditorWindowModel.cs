using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents the profile editor window model.
    /// </summary>
    public class ProfileEditorWindowModel : ModelBase
    {
        private readonly List<ProfileItemModel> _profiles = [];
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileEditorWindowModel"/> class.
        /// </summary>
        public ProfileEditorWindowModel() : base(0)
        {
        }
    }
}
