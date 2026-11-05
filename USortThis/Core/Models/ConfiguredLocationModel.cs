using System;
using System.IO;
using System.Linq;

namespace USortThis.Core.Models
{
    public class ConfiguredLocationModel : ModelBase
    {
        private string _label;
        private string _path;
        private string _username = string.Empty;
        private string _domain = string.Empty;
        private bool _useCurrentWindowsCredentials = true;
        private bool _isConnected;

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

        public string LocationTypeLabel => LocationType switch
        {
            "Usb" => "USB",
            "Folder" => "Folder",
            var type => type
        };

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

        public string Category => "User-configured";
        public string AvailabilityLabel => IsConnected ? "Available" : "Unavailable";
        public string CredentialTarget => $"USortThis/{Id}";

        public ConfiguredLocationModel(long id, string label, string path) : base(id)
        {
            _label = label;
            _path = path;
        }
    }
}
