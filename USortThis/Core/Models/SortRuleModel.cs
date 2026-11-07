using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents a sorting rule model.
    /// </summary>
    public class SortRuleModel : ModelBase
    {
        private string _name;

        /// <summary>
        /// Gets or sets the name of the sorting rule.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (value != _name)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="SortRuleModel"/> class.
        /// </summary>
        /// <param name="id">The identifier of the sorting rule.</param>
        /// <param name="name">The name of the sorting rule.</param>
        public SortRuleModel(long id, string name) : base(id)
        {
            _name = name;
        }
    }
}
