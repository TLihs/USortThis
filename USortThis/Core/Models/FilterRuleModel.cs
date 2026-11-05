using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace USortThis.Core.Models
{
    public class FilterRuleModel : ListModelBase<FilterRuleCriteriaBaseModel>
    {
        private readonly List<FilterRuleCriteriaBaseModel> _criterias;
        private string _name;
        private string _destinationPath = string.Empty;
        private string _dateFolderPattern = "yyyy-MM-dd";
        private bool _useCreationTime;

        public enum DestinationPathMode
        {
            PreserveRelativePath,
            DateFolder
        }

        private DestinationPathMode _destinationPathMode;

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

        public bool PreserveRelativePath
        {
            get => PathMode == DestinationPathMode.PreserveRelativePath;
            set => PathMode = value ? DestinationPathMode.PreserveRelativePath : DestinationPathMode.DateFolder;
        }

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

        public FilterRuleModel(long id, string name) : base(id)
        {
            _name = name;
            _criterias = [];
        }

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
