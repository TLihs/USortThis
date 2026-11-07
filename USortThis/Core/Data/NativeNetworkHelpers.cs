using System;
using System.Runtime.InteropServices;
using System.Text;

namespace USortThis.Core.Data
{
    public static class NativeNetworkHelpers
    {
        /// <summary>Queries Windows for the remote UNC path associated with a mapped drive.</summary>
        /// <param name="localName">The local mapped-drive name.</param>
        /// <param name="remoteName">Receives the remote path.</param>
        /// <param name="length">The buffer capacity and, on return, the required or used length.</param>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

        /// <summary>Resolves a mapped drive letter to its UNC path when Windows can do so.</summary>
        /// <param name="driveLetter">A drive name such as <c>E:</c> or <c>E</c>.</param>
        /// <returns>The remote UNC path, or null when the input is empty or resolution fails.</returns>
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
