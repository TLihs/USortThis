using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class ProfileEditorWindowModel : ModelBase
    {
        private readonly List<ProfileItemModel> _profiles = [];
        
        public ProfileEditorWindowModel() : base(0)
        {
        }
    }
}
