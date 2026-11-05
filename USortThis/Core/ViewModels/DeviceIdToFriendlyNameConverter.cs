using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using USortThis.Core.Models;
using USortThis.Core.Services;

namespace USortThis.Core.ViewModels
{
    public class DeviceIdToFriendlyNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var id = value as string;
            if (string.IsNullOrEmpty(id))
                return "(unassigned)";

            // Try to find matching device by Id, DevicePath, DeviceIdentifier or IP
            var dev = DeviceService.InterfaceDevices.FirstOrDefault(d =>
            {
                try
                {
                    // compare Id
                    var sid = d.Id.ToString();
                    if (sid == id)
                        return true;
                }
                catch
                {

                }

                try
                {
                    if (d is InterfaceStorageDevice sd && string.Equals(sd.DevicePath, id, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                catch
                {

                }

                try
                {
                    if (d is InterfaceUsbDevice ud && !string.IsNullOrEmpty(ud.DeviceIdentifier) && ud.DeviceIdentifier == id)
                        return true;
                }
                catch
                {

                }

                try
                {
                    if (d is InterfaceNetworkDevice nd && !string.IsNullOrEmpty(nd.IPAddress) && nd.IPAddress == id)
                        return true;
                }
                catch
                {

                }

                // fallback match by DeviceName
                try
                {
                    if (!string.IsNullOrEmpty(d.DeviceName) && d.DeviceName == id)
                        return true;
                }
                catch
                {
                
                }

                return false;
            });

            if (dev != null)
                return dev.DeviceName;

            return id;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
