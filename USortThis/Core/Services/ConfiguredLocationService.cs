using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using USortThis.Core.Interfaces;
using USortThis.Core.Models;

namespace USortThis.Core.Services
{
    public sealed class ConfiguredLocationService
    {
        private readonly IDatabaseService _databaseService;
        private readonly WindowsCredentialStore _credentialStore;

        public ConfiguredLocationService(IDatabaseService databaseService, WindowsCredentialStore credentialStore)
        {
            _databaseService = databaseService;
            _credentialStore = credentialStore;
        }

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

        public void Delete(ConfiguredLocationModel location)
        {
            ArgumentNullException.ThrowIfNull(location);
            _databaseService.DeleteConfiguredLocation(location.Id);
            _credentialStore.Delete(location.CredentialTarget);
        }

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

        private static string GetShareRoot(string path)
        {
            var parts = path.TrimStart('\\').Split('\\', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new ArgumentException("Enter a network path in the form \\\\server\\share.", nameof(path));
            return $"\\\\{parts[0]}\\{parts[1]}";
        }

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

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetAddConnection2(ref NetworkResource networkResource, string? password, string? username, int flags);

        [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
        private static extern int WNetCancelConnection2(string name, int flags, bool force);
    }
}
