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
    public static class DeviceService
    {
        public static ObservableCollection<DeviceModel> InterfaceDevices { get; set; } = [];

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
