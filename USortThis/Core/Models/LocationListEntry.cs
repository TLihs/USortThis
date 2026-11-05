namespace USortThis.Core.Models
{
    public sealed class LocationListEntry
    {
        public string Category { get; }
        public DeviceModel? DiscoveredDevice { get; }
        public ConfiguredLocationModel? ConfiguredLocation { get; }

        public LocationListEntry(DeviceModel device)
        {
            Category = "Discovered";
            DiscoveredDevice = device;
        }

        public LocationListEntry(ConfiguredLocationModel location)
        {
            Category = location.Category;
            ConfiguredLocation = location;
        }
    }
}
