using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Models
{
    public abstract class FilterRuleCriteriaBaseModel(long id) : ModelBase(id)
    {
        // Returns true when the given file satisfies the criteria
        public abstract bool Matches(FileItemModel file);
    }

    public class FilterRuleCriteriaModelNone(long id) : FilterRuleCriteriaBaseModel(id)
    {
        public override bool Matches(FileItemModel file)
        {
            // "None" criteria means no filtering: everything matches
            return true;
        }
    }

    // Match by file extension(s). Extensions can be provided comma-separated (e.g. ".jpg,.png")
    public class FilterRuleCriteriaModelExtension : FilterRuleCriteriaBaseModel
    {
        private string _extensions = string.Empty;
        private List<string> _parsedExtensions = [];
        private IFileSystemIOService _fileInfoService;

        // Comma-separated list of extensions (with or without leading dot). Case-insensitive.
        public string Extensions
        {
            get => _extensions;
            set
            {
                if (_extensions != value)
                {
                    _extensions = value;
                    RebuildExtensionCache();
                    OnPropertyChanged();
                }
            }
        }

        public FilterRuleCriteriaModelExtension(long id, string extensions, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
            Extensions = extensions;
        }

        private void RebuildExtensionCache()
        {
            _parsedExtensions = _extensions?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLowerInvariant().TrimStart('.'))
                .Where(e => e.Length > 0)
                .ToList() ?? [];
        }

        public override bool Matches(FileItemModel file)
        {
            if (_parsedExtensions.Count == 0)
                return true;

            var fileExt = (_fileInfoService.GetExtension(file.FilePath) ?? string.Empty)
                .Trim()
                .TrimStart('.')
                .ToLowerInvariant();
            return _parsedExtensions.Contains(fileExt);
        }
    }

    // Match by file name (not path). Supports simple substring matching or optional regex.
    public class FilterRuleCriteriaModelFileName : FilterRuleCriteriaBaseModel
    {
        private string _pattern;
        private bool _useRegex;
        private IFileSystemIOService _fileInfoService;

        public FilterRuleCriteriaModelFileName(long id, string pattern, bool useRegex, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
            _pattern = pattern;
            _useRegex = useRegex;
        }

        // Pattern used for matching. If UseRegex is false the pattern is treated as a case-insensitive substring.
        public string Pattern
        {
            get => _pattern;
            set
            {
                if (_pattern != value)
                {
                    _pattern = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool UseRegex
        {
            get => _useRegex;
            set
            {
                if (_useRegex != value)
                {
                    _useRegex = value;
                    OnPropertyChanged();
                }
            }
        }

        public override bool Matches(FileItemModel file)
        {
            if (string.IsNullOrEmpty(Pattern))
                return true;

            string name = _fileInfoService.GetFileNameWithoutExtension(file.FilePath) ?? string.Empty;
            if (string.IsNullOrEmpty(name))
                return false;

            // Use REGEX mode
            if (UseRegex)
            {
                try
                {
                    return Regex.IsMatch(name, Pattern, RegexOptions.IgnoreCase);
                }
                catch
                {
                    // Invalid regex → treat as no match (do not throw)
                    return false;
                }
            }

            // WILDCARD MODE => if pattern contains '*'
            if (Pattern.Contains('*'))
            {
                // Escape regex special chars EXCEPT wildcard
                string regex = Regex.Escape(Pattern)
                                   .Replace(@"\*", ".*"); // convert wildcard to regex

                // Match entire string
                regex = "^" + regex + "$";

                return Regex.IsMatch(name, regex, RegexOptions.IgnoreCase);
            }

            // EXACT MATCH MODE → no wildcard
            return name.Equals(Pattern, StringComparison.OrdinalIgnoreCase);
        }
    }

    // Match files with last write time (or creation time) >= MinDate
    public class FilterRuleCriteriaModelMinDate : FilterRuleCriteriaBaseModel
    {
        private DateTime _minDate;
        private bool _useCreationTime;
        private IFileSystemIOService _fileInfoService;

        public FilterRuleCriteriaModelMinDate(long id, DateTime minDate, bool useCreationTime, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
            _minDate = minDate;
            _useCreationTime = useCreationTime;
        }

        public DateTime MinDate
        {
            get => _minDate;
            set
            {
                if (_minDate != value)
                {
                    _minDate = value;
                    OnPropertyChanged();
                }
            }
        }

        // If true use CreationTime, otherwise use LastWriteTime
        public bool UseCreationTime
        {
            get => _useCreationTime;
            set
            {
                if (value != _useCreationTime)
                {
                    _useCreationTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public override bool Matches(FileItemModel file)
        {
            DateTime dt;
            try
            {
                var path = file.FilePath;
                if (UseCreationTime)
                {
                    var creationTime = _fileInfoService.GetCreationTime(path);
                    if (creationTime == null)
                        return false;
                    dt = creationTime.Value;
                }
                else
                {
                    var lastWriteTime = _fileInfoService.GetLastWriteTime(path);
                    if (lastWriteTime == null)
                        return false;
                    dt = lastWriteTime.Value;
                }
            }
            catch
            {
                return false; // if file cannot be accessed treat as not matching
            }

            return dt >= MinDate;
        }
    }

    // Match files with last write time (or creation time) <= MaxDate
    public class FilterRuleCriteriaModelMaxDate : FilterRuleCriteriaBaseModel
    {
        private DateTime _maxDate;
        private bool _useCreationTime;
        private IFileSystemIOService _fileInfoService;

        public FilterRuleCriteriaModelMaxDate(long id, DateTime maxDate, bool useCreationTime, IFileSystemIOService fileInfoService) : base(id)
        {
            _fileInfoService = fileInfoService;
            _maxDate = maxDate;
            _useCreationTime = useCreationTime;
        }

        public DateTime MaxDate
        {
            get => _maxDate;
            set
            {
                if (_maxDate != value)
                {
                    _maxDate = value;
                    OnPropertyChanged();
                }
            }
        }

        // If true use CreationTime, otherwise use LastWriteTime
        public bool UseCreationTime
        {
            get => _useCreationTime;
            set
            {
                if (value != _useCreationTime)
                {
                    _useCreationTime = value;
                    OnPropertyChanged();
                }
            }
        }

        public override bool Matches(FileItemModel file)
        {
            DateTime dt;
            try
            {
                var path = file.FilePath;
                if (UseCreationTime)
                {
                    var creationTime = _fileInfoService.GetCreationTime(path);
                    if (creationTime == null)
                        return false;
                    dt = creationTime.Value;
                }
                else
                {
                    var lastWriteTime = _fileInfoService.GetLastWriteTime(path);
                    if (lastWriteTime == null)
                        return false;
                    dt = lastWriteTime.Value;
                }
            }
            catch
            {
                return false;
            }

            return dt <= MaxDate;
        }
    }
}
