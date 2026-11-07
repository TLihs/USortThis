using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using USortThis.Core.Interfaces;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Base class for filter rule criteria models.
    /// </summary>
    public abstract class FilterRuleCriteriaBaseModel(long id) : ModelBase(id)
    {
        /// <summary>
        /// Returns true when the given file satisfies the criteria.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True if the file matches the criteria, otherwise false.</returns>
        public abstract bool Matches(FileItemModel file);
    }

    /// <summary>
    /// Filter rule criteria that matches all files (no filtering).
    /// </summary>
    public class FilterRuleCriteriaModelNone(long id) : FilterRuleCriteriaBaseModel(id)
    {
        /// <summary>
        /// Always returns true, as "None" criteria means no filtering: everything matches.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True for all files.</returns>
        public override bool Matches(FileItemModel file)
        {
            return true;
        }
    }

    /// <summary>
    /// Filter rule criteria that matches files by their extension(s).
    /// </summary>
    // Match by file extension(s). Extensions can be provided comma-separated (e.g. ".jpg,.png")
    public class FilterRuleCriteriaModelExtension : FilterRuleCriteriaBaseModel
    {
        private string _extensions = string.Empty;
        private List<string> _parsedExtensions = [];
        private IFileSystemIOService _fileInfoService;

        /// <summary>
        /// Comma-separated list of extensions (with or without leading dot). Case-insensitive.
        /// </summary>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleCriteriaModelExtension"/> class.
        /// </summary>
        /// <param name="id">The identifier for the filter rule.</param>
        /// <param name="extensions">The file extensions to match.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
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

        /// <summary>
        /// Matches the file if its extension is in the specified list of extensions.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True if the file's extension matches, otherwise false.</returns>
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

    /// <summary>
    /// Filter rule criteria that matches files by their name (not path).
    /// </summary>
    // Match by file name (not path). Supports simple substring matching or optional regex.
    public class FilterRuleCriteriaModelFileName : FilterRuleCriteriaBaseModel
    {
        private string _pattern;
        private bool _useRegex;
        private IFileSystemIOService _fileInfoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleCriteriaModelFileName"/> class.
        /// </summary>
        /// <param name="id">The identifier for the filter rule.</param>
        /// <param name="pattern">The pattern used for matching file names.</param>
        /// <param name="useRegex">Indicates whether to use regex for matching.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
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

        /// <summary>
        /// Matches the file if its name matches the specified pattern.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True if the file's name matches the pattern, otherwise false.</returns>
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

    /// <summary>
    /// Filter rule criteria that matches files by their last write or creation time (minimum date).
    /// </summary>
    // Match files with last write time (or creation time) >= MinDate
    public class FilterRuleCriteriaModelMinDate : FilterRuleCriteriaBaseModel
    {
        private DateTime _minDate;
        private bool _useCreationTime;
        private IFileSystemIOService _fileInfoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleCriteriaModelMinDate"/> class.
        /// </summary>
        /// <param name="id">The identifier for the filter rule.</param>
        /// <param name="minDate">The minimum date for the last write or creation time.</param>
        /// <param name="useCreationTime">Indicates whether to use creation time instead of last write time.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
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

        /// <summary>
        /// Matches the file if its last write or creation time is greater than or equal to the specified minimum date.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True if the file's time is greater than or equal to the minimum date, otherwise false.</returns>
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

    /// <summary>
    /// Filter rule criteria that matches files by their last write or creation time (maximum date).
    /// </summary>
    // Match files with last write time (or creation time) <= MaxDate
    public class FilterRuleCriteriaModelMaxDate : FilterRuleCriteriaBaseModel
    {
        private DateTime _maxDate;
        private bool _useCreationTime;
        private IFileSystemIOService _fileInfoService;

        /// <summary>
        /// Initializes a new instance of the <see cref="FilterRuleCriteriaModelMaxDate"/> class.
        /// </summary>
        /// <param name="id">The identifier for the filter rule.</param>
        /// <param name="maxDate">The maximum date for the last write or creation time.</param>
        /// <param name="useCreationTime">Indicates whether to use creation time instead of last write time.</param>
        /// <param name="fileInfoService">The file system I/O service.</param>
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

        /// <summary>
        /// Matches the file if its last write or creation time is less than or equal to the specified maximum date.
        /// </summary>
        /// <param name="file">The file item model to check.</param>
        /// <returns>True if the file's time is less than or equal to the maximum date, otherwise false.</returns>
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
