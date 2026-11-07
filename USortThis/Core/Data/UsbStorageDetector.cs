using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace USortThis.Core.Data
{
    public class UsbDriveInfo
    {
        public string DriveLetter { get; set; } = string.Empty;
        public bool IsUsb { get; set; }
        public byte BusType { get; set; }
        public string? VendorId { get; set; }
        public string? ProductId { get; set; }
        public string? ProductRevision { get; set; }
        public string? SerialNumber { get; set; }
        public string? VolumeLabel { get; set; }
        public string? FileSystem { get; set; }
    }

    public static class UsbStorageDetector
    {
        // IOCTL_STORAGE_QUERY_PROPERTY (common constant)
        private const uint IOCTL_STORAGE_QUERY_PROPERTY = 0x2D1400;

        private enum StoragePropertyId : uint
        {
            StorageDeviceProperty = 0
        }

        private enum StorageQueryType : uint
        {
            PropertyStandardQuery = 0
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_PROPERTY_QUERY
        {
            public uint PropertyId;
            public uint QueryType;
            public uint AdditionalParameters; // placeholder - not used
        }

        // STORAGE_BUS_TYPE values; USB is 7
        // (Partial list; we only need BusTypeUsb)
        private const byte BusTypeUsb = 7;

        /// <summary>Enumerates ready drives and returns device details for those using the USB bus.</summary>
        /// <returns>The USB drive information that could be queried successfully.</returns>
        public static IEnumerable<UsbDriveInfo> GetUsbDrives()
        {
            var results = new List<UsbDriveInfo>();

            foreach (var d in DriveInfo.GetDrives())
            {
                try
                {
                    // We'll only attempt drives that appear ready and have a root (skip network and CDROM unless you need them)
                    if (!d.IsReady)
                        continue;

                    // Open volume handle like \\.\E:
                    string path = @"\\.\" + d.Name.TrimEnd('\\');

                    using var handle = CreateFileHandle(path);
                    if (handle == null || handle.IsInvalid)
                        continue;

                    // prepare input query
                    var query = new STORAGE_PROPERTY_QUERY
                    {
                        PropertyId = (uint)StoragePropertyId.StorageDeviceProperty,
                        QueryType = (uint)StorageQueryType.PropertyStandardQuery,
                        AdditionalParameters = 0
                    };

                    int querySize = Marshal.SizeOf<STORAGE_PROPERTY_QUERY>();
                    nint inBuffer = Marshal.AllocHGlobal(querySize);
                    try
                    {
                        Marshal.StructureToPtr(query, inBuffer, false);

                        // allocate output buffer (descriptor is variable length; pick a reasonably large buffer)
                        byte[] outBuf = new byte[4096];
                        if (DeviceIoControl(handle.DangerousGetHandle(), IOCTL_STORAGE_QUERY_PROPERTY, inBuffer, (uint)querySize,
                            outBuf, (uint)outBuf.Length, out uint bytesReturned, nint.Zero))
                        {
                            if (bytesReturned > 0)
                            {
                                var info = ParseStorageDeviceDescriptor(outBuf, (int)bytesReturned);
                                info.DriveLetter = d.Name;
                                info.VolumeLabel = d.VolumeLabel;
                                info.FileSystem = d.DriveFormat;

                                // Only include actual USB devices
                                if (info.IsUsb)
                                    results.Add(info);
                            }
                        }
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(inBuffer);
                    }
                }
                catch
                {
                    // best-effort; skip problematic drives
                }
            }

            return results;
        }

        /// <summary>Parses USB identifiers and bus information from a storage-device descriptor buffer.</summary>
        /// <param name="buffer">The descriptor bytes returned by Windows.</param>
        /// <param name="length">The number of valid bytes in <paramref name="buffer"/>.</param>
        /// <returns>The parsed drive information, with default values when the descriptor is too short.</returns>
        private static UsbDriveInfo ParseStorageDeviceDescriptor(byte[] buffer, int length)
        {
            // STORAGE_DEVICE_DESCRIPTOR structure layout (partial):
            // ULONG Version;                    // +0
            // ULONG Size;                       // +4
            // BYTE DeviceType;                  // +8
            // BYTE DeviceTypeModifier;          // +9
            // BOOLEAN RemovableMedia;           // +10
            // BOOLEAN CommandQueueing;          // +11
            // ULONG VendorIdOffset;             // +12
            // ULONG ProductIdOffset;            // +16
            // ULONG ProductRevisionOffset;      // +20
            // ULONG SerialNumberOffset;         // +24
            // STORAGE_BUS_TYPE BusType;         // +28 (1 byte enum)
            // ... (various fields)
            // The offsets are byte offsets into this same buffer where null-terminated ANSI strings live.

            UsbDriveInfo result = new UsbDriveInfo();

            if (length < 29) // minimal sanity - need at least up to BusType field
                return result;

            // read little-endian uints/bytes from buffer safely
            uint readUInt32(int offset)
            {
                // reads a bounded little-endian 32-bit value
                if (offset + 4 > length) return 0;
                return (uint)(buffer[offset] | buffer[offset + 1] << 8 | buffer[offset + 2] << 16 | buffer[offset + 3] << 24);
            }
            byte readByte(int offset)
            {
                // reads a byte within the valid buffer length
                if (offset >= length) return 0;
                return buffer[offset];
            }

            string? readStringFromOffset(uint off)
            {
                // reads a null-terminated ASCII string at a descriptor offset
                if (off == 0 || off >= length) return null;
                int i = (int)off;
                int end = i;
                while (end < length && buffer[end] != 0) end++;
                if (end <= i) return null;
                return Encoding.ASCII.GetString(buffer, i, end - i).Trim();
            }

            // Offsets in the STORAGE_DEVICE_DESCRIPTOR:
            // VendorIdOffset: at byte offset 12
            // ProductIdOffset: at byte offset 16
            // ProductRevisionOffset: at byte offset 20
            // SerialNumberOffset: at byte offset 24
            // BusType: at byte offset 28 (should be reliable)
            uint vendorOffset = readUInt32(12);
            uint productOffset = readUInt32(16);
            uint productRevOffset = readUInt32(20);
            uint serialOffset = readUInt32(24);

            // BusType is at offset 28 in the STORAGE_DEVICE_DESCRIPTOR
            byte busType = readByte(28);

            result.VendorId = readStringFromOffset(vendorOffset);
            result.ProductId = readStringFromOffset(productOffset);
            result.ProductRevision = readStringFromOffset(productRevOffset);
            result.SerialNumber = readStringFromOffset(serialOffset);
            result.BusType = busType;
            result.IsUsb = busType == BusTypeUsb;

            return result;
        }

        /// <summary>Opens a device path for shared read access and releases invalid handles.</summary>
        /// <param name="path">The device path to open.</param>
        /// <returns>A valid safe handle, or null when the device cannot be opened.</returns>
        private static SafeFileHandle? CreateFileHandle(string path)
        {
            // GENERIC_READ | GENERIC_WRITE (we only need read)
            const uint GENERIC_READ = 0x80000000;
            const uint FILE_SHARE_READ = 0x00000001;
            const uint FILE_SHARE_WRITE = 0x00000002;
            const uint OPEN_EXISTING = 3;

            SafeFileHandle handle = CreateFile(path, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, nint.Zero, OPEN_EXISTING, 0, nint.Zero);
            if (handle.IsInvalid)
            {
                handle.Dispose();
                return null;
            }
            return handle;
        }

        #region P/Invoke

        /// <summary>Opens an existing file or device and returns its native handle.</summary>
        /// <param name="lpFileName">The path of the file or device.</param>
        /// <param name="dwDesiredAccess">The requested access mode.</param>
        /// <param name="dwShareMode">The sharing mode for the open handle.</param>
        /// <param name="lpSecurityAttributes">Optional native security attributes.</param>
        /// <param name="dwCreationDisposition">The action to take when opening the path.</param>
        /// <param name="dwFlagsAndAttributes">The file or device flags.</param>
        /// <param name="hTemplateFile">An optional template handle.</param>
        /// <returns>A safe handle for the opened file or device.</returns>
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            nint lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            nint hTemplateFile);

        /// <summary>Sends a control code to a device driver.</summary>
        /// <param name="hDevice">The device handle receiving the control code.</param>
        /// <param name="dwIoControlCode">The control code to send.</param>
        /// <param name="lpInBuffer">The input buffer, when required by the control code.</param>
        /// <param name="nInBufferSize">The size of the input buffer.</param>
        /// <param name="lpOutBuffer">Receives the device response.</param>
        /// <param name="nOutBufferSize">The capacity of the output buffer.</param>
        /// <param name="lpBytesReturned">Receives the number of bytes returned.</param>
        /// <param name="lpOverlapped">Optional overlapped-I/O state.</param>
        /// <returns>True when the control operation succeeds.</returns>
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            nint hDevice,
            uint dwIoControlCode,
            nint lpInBuffer,
            uint nInBufferSize,
            [Out] byte[] lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            nint lpOverlapped);

        #endregion
    }
}
