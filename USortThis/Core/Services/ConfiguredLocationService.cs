using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Service class for managing configured locations.
    /// </summary>
    public sealed class ConfiguredLocationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly WindowsCredentialStore _credentialStore;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredLocationService"/> class.
        /// </summary>
        /// <param name="databaseService">The database service.</param>
        /// <param name="credentialStore">The credential store.</param>
        public ConfiguredLocationService(IDatabaseService databaseService, WindowsCredentialStore credentialStore)
        {
            _databaseService = databaseService;
            _credentialStore = credentialStore;
        }

        /// <summary>
        /// Saves the specified configured location.
        /// </summary>
        /// <param name="location">The configured location to save.</param>
        /// <param name="password">The password for the configured credentials.</param>
        /// <exception cref="ArgumentNullException">Thrown when location is null.</exception>
        /// <exception cref="ArgumentException">Thrown when a password is required but not provided.</exception>
        public void Save(ConfiguredLocationModel location, string? password)
        {
            ArgumentNullException.ThrowIfNull(location);
            if (!location.UseCurrentWindowsCredentials && string.IsNullOrWhiteSpace(password) && _credentialStore.Read(location.CredentialTarget) is null)
                throw new ArgumentException("A password is required for the configured credentials.", nameof(password));

            if (location.UseCurrentWindowsCredentials)
            {
                _credentialStore.Delete(location.CredentialTarget);
            }
            else
            {
                var username = string.IsNullOrWhiteSpace(location.Domain)
                    ? location.Username
                    : $"{location.Domain}\\{location.Username}";
                var existing = _credentialStore.Read(location.CredentialTarget);
                _credentialStore.Write(location.CredentialTarget, username, password ?? existing!.Value.Password);
            }

            _databaseService.SaveConfiguredLocation(location);
        }

        /// <summary>
        /// Deletes the specified configured location.
        /// </summary>
        /// <param name="location">The configured location to delete.</param>
        /// <exception cref="ArgumentNullException">Thrown when location is null.</exception>
        public void Delete(ConfiguredLocationModel location)
        {
            ArgumentNullException.ThrowIfNull(location);
            _databaseService.DeleteConfiguredLocation(location.Id);
            _credentialStore.Delete(location.CredentialTarget);
        }

        /// <summary>
        /// Tests the connection for the specified configured location.
        /// </summary>
        /// <param name="location">The configured location to test.</param>
        /// <returns>A tuple indicating the success of the connection test and a corresponding message.</returns>
        /// <exception cref="ArgumentNullException">Thrown when location is null.</exception>
        public (bool Success, string Message) TestConnection(ConfiguredLocationModel location)
        {
            ArgumentNullException.ThrowIfNull(location);
            if (string.IsNullOrWhiteSpace(location.Path))
                return (false, "Enter a location path.");

            if (!location.Path.StartsWith("\\\\", StringComparison.Ordinal))
                return Directory.Exists(location.Path)
                    ? (true, "Location is accessible.")
                    : (false, "The location is not accessible.");

            string remoteName;
            try
            {
                remoteName = GetShareRoot(location.Path);
            }
            catch (ArgumentException exception)
            {
                return (false, exception.Message);
            }
            bool connectionAdded = false;
            try
            {
                if (!location.UseCurrentWindowsCredentials)
                {
                    var stored = _credentialStore.Read(location.CredentialTarget);
                    if (stored is null)
                        return (false, "No saved credentials were found for this location.");
                    var resource = new NetworkResource
                    {
                        Type = 1,
                        RemoteName = remoteName
                    };
                    var result = WNetAddConnection2(ref resource, stored.Value.Password, stored.Value.Username, 0);
                    if (result != 0)
                        return (false, GetNetworkError(result));
                    connectionAdded = true;
                }

                return Directory.Exists(location.Path)
                    ? (true, "Location is accessible.")
                    : (false, "The network location is not accessible.");
            }
            catch (Win32Exception exception)
            {
                return (false, exception.Message);
            }
            finally
            {
                if (connectionAdded)
                    WNetCancelConnection2(remoteName, 0, true);
            }
        }

        /// <summary>
        /// Gets the share root from the specified network path.
        /// </summary>
        /// <param name="path">The network path.</param>
        /// <returns>The share root of the network path.</returns>
        /// <exception cref="ArgumentException">Thrown when the path is not in the form \\\\server\\share.</exception>
        private static string GetShareRoot(string path)
        {
            var parts = path.TrimStart('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ArgumentException("Enter a network path in the form \\\\server\\share.", nameof(path));
            return $"\\\\{parts[0]}\\{parts[1]}";
        }

        /// <summary>
        /// Gets the error message for the specified network error code.
        /// </summary>
        /// <param name="error">The network error code.</param>
        /// <returns>The error message.</returns>
        private static string GetNetworkError(int error) => error switch
        {
            1219 => "Windows already has a connection to this server using different credentials. Disconnect that connection or use the same credentials.",
            5 => "Access was denied. Check the credentials and permissions.",
            53 => "The network path was not found.",
            _ => new Win32Exception(error).Message
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NetworkResource
        {
            public int Scope;
            public int Type;
            public int DisplayType;
            public int Usage;
            public string? LocalName;
            public string? RemoteName;
            public string? Comment;
            public string? Provider;
        }

        /// <summary>
        /// Establishes a network connection to a remote resource.
        /// </summary>
        /// <param name="networkResource">The network resource to connect to.</param>
        /// <param name="password">The password for the network resource.</param>
        /// <param name="username">The username for the network resource.</param>
        /// <param name="flags">Additional flags for the connection.</param>
        /// <returns>Returns 0 on success, or a Win32 error code on failure.</returns>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NetworkResource networkResource, string? password, string? username, int flags);

        /// <summary>
        /// Cancels a network connection to a remote resource.
        /// </summary>
        /// <param name="name">The name of the network resource.</param>
        /// <param name="flags">Additional flags for the disconnection.</param>
        /// <param name="force">Indicates whether to force the disconnection.</param>
        /// <returns>Returns 0 on success, or a Win32 error code on failure.</returns>
        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);
    }
}
