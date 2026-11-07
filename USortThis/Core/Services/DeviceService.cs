using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Service class for managing devices and their connections.
    /// </summary>
    public static class DeviceService
    {
        /// <summary>
        /// Initializes the <see cref="DeviceService"/> class.
        /// </summary>
        static DeviceService()
        {
        }

        /// <summary>
        /// Gets the collection of interface devices.
        /// </summary>
        public static ObservableCollection<DeviceModel> InterfaceDevices { get; set; } = new();

        /// <summary>
        /// Adds a new device of the specified type.
        /// </summary>
        /// <param name="deviceType">The type of the device to add.</param>
        /// <param name="deviceName">The name of the device.</param>
        /// <param name="location">The location of the device.</param>
        /// <returns>The newly added device, or null if the device could not be added.</returns>
        /// <exception cref="ArgumentException">Thrown when the device type is unknown.</exception>
        public static DeviceModel? AddDevice(DeviceModel.DeviceTypes deviceType, string deviceName, string location)
        {
            DeviceModel? device = deviceType switch
            {
                DeviceModel.DeviceTypes.Local => new InterfaceStorageDevice(deviceName, location),
                DeviceModel.DeviceTypes.Network => new InterfaceNetworkDevice(deviceName, location),
                DeviceModel.DeviceTypes.Usb => new InterfaceUsbDevice(deviceName, null),
                _ => null,
            };

            if (device != null)
            {
                // Marshal to UI thread so ObservableCollection changes are visible
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher != null && !dispatcher.CheckAccess())
                {
                    dispatcher.Invoke(() =>
                    {
                        InterfaceDevices.Add(device);
                    });
                }
                else
                {
                    InterfaceDevices.Add(device);
                }
                device.StartConnectionStatusPolling();
            }

            return device;
        }

        /// <summary>
        /// Retrieves a device based on the given file path.
        /// </summary>
        /// <param name="path">The file path to lookup the device.</param>
        /// <returns>The device associated with the path, or null if no device is found.</returns>
        /// <exception cref="ArgumentException">Thrown when the path is null or empty.</exception>
        public static DeviceModel? GetDeviceFromPath(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path))
                    return null;

                var type = DeviceModel.GetDeviceTypeFromPath(path);
                if (type == DeviceModel.DeviceTypes.Unknown)
                    return null;

                // Normalize root when available
                string? root = Path.GetPathRoot(path);
                if (!string.IsNullOrEmpty(root) && !root.EndsWith('\\'))
                    root += '\\';

                // Try to find an existing device in InterfaceManager
                DeviceModel? found = null;

                if (type == DeviceModel.DeviceTypes.Local || type == DeviceModel.DeviceTypes.Usb)
                {
                    // Find storage device matching the drive root
                    found = InterfaceDevices
                        .FirstOrDefault(d => d is InterfaceStorageDevice sd && string.Equals(sd.DevicePath?.TrimEnd('\\'), root?.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase));
                }
                else if (type == DeviceModel.DeviceTypes.Network)
                {
                    // For UNC paths, extract server name (\\server\share)
                    string server = path.TrimStart('\\', '/');
                    var parts = server.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        server = parts[0];

                    found = InterfaceDevices
                        .FirstOrDefault(d => d is InterfaceNetworkDevice nd && (string.Equals(nd.DeviceName, server, StringComparison.OrdinalIgnoreCase) || string.Equals(nd.IPAddress, server, StringComparison.OrdinalIgnoreCase)));
                }

                if (found != null)
                    return found;

                // No existing device - create one via InterfaceManager.AddDevice
                string deviceName = root ?? path;
                if (type == DeviceModel.DeviceTypes.Network)
                {
                    // Use server name for device name if possible
                    string server = path.TrimStart('\\', '/');
                    var parts = server.Split(['\\', '/'], StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                        deviceName = parts[0];
                }

                var newDevice = AddDevice(type, deviceName, root ?? path);
                return newDevice;
            }
            catch
            {
                // Swallow errors - leave AssociatedDevice as null
                return null;
            }
        }
    }
}
