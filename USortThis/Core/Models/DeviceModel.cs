using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using USortThis.Core.Data;
using System.Diagnostics;

namespace USortThis.Core.Models
{
    public abstract class DeviceModel : ModelBase
    {
        private static long _nextId = -1;

        public enum DeviceTypes
        {
            Unknown,
            Local,
            Network,
            Usb
        }

        protected bool _isConnected = false;
        private string _deviceName = string.Empty;

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }
        public string DeviceName
        {
            get => _deviceName;
            set
            {
                if (_deviceName != value)
                {
                    _deviceName = value;
                    OnPropertyChanged();
                }
            }
        }

        public DeviceModel(long id, DeviceTypes deviceType, string deviceName) : base(id)
        {
            DeviceType = deviceType;
            DeviceName = deviceName;
        }

        public DeviceModel(DeviceTypes deviceType, string deviceName) : base(++_nextId)
        {
            DeviceType = deviceType;
            DeviceName = deviceName;
        }

        public DeviceTypes DeviceType { get; }

        // New helper: determine device type from a path string
        public static DeviceTypes GetDeviceTypeFromPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return DeviceTypes.Unknown;

            path = path.Trim();

            // UNC paths are network shares
            if (path.StartsWith("\\\\") || path.StartsWith("//"))
                return DeviceTypes.Network;

            try
            {
                // Get root (e.g. "C:\\")
                string? root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                    return DeviceTypes.Unknown;

                // Normalize drive name to include trailing backslash like DriveInfo.Name
                if (!root.EndsWith("\\"))
                    root += "\\";

                // Find drive info for matching root
                var drive = DriveInfo.GetDrives().FirstOrDefault(d => string.Equals(d.Name, root, StringComparison.OrdinalIgnoreCase));
                if (drive == null)
                    return DeviceTypes.Unknown;

                // Network drive mapping
                if (drive.DriveType == DriveType.Network)
                    return DeviceTypes.Network;

                // If drive is removable or fixed, check whether it's a USB storage device
                if (drive.DriveType == DriveType.Removable || drive.DriveType == DriveType.Fixed)
                {
                    try
                    {
                        // UsbStorageDetector returns entries with DriveLetter like "E:\\" (DriveInfo.Name)
                        var usbDrives = UsbStorageDetector.GetUsbDrives();
                        if (usbDrives != null)
                        {
                            foreach (var ud in usbDrives)
                            {
                                if (!string.IsNullOrEmpty(ud.DriveLetter) && string.Equals(ud.DriveLetter, drive.Name, StringComparison.OrdinalIgnoreCase))
                                    return DeviceTypes.Usb;
                            }
                        }
                    }
                    catch
                    {
                        // swallow and fall back to Local
                    }

                    // Not detected as USB -> consider local
                    return DeviceTypes.Local;
                }

                // Other drive types (CDRom, Unknown) considered Unknown
                return DeviceTypes.Unknown;
            }
            catch
            {
                return DeviceTypes.Unknown;
            }
        }

        public void StartConnectionStatusPolling()
        {
            Task.Run(() => ConnectionStatusPollingThread());
        }

        public void ConnectionStatusPollingThread()
        {
            while (true)
            {
                try
                {
                    CheckConnectionStatus();
                    System.Threading.Thread.Sleep(500);
                }
                catch (Exception ex)
                {
                    Debug.Print($"Error in ConnectionStatusPollingThread for device {DeviceName}: {ex.Message}");
                }
            }
        }

        public abstract void CheckConnectionStatus();
    }

    public class InterfaceStorageDevice : DeviceModel
    {
        public string DevicePath { get; set; }

        public InterfaceStorageDevice(string deviceName, string devicePath) : base(DeviceTypes.Local, deviceName)
        {
            DevicePath = devicePath;
        }

        public InterfaceStorageDevice(long id, string deviceName, string devicePath) : base(id, DeviceTypes.Local, deviceName)
        {
            DevicePath = devicePath;
        }

        public override void CheckConnectionStatus()
        {
            try
            {
                // Example implementation: Check if the device path exists
                bool pathExists = Directory.Exists(DevicePath);

                // Marshal property change to UI thread if needed
                if (pathExists != IsConnected)
                {
                    if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() =>
                        {
                            IsConnected = pathExists;
                        });
                    }
                    else
                    {
                        IsConnected = pathExists;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Error checking connection status for {DeviceName}: {ex.Message}");
                // On error, assume disconnected
                if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnected = false;
                    });
                }
                else
                {
                    IsConnected = false;
                }
            }
        }
    }

    public class InterfaceNetworkDevice : DeviceModel
    {
        private string _ipAddress; // Actual IP address or hostname for connection checks
        private string _devicePath; // Drive letter or UNC path for display

        public string IPAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The device path as shown in the GUI (drive letter like "Z:" or UNC path like "\\server\share").
        /// This is separate from IPAddress, which holds the actual IP/hostname for connection checks.
        /// </summary>
        public string DevicePath
        {
            get => _devicePath;
            set
            {
                if (_devicePath != value)
                {
                    _devicePath = value;
                    OnPropertyChanged();
                }
            }
        }

        // Credentials for network connection checks. Password is stored as plain string for simplicity;
        // for production consider SecureString or OS credential stores.
        public string? NetworkUsername { get; set; }
        public string? NetworkPassword { get; set; }
        public string? NetworkDomain { get; set; } // optional

        public InterfaceNetworkDevice(long id, string deviceName, string ipAddress, string? username = null, string? password = null, string? domain = null) : base(id, DeviceTypes.Network, deviceName)
        {
            _ipAddress = ExtractHostnameOrIp(ipAddress);
            _devicePath = ipAddress; // Store the original path for display
            NetworkUsername = username;
            NetworkPassword = password;
            NetworkDomain = domain;
        }

        public InterfaceNetworkDevice(string deviceName, string ipAddress, string? username = null, string? password = null, string? domain = null) : base(DeviceTypes.Network, deviceName)
        {
            _ipAddress = ExtractHostnameOrIp(ipAddress);
            _devicePath = ipAddress; // Store the original path for display
            NetworkUsername = username;
            NetworkPassword = password;
            NetworkDomain = domain;
        }

        /// <summary>
        /// Extract hostname or IP from a path (UNC path or drive letter).
        /// For UNC paths like "\\server\share", extracts "server".
        /// For drive letters like "Z:\", attempts to resolve via P/Invoke or returns the input.
        /// </summary>
        private static string ExtractHostnameOrIp(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            // UNC path: \\server\share or //server/share
            if (path.StartsWith("\\\\", StringComparison.OrdinalIgnoreCase) || path.StartsWith("//", StringComparison.OrdinalIgnoreCase))
            {
                string trimmed = path.TrimStart('\\', '/');
                var parts = trimmed.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length > 0)
                    return parts[0]; // Return server name
            }

            // Drive letter: Z:\ or Z:
            if (path.Length >= 2 && path[1] == ':')
            {
                try
                {
                    // Use P/Invoke to get the UNC path for a mapped network drive
                    var remoteName = new StringBuilder(260);
                    int size = remoteName.Capacity;
                    uint result = WNetGetConnection($"{path[0]}:", remoteName, ref size);

                    if (result == 0) // NO_ERROR
                    {
                        string remoteUNC = remoteName.ToString();
                        // Extract hostname from \\server\share format
                        string trimmed = remoteUNC.TrimStart('\\');
                        var parts = trimmed.Split(['\\']);
                        if (parts.Length > 0)
                            return parts[0];
                    }
                    else if (result == 2250) // ERROR_NOT_CONNECTED - not a mapped drive
                    {
                        Debug.Print($"Drive {path[0]} is not a mapped network drive");
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"WNetGetConnection failed for drive {path[0]}: {ex.Message}");
                }
            }

            return path;
        }

        // P/Invoke for WNetGetConnection
        [DllImport("mpr.dll", CharSet = CharSet.Auto)]
        private static extern uint WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

        public override void CheckConnectionStatus()
        {
            // Prefer credential-based check when credentials are supplied (best for SMB shares that require auth).
            try
            {
                if (!string.IsNullOrEmpty(NetworkUsername) && NetworkPassword != null)
                {
                    // Try to establish a short-lived authenticated connection to the IPC$ share.
                    // This does not map a persistent drive letter and is cleaned up after the check.
                    bool connected = TryConnectToIpcShare(IPAddress, NetworkUsername, NetworkPassword, NetworkDomain);
                    MarshalIsConnected(connected);
                    return;
                }

                // Fallback: simple ping using the extracted hostname/IP
                try
                {
                    using var ping = new System.Net.NetworkInformation.Ping();
                    var reply = ping.Send(IPAddress, 500);
                    MarshalIsConnected(reply.Status == System.Net.NetworkInformation.IPStatus.Success);
                }
                catch (Exception ex)
                {
                    Debug.Print($"Ping failed for {IPAddress}: {ex.Message}");
                    MarshalIsConnected(false);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"Connection status check failed for {DeviceName}: {ex.Message}");
                MarshalIsConnected(false);
            }
        }

        /// <summary>
        /// Helper to set IsConnected on the UI thread if needed.
        /// </summary>
        private void MarshalIsConnected(bool isConnected)
        {
            if (isConnected != IsConnected)
            {
                if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnected = isConnected;
                    });
                }
                else
                {
                    IsConnected = isConnected;
                }
            }
        }

        // Attempts to briefly authenticate to \\{ip}\IPC$ using provided credentials.
        // Returns true if authentication succeeded (NO_ERROR from WNetAddConnection2).
        private static bool TryConnectToIpcShare(string ip, string username, string password, string? domain)
        {
            string remote = $"\\\\{ip}\\IPC$";
            string user = username;

            // If domain provided and username doesn't already contain domain, prefix it.
            if (!string.IsNullOrEmpty(domain) && !username.Contains("\\"))
                user = domain + "\\" + username;

            var nr = new NETRESOURCE
            {
                dwType = RESOURCETYPE_DISK,
                lpLocalName = null,
                lpRemoteName = remote,
                lpProvider = null
            };

            // Try add connection
            int result = WNetAddConnection2(ref nr, password, user, 0);
            if (result == NO_ERROR)
            {
                // Clean up the temporary connection
                _ = WNetCancelConnection2(remote, 0, true);
                return true;
            }

            // Some servers accept username without domain or in different formats, try without domain prefix if domain was provided.
            if (!string.IsNullOrEmpty(domain) && user.Contains('\\'))
            {
                user = username; // try username only
                result = WNetAddConnection2(ref nr, password, user, 0);
                if (result == NO_ERROR)
                {
                    _ = WNetCancelConnection2(remote, 0, true);
                    return true;
                }
            }

            return false;
        }

        #region P/Invoke for network credential connect

        private const int NO_ERROR = 0;
        private const uint RESOURCETYPE_DISK = 0x00000001;

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NETRESOURCE
        {
            public uint dwScope;
            public uint dwType;
            public uint dwDisplayType;
            public uint dwUsage;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpLocalName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpRemoteName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpComment;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string? lpProvider;
        }

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2([In] ref NETRESOURCE lpNetResource, [MarshalAs(UnmanagedType.LPWStr)] string lpPassword, [MarshalAs(UnmanagedType.LPWStr)] string lpUserName, uint dwFlags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2([MarshalAs(UnmanagedType.LPWStr)] string lpName, uint dwFlags, bool fForce);

        #endregion
    }

    public class InterfaceUsbDevice : DeviceModel
    {
        // Optional identifier used to reliably identify the phone across drive-letter changes.
        // Example values you can store:
        // - full PnP device id (e.g. "USB\\VID_18D1&PID_4EE7\\0123456789ABCDEF")
        // - VID+PID substring (e.g. "VID_18D1&PID_4EE7")
        // - device serial extracted from PnPDeviceID
        private string? _deviceIdentifier;

        public string? DeviceIdentifier
        {
            get => _deviceIdentifier;
            set
            {
                if (_deviceIdentifier != value)
                {
                    _deviceIdentifier = value;
                    OnPropertyChanged();
                }
            }
        }

        public InterfaceUsbDevice(long id, string deviceName, string? deviceIdentifier = null) : base(id, DeviceTypes.Usb, deviceName)
        {
            DeviceIdentifier = deviceIdentifier;
        }

        public InterfaceUsbDevice(string deviceName, string? deviceIdentifier = null) : base(DeviceTypes.Usb, deviceName)
        {
            DeviceIdentifier = deviceIdentifier;
        }

        public override void CheckConnectionStatus()
        {
            try
            {
                // Query USB storage drives (fast, no WMI).
                var usbDrives = UsbStorageDetector.GetUsbDrives().ToList();

                // No USB storage drives -> not connected
                if (usbDrives.Count == 0)
                {
                    MarshalIsConnected(false);
                    return;
                }

                bool match = false;

                foreach (var drive in usbDrives)
                {
                    // If the caller provided a DeviceIdentifier, prefer that.
                    if (!string.IsNullOrEmpty(DeviceIdentifier))
                    {
                        // Match against serial, vendor+product, product, vendor or volume label
                        if (!string.IsNullOrEmpty(drive.SerialNumber) &&
                            drive.SerialNumber.Contains(DeviceIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        var vp = (drive.VendorId ?? string.Empty) + (drive.ProductId ?? string.Empty);
                        if (!string.IsNullOrEmpty(vp) &&
                            vp.Contains(DeviceIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(drive.ProductId) &&
                            drive.ProductId.Contains(DeviceIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(drive.VendorId) &&
                            drive.VendorId.Contains(DeviceIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(drive.VolumeLabel) &&
                            drive.VolumeLabel.Contains(DeviceIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                    else
                    {
                        // No DeviceIdentifier supplied — best-effort using the configured DeviceName.
                        if (!string.IsNullOrEmpty(drive.VolumeLabel) &&
                            drive.VolumeLabel.Contains(DeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(drive.ProductId) &&
                            drive.ProductId.Contains(DeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }

                        if (!string.IsNullOrEmpty(drive.VendorId) &&
                            drive.VendorId.Contains(DeviceName, StringComparison.OrdinalIgnoreCase))
                        {
                            match = true;
                            break;
                        }
                    }
                }

                MarshalIsConnected(match);
            }
            catch (Exception ex)
            {
                Debug.Print($"Error checking USB connection status for {DeviceName}: {ex.Message}");
                MarshalIsConnected(false);
            }
        }

        /// <summary>
        /// Helper to set IsConnected on the UI thread if needed.
        /// </summary>
        private void MarshalIsConnected(bool isConnected)
        {
            if (isConnected != IsConnected)
            {
                if (System.Windows.Application.Current?.Dispatcher != null && !System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnected = isConnected;
                    });
                }
                else
                {
                    IsConnected = isConnected;
                }
            }
        }
    }
}
