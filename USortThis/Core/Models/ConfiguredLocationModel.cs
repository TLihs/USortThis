using System;
using System.IO;
using System.Linq;

namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents a user-configured location, such as a folder or a network drive.
    /// </summary>
    public class ConfiguredLocationModel : ModelBase
    {
        private string _label;
        private string _path;
        private string _username = string.Empty;
        private string _domain = string.Empty;
        private bool _useCurrentWindowsCredentials = true;
        private bool _isConnected;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredLocationModel"/> class.
        /// </summary>
        /// <param name="id">The unique identifier for the location.</param>
        /// <param name="label">The display label for the location.</param>
        /// <param name="path">The file system path of the location.</param>
        public ConfiguredLocationModel(long id, string label, string path) : base(id)
        {
            _label = label;
            _path = path;
        }

        /// <summary>
        /// Gets or sets the label of the location.
        /// </summary>
        public string Label
        {
            get => _label;
            set
            {
                if (_label == value) return;
                _label = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the type of the location (e.g., Network, Usb, Local, Folder).
        /// </summary>
        public string LocationType
        {
            get
            {
                if (Path.StartsWith("\\\\", StringComparison.Ordinal))
                    return "Network";
                var root = System.IO.Path.GetPathRoot(Path);
                if (!string.IsNullOrEmpty(root) && root.Length >= 2 && root[1] == ':')
                    return DriveInfo.GetDrives().FirstOrDefault(drive => string.Equals(drive.Name, root, StringComparison.OrdinalIgnoreCase))?.DriveType switch
                    {
                        DriveType.Removable => "Usb",
                        DriveType.Network => "Network",
                        _ => "Local"
                    };
                return "Folder";
            }
        }

        /// <summary>
        /// Gets the display label for the location type.
        /// </summary>
        public string LocationTypeLabel => LocationType switch
        {
            "Usb" => "USB",
            "Folder" => "Folder",
            var type => type
        };

        /// <summary>
        /// Gets or sets the file system path of the location.
        /// </summary>
        public string Path
        {
            get => _path;
            set
            {
                if (_path == value) return;
                _path = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(LocationType));
                OnPropertyChanged(nameof(LocationTypeLabel));
            }
        }

        /// <summary>
        /// Gets or sets the username for accessing the location, if required.
        /// </summary>
        public string Username
        {
            get => _username;
            set
            {
                if (_username == value) return;
                _username = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the domain for accessing the location, if required.
        /// </summary>
        public string Domain
        {
            get => _domain;
            set
            {
                if (_domain == value) return;
                _domain = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the current Windows credentials for accessing the location.
        /// </summary>
        public bool UseCurrentWindowsCredentials
        {
            get => _useCurrentWindowsCredentials;
            set
            {
                if (_useCurrentWindowsCredentials == value) return;
                _useCurrentWindowsCredentials = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the location is currently connected and available.
        /// </summary>
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected == value) return;
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AvailabilityLabel));
            }
        }

        /// <summary>
        /// Gets the category of the location, which is always "User-configured" for this model.
        /// </summary>
        public string Category => "User-configured";

        /// <summary>
        /// Gets the availability label, indicating whether the location is "Available" or "Unavailable".
        /// </summary>
        public string AvailabilityLabel => IsConnected ? "Available" : "Unavailable";

        /// <summary>
        /// Gets the credential target string used for storing and retrieving credentials.
        /// </summary>
        public string CredentialTarget => $"USortThis/{Id}";
    }
}
