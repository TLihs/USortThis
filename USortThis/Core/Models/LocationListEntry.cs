namespace USortThis.Core.Models
{
    /// <summary>
    /// Represents an entry in the location list, which can be either a discovered device or a configured location.
    /// </summary>
    public sealed class LocationListEntry
    {
        /// <summary>
        /// Gets the category of the location list entry.
        /// </summary>
        public string Category { get; }

        /// <summary>
        /// Gets the discovered device associated with the location list entry, if any.
        /// </summary>
        public DeviceModel? DiscoveredDevice { get; }

        /// <summary>
        /// Gets the configured location associated with the location list entry, if any.
        /// </summary>
        public ConfiguredLocationModel? ConfiguredLocation { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationListEntry"/> class for a discovered device.
        /// </summary>
        /// <param name="device">The discovered device.</param>
        public LocationListEntry(DeviceModel device)
        {
            Category = "Discovered";
            DiscoveredDevice = device;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocationListEntry"/> class for a configured location.
        /// </summary>
        /// <param name="location">The configured location.</param>
        public LocationListEntry(ConfiguredLocationModel location)
        {
            Category = location.Category;
            ConfiguredLocation = location;
        }
    }
}
