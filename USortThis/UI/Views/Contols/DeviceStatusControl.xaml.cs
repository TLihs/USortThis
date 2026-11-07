using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using USortThis.Core.Models;

namespace USortThis.UI.Views.Controls
{
    /// <summary>
    /// Interaction logic for DeviceStatusControl.xaml
    /// </summary>
    public partial class DeviceStatusControl : System.Windows.Controls.UserControl
    {
        private DeviceModel? _boundDevice;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceStatusControl"/> class.
        /// </summary>
        public DeviceStatusControl()
        {
            InitializeComponent();
            this.DataContextChanged += DeviceStatusControl_DataContextChanged;
        }

        /// <summary>
        /// Event handler that is called when the data context changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DeviceStatusControl_DataContextChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is DeviceModel dev)
            {
                BindDevice(dev);
            }
        }

        /// <summary>
        /// Binds the specified device to the control, updating the UI to reflect the device's properties.
        /// </summary>
        /// <param name="device">The device to bind to the control.</param>
        public void BindDevice(DeviceModel device)
        {
            if (device == null) return;

            _boundDevice = device;

            DeviceNameText.Text = device.DeviceName;

            // Find and set the device type badge
            if (this.FindName("DeviceTypeBadge") is TextBlock badge)
            {
                badge.Text = device.DeviceType.ToString();
            }

            string path = string.Empty;
            string extra = string.Empty;

            switch (device)
            {
                case InterfaceStorageDevice sd:
                    path = sd.DevicePath;
                    extra = $"Type: Local Drive";
                    Icon.Source = GetIconForDevice(device.DeviceType);
                    break;
                case InterfaceUsbDevice ud:
                    path = ud.DeviceIdentifier ?? "Unknown";
                    extra = string.IsNullOrEmpty(ud.DeviceIdentifier) ? "USB Device" : $"ID: {ud.DeviceIdentifier}";
                    Icon.Source = GetIconForDevice(device.DeviceType);
                    break;
                case InterfaceNetworkDevice nd:
                    path = nd.DevicePath ?? "Unknown";
                    extra = !string.IsNullOrEmpty(nd.IPAddress) ? $"IP: {nd.IPAddress}" : "Network Device";
                    Icon.Source = GetIconForDevice(device.DeviceType);
                    break;
                default:
                    Icon.Source = GetIconForDevice(device.DeviceType);
                    break;
            }

            DevicePathText.Text = path;
            DeviceExtraText.Text = extra;

            // Update connection status indicator
            UpdateConnectionStatus(device.IsConnected);

            // Subscribe to connection status changes
            if (device != null)
            {
                device.PropertyChanged -= Device_PropertyChanged;
                device.PropertyChanged += Device_PropertyChanged;
            }
        }

        /// <summary>
        /// Event handler that is called when a property of the device changes.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void Device_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceModel.IsConnected) && _boundDevice != null)
            {
                UpdateConnectionStatus(_boundDevice.IsConnected);
            }
        }

        /// <summary>
        /// Updates the visual indication of the device's connection status.
        /// </summary>
        /// <param name="isConnected">A boolean value indicating whether the device is connected.</param>
        private void UpdateConnectionStatus(bool isConnected)
        {
            if (this.FindName("StatusIndicator") is Ellipse indicator)
            {
                indicator.Fill = isConnected 
                    ? new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 205, 50))   // LimeGreen
                    : new SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 20, 60));  // Crimson
            }
        }

        /// <summary>
        /// Gets the icon for the specified device type.
        /// Currently, this method returns null.
        /// </summary>
        /// <param name="type">The device type.</param>
        /// <returns>An <see cref="ImageSource"/> representing the icon for the device type, or null.</returns>
        private ImageSource? GetIconForDevice(DeviceModel.DeviceTypes type)
        {
            // For simplicity return null; future: return pack icons or emoji-based approach
            return null;
        }
    }
}
