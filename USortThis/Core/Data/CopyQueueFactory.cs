using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using USortThis.UI.Views.Controls;
using USortThis.Core.Services;
using USortThis.Core.Models;
using System.Diagnostics;

namespace USortThis.Core.Data
{
    public static partial class CopyQueueFactory
    {
        public static ObservableCollection<FileItemModel> FileItems { get; } = [];
        public static ObservableCollection<FileItemModel> CompletedFileItems { get; } = [];

        private static CancellationTokenSource? _copyCts = null;

        // Caches for performance
        private static readonly ConcurrentDictionary<string, string[]> _dnsCache = new();
        private static readonly object _usbCacheLock = new();
        private static List<UsbDriveInfo> _usbCache = [];

        // Track which drives have already been added to DeviceService to prevent duplicates
        private static readonly HashSet<string> _addedUsbDrives = new(StringComparer.OrdinalIgnoreCase);
        private static readonly object _addedDrivesLock = new();

        // USB refresh timer and throttling
        private static System.Threading.Timer? _usbRefreshTimer;
        private static DateTime _lastUsbRefresh = DateTime.MinValue;
        private static readonly TimeSpan _usbRefreshInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan _usbRefreshMinInterval = TimeSpan.FromSeconds(1);

        static CopyQueueFactory()
        {
            // Subscribe to device availability changes to attempt resume
            DeviceService.InterfaceDevices.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (DeviceModel d in e.NewItems.Cast<DeviceModel>())
                    {
                        d.PropertyChanged += Device_PropertyChanged;
                    }

                    // New devices added -> refresh usb cache and try to remap unassigned items
                    Task.Run(() =>
                    {
                        RefreshUsbCache();
                        _ = ReevaluateUnassignedQueueItemsAsync();
                    });
                }
                if (e.OldItems != null)
                {
                    foreach (DeviceModel d in e.OldItems.Cast<DeviceModel>())
                        d.PropertyChanged -= Device_PropertyChanged;
                }
            };

            // Subscribe existing devices
            foreach (var d in DeviceService.InterfaceDevices)
                d.PropertyChanged += Device_PropertyChanged;

            // Prime USB cache
            RefreshUsbCache();

            // Start periodic USB refresh timer
            try
            {
                _usbRefreshTimer = new System.Threading.Timer(_ =>
                {
                    try
                    {
                        Debug.Print("[CopyQueueManager] Periodic USB cache refresh triggered.");
                        RefreshUsbCache();
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[CopyQueueManager] Error refreshing USB cache: {ex.Message}");
                    }
                }, null, _usbRefreshInterval, _usbRefreshInterval);

                // Ensure timer is disposed on application exit
                    try
                    {
                        if (System.Windows.Application.Current != null)
                        {
                            System.Windows.Application.Current.Exit += (sender, args) =>
                            {
                                try
                                {
                                    Debug.Print("[CopyQueueManager] Application exiting, disposing USB refresh timer.");
                                    _usbRefreshTimer?.Dispose();
                                    _usbRefreshTimer = null;
                                }
                                catch { }
                            };
                        }
                    }
                    catch { }
            }
            catch (Exception ex)
            {
                Debug.Print($"[CopyQueueManager] Failed to start USB refresh timer: {ex.Message}");
            }
        }

        private static void Device_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(DeviceModel.IsConnected))
            {
                var dev = sender as DeviceModel;
                if (dev != null && dev.IsConnected)
                {
                    // Try to start processing when a device becomes available
                    StartCopyQueue();

                    // Re-evaluate unassigned items in background
                    Task.Run(() => ReevaluateUnassignedQueueItemsAsync());
                }
                // Refresh USB cache on device poll events but debounce to avoid frequent calls
                try
                {
                    if (DateTime.UtcNow - _lastUsbRefresh > _usbRefreshMinInterval)
                    {
                        Debug.Print("[CopyQueueManager] Device poll triggered USB cache refresh.");
                        Task.Run(() => RefreshUsbCache());
                    }
                }
                catch { }
            }
        }

        public static async Task InitializeAsync()
        {
            //// Load pending items from DB
            //var records = DatabaseService.LoadPendingFileItems();
            //if (records != null && records.Count > 0)
            //{
            //    await Application.Current.Dispatcher.InvokeAsync(() =>
            //    {
            //        foreach (var r in records)
            //        {
            //            var vm = new FileItemModel(
            //                r.QueueId,
            //                r.FilePath,
            //                r.FileSize,
            //                r.CreationTime,
            //                r.LastWriteTime,
            //                (FileItemModel.CopyStatus)r.Status,
            //                (FileItemModel.FailedReason)r.FailureReason);
            //            lock (FileItems)
            //                FileItems.Add(vm);
            //        }
            //    });
            //}

            // Enumerate current system drives and add any missing devices
            EnumerateAndAddSystemDrives();

            // Optionally start processing
            StartCopyQueue();
        }

        /// <summary>
        /// Enumerate all current system drives and add them to DeviceService if not already present.
        /// This ensures the UI device list is populated with discovered drives.
        /// </summary>
        private static void EnumerateAndAddSystemDrives()
        {
            try
            {
                Debug.Print("[CopyQueueFactory] Enumerating system drives...");

                // Get all ready drives from the system
                foreach (var drive in DriveInfo.GetDrives())
                {
                    try
                    {
                        if (!drive.IsReady)
                            continue;

                        string drivePath = drive.Name;
                        string normalizedDrive = drivePath.TrimEnd('\\').ToUpperInvariant();

                        // Check if already tracked to prevent re-adding
                        lock (_addedDrivesLock)
                        {
                            if (_addedUsbDrives.Contains(normalizedDrive))
                            {
                                Debug.Print($"[CopyQueueFactory] Drive {normalizedDrive} already tracked, skipping.");
                                continue;
                            }
                        }

                        // Check if device is already in the collection
                        var existing = DeviceService.InterfaceDevices.FirstOrDefault(d =>
                        {
                            try
                            {
                                if (d is InterfaceStorageDevice sd)
                                    return string.Equals(sd.DevicePath?.TrimEnd('\\'), normalizedDrive, StringComparison.OrdinalIgnoreCase);
                                return false;
                            }
                            catch { }
                            return false;
                        });

                        if (existing != null)
                        {
                            // Already registered, just track it
                            lock (_addedDrivesLock)
                            {
                                _addedUsbDrives.Add(normalizedDrive);
                            }
                            continue;
                        }

                        // Determine device type and add it
                        var deviceType = DeviceModel.GetDeviceTypeFromPath(drivePath);
                        if (deviceType != DeviceModel.DeviceTypes.Unknown)
                        {
                            string deviceName = drive.VolumeLabel ?? normalizedDrive;
                            if (string.IsNullOrEmpty(deviceName))
                                deviceName = normalizedDrive;

                            var addedDevice = DeviceService.AddDevice(deviceType, deviceName, drivePath);
                            if (addedDevice != null)
                            {
                                lock (_addedDrivesLock)
                                {
                                    _addedUsbDrives.Add(normalizedDrive);
                                }
                                Debug.Print($"[CopyQueueFactory] Enumerated and added device: {deviceName} ({normalizedDrive}) as {deviceType}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[CopyQueueFactory] Error processing drive {drive.Name}: {ex.Message}");
                    }
                }

                Debug.Print($"[CopyQueueFactory] System drive enumeration complete. Total devices: {DeviceService.InterfaceDevices.Count}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[CopyQueueFactory] Failed to enumerate system drives: {ex.Message}");
            }
        }

        public static void EnqueueFileForCopy(FileItemModel file)
        {
            //DatabaseService.SaveOrUpdateCopyQueueRecord(new DatabaseService.CopyQueueRecord
            //{
            //    QueueId = file.Id,
            //    FilePath = file.FilePath,
            //    FileSize = file.FileSize,
            //    CreationTime = file.CreationTime,
            //    LastWriteTime = file.LastWriteTime,
            //    Status = (int)file.Status,
            //    FailureReason = (int)file.FailureReason,
            //});

            //lock (FileItems)
            //    FileItems.Add(file);
        }

        // Attempt to map a path to a known InterfaceDevice and return its Id string
        private static string? FindDeviceIdForPath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.Trim();

            // UNC path: \\server\share\...
            if (path.StartsWith("\\\\") || path.StartsWith("//"))
            {
                try
                {
                    // Extract server name from UNC path
                    var parts = path.TrimStart('\\', '/').Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 0)
                    {
                        var server = parts[0];

                        // Try direct device name / IP match first
                        var dev = DeviceService.InterfaceDevices.FirstOrDefault(d =>
                        {
                            try
                            {
                                if (d is InterfaceNetworkDevice nd)
                                {
                                    if (!string.IsNullOrEmpty(nd.DeviceName) && string.Equals(nd.DeviceName, server, StringComparison.OrdinalIgnoreCase))
                                        return true;
                                    if (!string.IsNullOrEmpty(nd.IPAddress) && string.Equals(nd.IPAddress, server, StringComparison.OrdinalIgnoreCase))
                                        return true;
                                }
                            }
                            catch { }
                            return false;
                        });
                        if (dev != null) return dev.Id.ToString();

                        // Resolve server hostname to IPs and compare against known network device IPs (cached)
                        try
                        {
                            var addrs = GetHostAddressesCached(server);
                            foreach (var str in addrs)
                            {
                                InterfaceNetworkDevice? match = DeviceService.InterfaceDevices.OfType<InterfaceNetworkDevice>().FirstOrDefault(nd => !string.IsNullOrEmpty(nd.IPAddress) && string.Equals(nd.IPAddress, str, StringComparison.OrdinalIgnoreCase));
                                if (match != null)
                                    return match.Id.ToString();
                            }
                        }
                        catch { }

                        // Fallback: compare against DeviceName substrings
                        dev = DeviceService.InterfaceDevices.FirstOrDefault(d => !string.IsNullOrEmpty(d.DeviceName) && d.DeviceName.IndexOf(server, StringComparison.OrdinalIgnoreCase) >= 0);
                        if (dev != null) return dev.Id.ToString();
                    }
                }
                catch { }
            }
            else
            {
                try
                {
                    string root = Path.GetPathRoot(path) ?? string.Empty;
                    if (!string.IsNullOrEmpty(root))
                    {
                        // Normalize root to include trailing backslash
                        if (!root.EndsWith("\\")) root = root + "\\";

                        //1) Direct match against registered storage devices
                        var dev = DeviceService.InterfaceDevices.FirstOrDefault(d =>
                        {
                            try
                            {
                                if (d is InterfaceStorageDevice sd)
                                {
                                    if (!string.IsNullOrEmpty(sd.DevicePath) && string.Equals(NormalizeRoot(sd.DevicePath), NormalizeRoot(root), StringComparison.OrdinalIgnoreCase))
                                        return true;
                                }
                            }
                            catch { }
                            return false;
                        });
                        if (dev != null) return dev.Id.ToString();

                        //2) If drive letter maps to a USB drive, use cached UsbStorageDetector info to find serial/label and match InterfaceUsbDevice or InterfaceStorageDevice
                        try
                        {
                            var driveLetter = root;
                            EnsureUsbCache();
                            var matchUsb = _usbCache.FirstOrDefault(u => !string.IsNullOrEmpty(u.DriveLetter) && string.Equals(NormalizeRoot(u.DriveLetter), NormalizeRoot(driveLetter), StringComparison.OrdinalIgnoreCase));
                            if (matchUsb != null)
                            {
                                // Try to match by serial or vendor/product to a known InterfaceUsbDevice
                                var candidate = DeviceService.InterfaceDevices.OfType<InterfaceUsbDevice>().FirstOrDefault(ud =>
                                {
                                    try
                                    {
                                        if (!string.IsNullOrEmpty(ud.DeviceIdentifier) && (
                                             !string.IsNullOrEmpty(matchUsb.SerialNumber) && ud.DeviceIdentifier.IndexOf(matchUsb.SerialNumber, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             !string.IsNullOrEmpty(matchUsb.ProductId) && ud.DeviceIdentifier.IndexOf(matchUsb.ProductId, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                             !string.IsNullOrEmpty(matchUsb.VendorId) && ud.DeviceIdentifier.IndexOf(matchUsb.VendorId, StringComparison.OrdinalIgnoreCase) >= 0
                                        ))
                                            return true;
                                    }
                                    catch { }
                                    return false;
                                });
                                if (candidate != null) return candidate.Id.ToString();

                                // Fallback: match by volume label to any device name
                                if (!string.IsNullOrEmpty(matchUsb.VolumeLabel))
                                {
                                    var byName = DeviceService.InterfaceDevices.FirstOrDefault(d => !string.IsNullOrEmpty(d.DeviceName) && d.DeviceName.IndexOf(matchUsb.VolumeLabel, StringComparison.OrdinalIgnoreCase) >= 0);
                                    if (byName != null) return byName.Id.ToString();
                                }

                                // As a last resort, match a storage device entry where DevicePath equals root
                                var storageMatch = DeviceService.InterfaceDevices.OfType<InterfaceStorageDevice>().FirstOrDefault(sd => !string.IsNullOrEmpty(sd.DevicePath) && string.Equals(NormalizeRoot(sd.DevicePath), NormalizeRoot(root), StringComparison.OrdinalIgnoreCase));
                                if (storageMatch != null) return storageMatch.Id.ToString();
                            }
                        }
                        catch { }

                        //3) If it's a network-mapped drive letter, attempt to resolve the UNC path via WNetGetConnection (best-effort)
                        try
                        {
                            string? unc = NativeNetworkHelpers.GetUncForMappedDrive(root.TrimEnd('\\'));
                            if (!string.IsNullOrEmpty(unc))
                            {
                                // Recurse into UNC handling
                                var id = FindDeviceIdForPath(unc);
                                if (!string.IsNullOrEmpty(id))
                                    return id;
                            }
                        }
                        catch { }

                        //4) Fallback: try to find any device whose DevicePath is a prefix of the root
                        var prefixMatch = DeviceService.InterfaceDevices.FirstOrDefault(d =>
                        {
                            try
                            {
                                if (d is InterfaceStorageDevice sd && !string.IsNullOrEmpty(sd.DevicePath))
                                {
                                    var normalized = NormalizeRoot(sd.DevicePath);
                                    if (NormalizeRoot(root).StartsWith(normalized, StringComparison.OrdinalIgnoreCase))
                                        return true;
                                }
                            }
                            catch { }
                            return false;
                        });
                        if (prefixMatch != null)
                            return prefixMatch.Id.ToString();
                    }
                }
                catch { }
            }

            return null;
        }

        private static void EnsureUsbCache()
        {
            RefreshUsbCache();
        }

        private static void RefreshUsbCache()
        {
            try
            {
                Debug.Print("[CopyQueueManager] RefreshUsbCache: starting");
                var list = UsbStorageDetector.GetUsbDrives();
                lock (_usbCacheLock)
                {
                    _usbCache = [.. list];
                    _lastUsbRefresh = DateTime.UtcNow;
                }
                Debug.Print($"[CopyQueueManager] RefreshUsbCache: found {_usbCache.Count} USB drives.");

                // Add any newly discovered USB drives as devices
                try
                {
                    foreach (var usbDrive in _usbCache)
                    {
                        if (string.IsNullOrEmpty(usbDrive.DriveLetter))
                            continue;

                        string normalizedDrive = usbDrive.DriveLetter.TrimEnd('\\').ToUpperInvariant();

                        // Check if this drive has already been added to prevent re-discovery
                        lock (_addedDrivesLock)
                        {
                            if (_addedUsbDrives.Contains(normalizedDrive))
                            {
                                Debug.Print($"[CopyQueueManager] Drive {normalizedDrive} already tracked, skipping.");
                                continue;
                            }
                        }

                        // Check if this drive is already registered as a device
                        var existing = DeviceService.InterfaceDevices.FirstOrDefault(d =>
                        {
                            try
                            {
                                if (d is InterfaceStorageDevice sd)
                                    return string.Equals(sd.DevicePath?.TrimEnd('\\'), normalizedDrive, StringComparison.OrdinalIgnoreCase);
                                if (d is InterfaceUsbDevice ud)
                                    return !string.IsNullOrEmpty(ud.DeviceIdentifier) && 
                                           (!string.IsNullOrEmpty(usbDrive.SerialNumber) && ud.DeviceIdentifier.Equals(usbDrive.SerialNumber, StringComparison.OrdinalIgnoreCase) ||
                                            !string.IsNullOrEmpty(usbDrive.ProductId) && ud.DeviceIdentifier.Equals(usbDrive.ProductId, StringComparison.OrdinalIgnoreCase));
                            }
                            catch { }
                            return false;
                        });

                        if (existing != null)
                        {
                            // Already exists in DeviceService, just track it
                            lock (_addedDrivesLock)
                            {
                                _addedUsbDrives.Add(normalizedDrive);
                            }
                            Debug.Print($"[CopyQueueManager] Drive {normalizedDrive} already in DeviceService, tracking it.");
                            continue;
                        }

                        // New drive detected - add it
                        string deviceName = usbDrive.VolumeLabel ?? normalizedDrive;
                        if (string.IsNullOrEmpty(deviceName))
                            deviceName = normalizedDrive;

                        var addedDevice = DeviceService.AddDevice(DeviceModel.DeviceTypes.Usb, deviceName, usbDrive.DriveLetter);
                        if (addedDevice != null)
                        {
                            lock (_addedDrivesLock)
                            {
                                _addedUsbDrives.Add(normalizedDrive);
                            }
                            Debug.Print($"[CopyQueueManager] Auto-discovered and added device: {deviceName} ({normalizedDrive})");
                        }
                        else
                        {
                            Debug.Print($"[CopyQueueManager] Failed to add device: {deviceName} ({normalizedDrive})");
                        }
                    }
                }
                catch (Exception discEx)
                {
                    Debug.Print($"[CopyQueueManager] Error discovering devices: {discEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[CopyQueueManager] RefreshUsbCache: failed - {ex.Message}");
            }
        }

        private static string[] GetHostAddressesCached(string host)
        {
            if (string.IsNullOrEmpty(host))
                return [];

            if (_dnsCache.TryGetValue(host, out var addrs))
            {
                Debug.Print($"[CopyQueueManager] GetHostAddressesCached: cache hit for '{host}' ({addrs.Length} addresses)");
                return addrs;
            }

            try
            {
                Debug.Print($"[CopyQueueManager] GetHostAddressesCached: resolving '{host}'");
                var ips = System.Net.Dns.GetHostAddresses(host).Select(a => a.ToString()).ToArray();
                _dnsCache[host] = ips;
                Debug.Print($"[CopyQueueManager] GetHostAddressesCached: resolved {ips.Length} addresses for '{host}'");
                return ips;
            }
            catch (Exception ex)
            {
                Debug.Print($"[CopyQueueManager] GetHostAddressesCached: failed to resolve '{host}': {ex.Message}");
                _dnsCache[host] = [];
                return [];
            }
        }

        private static async Task ReevaluateUnassignedQueueItemsAsync()
        {
            //try
            //{
            //    List<FileItemViewModel> items;
            //    lock (FileItems)
            //    {
            //        items = [.. FileItems.Where(i => string.IsNullOrEmpty(i.DeviceId))];
            //    }

            //    foreach (var item in items)
            //    {
            //        try
            //        {
            //            var id = FindDeviceIdForPath(item.SourcePath);
            //            if (!string.IsNullOrEmpty(id))
            //            {
            //                // Update VM on UI thread
            //                if (Application.Current != null)
            //                {
            //                    await Application.Current.Dispatcher.InvokeAsync(() => item.DeviceId = id);
            //                }
            //                else
            //                {
            //                    item.DeviceId = id;
            //                }

            //                // Persist change
            //                DatabaseService.SaveOrUpdateCopyQueueRecord(new DatabaseService.CopyQueueRecord
            //                {
            //                    QueueId = item.Id,
            //                    ProfileId = string.Empty,
            //                    SourcePath = item.SourcePath,
            //                    DestinationPath = item.DestinationPath,
            //                    FileName = Path.GetFileName(item.SourcePath),
            //                    FileSize = 0,
            //                    Status = (int)item.Status,
            //                    FailureReason = (int)item.FailureReason,
            //                    DeviceId = item.DeviceId,
            //                    CreatedAt = DateTime.UtcNow,
            //                    UpdatedAt = DateTime.UtcNow,
            //                    RetryCount = 0,
            //                    Progress = item.Progress
            //                });
            //            }
            //        }
            //        catch { }
            //    }
            //}
            //catch { }
        }

        private static string NormalizeRoot(string p)
        {
            if (string.IsNullOrEmpty(p)) return string.Empty;
            try
            {
                var root = Path.GetPathRoot(p) ?? p;
                if (!root.EndsWith('\\'))
                    root += '\\';
                return root.ToUpperInvariant();
            }
            catch { return p; }
        }

        /// <summary>
        /// Start processing the copy queue. Returns true if a new processing task was started.
        /// </summary>
        /// <param name="maxDegreeOfParallelism">Maximum number of concurrent copy operations (throttling).</param>
        public static bool StartCopyQueue(int maxDegreeOfParallelism = 1)
        {
            //if (_copyTask == null || _copyTask.IsCompleted)
            //{
            //    _copyCts = new CancellationTokenSource();
            //    CancellationToken ct = _copyCts.Token;

            //    _copyTask = Task.Run(async () =>
            //    {
            //        // Take a snapshot of the items to process to avoid holding locks during IO
            //        List<FileItemModel> itemsToProcess;
            //        lock (FileItems)
            //            itemsToProcess = [.. FileItems];

            //        if (itemsToProcess.Count == 0)
            //            return;

            //        // Use a semaphore to limit concurrency (throttling)
            //        using SemaphoreSlim semaphore = new(Math.Max(1, maxDegreeOfParallelism));

            //        foreach (var item in itemsToProcess)
            //        {
            //            await semaphore.WaitAsync(ct).ConfigureAwait(false);
            //            item.StartTransfer(ct);
            //        }
            //    }, _copyCts.Token);

            //    return true;
            //}
            return false;
        }

        /// <summary>
        /// Request cancellation of the current copy queue processing.
        /// </summary>
        public static void StopCopyQueue()
        {
            try
            {
                _copyCts?.Cancel();
            }
            catch { }
        }
    }
}
