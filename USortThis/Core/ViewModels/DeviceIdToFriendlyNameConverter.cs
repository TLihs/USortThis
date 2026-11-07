using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;
using USortThis.Core.Models;
using USortThis.Core.Services;

namespace USortThis.Core.ViewModels
{
    /// <summary>
    /// Converter class that translates device identifiers to friendly device names for display purposes.
    /// </summary>
    public class DeviceIdToFriendlyNameConverter : IValueConverter
    {
        /// <summary>
        /// Converts the given device identifier to a friendly device name.
        /// </summary>
        /// <remarks>
        /// The method looks up the device identifier among known devices and returns the corresponding
        /// device name. If no match is found, the method falls back to returning the device identifier
        /// itself, enclosed in parentheses if it is empty or null.
        /// </remarks>
        /// <param name="value">The device identifier to convert.</param>
        /// <param name="targetType">The target type for the conversion (unused).</param>
        /// <param name="parameter">Additional parameters for the conversion (unused).</param>
        /// <param name="culture">The culture information (unused).</param>
        /// <returns>
        /// The friendly device name if a match is found; otherwise, the original device identifier.
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Thrown when attempting to convert back to the device identifier.
        /// </exception>
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

        /// <summary>
        /// Converts the given value back to the device identifier (not supported).
        /// </summary>
        /// <param name="value">The value to convert back (unused).</param>
        /// <param name="targetType">The target type for the conversion (unused).</param>
        /// <param name="parameter">Additional parameters for the conversion (unused).</param>
        /// <param name="culture">The culture information (unused).</param>
        /// <returns>
        /// The device identifier (not supported, always throws an exception).
        /// </returns>
        /// <exception cref="NotSupportedException">
        /// Always thrown to indicate that the conversion back to the device identifier is not supported.
        /// </exception>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
