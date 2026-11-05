using System;
using System.Runtime.InteropServices;
using System.Text;

namespace USortThis.Core.Data
{
    public static class NativeNetworkHelpers
    {
        // Wrapper for WNetGetConnection to resolve mapped drive letters to UNC paths
        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

        public static string? GetUncForMappedDrive(string driveLetter)
        {
            if (string.IsNullOrEmpty(driveLetter))
                return null;
            // Expect driveLetter like "E:" or "E"
            string dl = driveLetter;
            if (dl.Length == 1) dl = dl + ":";
            if (dl.Length == 2 && dl[1] != ':') dl = dl[0] + ":";

            var sb = new StringBuilder(512);
            int size = sb.Capacity;
            int res = WNetGetConnection(dl, sb, ref size);
            return res == 0 ? sb.ToString() : null;
        }
    }
}
