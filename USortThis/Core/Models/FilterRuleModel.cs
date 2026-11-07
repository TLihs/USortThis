using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents a filter rule for sorting files.
    /// </summary>
    public class FilterRuleModel : ListModelBase<FilterRuleCriteriaBaseModel>
    {
        private readonly List<FilterRuleCriteriaBaseModel> _criterias;
        private string _name;
        private string _destinationPath = string.Empty;
        private string _dateFolderPattern = "yyyy-MM-dd";
        private bool _useCreationTime;

        public enum DestinationPathMode
        {
            /// <summary>
            /// Preserves the relative path of the file.
            /// </summary>
            PreserveRelativePath,

            /// <summary>
            /// Organizes files into date-based folders.
            /// </summary>
            DateFolder
        }

        private DestinationPathMode _destinationPathMode;

        /// <summary>
        /// Gets or sets the name of the filter rule.
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the destination path for the filtered files.
        /// </summary>
        public string DestinationPath
        {
            get => _destinationPath;
            set
            {
                if (_destinationPath != value)
                {
                    _destinationPath = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the mode for determining the destination path.
        /// </summary>
        public DestinationPathMode PathMode
        {
            get => _destinationPathMode;
            set
            {
                if (_destinationPathMode != value)
                {
                    _destinationPathMode = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PreserveRelativePath));
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to preserve the relative path.
        /// </summary>
        public bool PreserveRelativePath
        {
            get => PathMode == DestinationPathMode.PreserveRelativePath;
            set => PathMode = value ? DestinationPathMode.PreserveRelativePath : DestinationPathMode.DateFolder;
        }

        /// <summary>
        /// Gets or sets the date folder pattern for organizing files.
        /// </summary>
        public string DateFolderPattern
        {
            get => _dateFolderPattern;
            set
            {
                if (_dateFolderPattern != value)
                {
                    _dateFolderPattern = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the creation time of the files.
        /// </summary>
        public bool UseCreationTime
        {
            get => _useCreationTime;
            set
            {
                if (_useCreationTime != value)
                {
                    _useCreationTime = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a read-only view of the rule criteria.
        /// </summary>
        public IReadOnlyList<FilterRuleCriteriaBaseModel> Criterias => _criterias.AsReadOnly();

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the filter rule.</param>
        /// <param name="name">The name of the filter rule.</param>
        public FilterRuleModel(long id, string name) : base(id)
        {
            _name = name;
            _criterias = [];
        }

        /// <summary>
        /// Adds a new criteria to the filter rule.
        /// </summary>
        /// <param name="model">The filter rule criteria to add.</param>
        public void AddRuleCriteria(FilterRuleCriteriaBaseModel model)
        {
            try
            {
                _criterias.Add(model);
                OnItemAdded(model);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Removes a criteria from the filter rule.
        /// </summary>
        /// <param name="model">The filter rule criteria to remove.</param>
        public void RemoveRuleCriteria(FilterRuleCriteriaBaseModel model)
        {
            try
            {
                if (_criterias.Remove(model))
                    OnItemRemoved(model);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Returns true if the provided file matches this filter rule.
        /// A rule with no criteria matches everything.
        /// </summary>
        /// <param name="file">The file to evaluate.</param>
        /// <returns>true if the file matches the filter rule; otherwise, false.</returns>
        public bool Matches(FileItemModel file)
        {
            if (_criterias.Count == 0)
                return false;

            // All criteria must match (AND)
            foreach (var c in _criterias)
            {
                try
                {
                    if (!c.Matches(file))
                        return false;
                }
                catch
                {
                    // Treat exceptions as non-match
                    return false;
                }
            }

            return true;
        }
    }
}
