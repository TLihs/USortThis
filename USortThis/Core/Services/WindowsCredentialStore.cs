using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace USortThis.Core.Services
{
    /// <summary>
    /// Provides methods to store, retrieve, and delete credentials in the Windows Credential Store.
    /// </summary>
    public sealed class WindowsCredentialStore
    {
        private const uint GenericCredential = 1;
        private const uint LocalMachinePersistence = 2;

        /// <summary>
        /// Stores a credential in the Windows Credential Store.
        /// </summary>
        /// <param name="target">The target application or service for the credential.</param>
        /// <param name="username">The username for the credential.</param>
        /// <param name="password">The password for the credential.</param>
        /// <exception cref="Win32Exception">Thrown when the credential could not be written.</exception>
        public void Write(string target, string username, string password)
        {
            var credential = new NativeCredential
            {
                Type = GenericCredential,
                TargetName = target,
                UserName = username,
                CredentialBlobSize = checked((uint)Encoding.Unicode.GetByteCount(password)),
                Persist = LocalMachinePersistence
            };

            var blob = Marshal.StringToCoTaskMemUni(password);
            credential.CredentialBlob = blob;
            try
            {
                if (!CredWrite(ref credential, 0))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
            }
            finally
            {
                Marshal.ZeroFreeCoTaskMemUnicode(blob);
            }
        }

        /// <summary>
        /// Retrieves a credential from the Windows Credential Store.
        /// </summary>
        /// <param name="target">The target application or service for the credential.</param>
        /// <returns>
        /// A tuple containing the username and password for the credential, or null if the credential does not exist.
        /// </returns>
        /// <exception cref="Win32Exception">Thrown when the credential could not be read.</exception>
        public (string Username, string Password)? Read(string target)
        {
            if (!CredRead(target, GenericCredential, 0, out var credentialPointer))
            {
                var error = Marshal.GetLastWin32Error();
                if (error == 1168)
                    return null;
                throw new Win32Exception(error);
            }

            try
            {
                var credential = Marshal.PtrToStructure<NativeCredential>(credentialPointer);
                var password = credential.CredentialBlobSize == 0
                    ? string.Empty
                    : Marshal.PtrToStringUni(credential.CredentialBlob, checked((int)credential.CredentialBlobSize / sizeof(char))) ?? string.Empty;
                return (credential.UserName ?? string.Empty, password);
            }
            finally
            {
                CredFree(credentialPointer);
            }
        }

        /// <summary>
        /// Deletes a credential from the Windows Credential Store.
        /// </summary>
        /// <param name="target">The target application or service for the credential.</param>
        /// <exception cref="Win32Exception">Thrown when the credential could not be deleted.</exception>
        public void Delete(string target)
        {
            if (CredDelete(target, GenericCredential, 0))
                return;
            var error = Marshal.GetLastWin32Error();
            if (error != 1168)
                throw new Win32Exception(error);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct NativeCredential
        {
            public uint Flags;
            public uint Type;
            public string? TargetName;
            public string? Comment;
            public System.Runtime.InteropServices.ComTypes.FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public uint Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            public string? TargetAlias;
            public string? UserName;
        }

        /// <summary>
        /// Writes a credential to the Windows Credential Store.
        /// </summary>
        /// <param name="credential">The credential to write.</param>
        /// <param name="flags">Additional flags for the operation.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <exception cref="MarshalAs(UnmanagedType.Bool)">Thrown when the credential could not be written.</exception>
        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref NativeCredential credential, uint flags);

        /// <summary>
        /// Reads a credential from the Windows Credential Store.
        /// </summary>
        /// <param name="target">The target of the credential to read.</param>
        /// <param name="type">The type of the credential.</param>
        /// <param name="flags">Additional flags for the operation.</param>
        /// <param name="credential">A pointer to the credential.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <exception cref="MarshalAs(UnmanagedType.Bool)">Thrown when the credential could not be read.</exception>
        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

        /// <summary>
        /// Deletes a credential from the Windows Credential Store.
        /// </summary>
        /// <param name="target">The target of the credential to delete.</param>
        /// <param name="type">The type of the credential.</param>
        /// <param name="flags">Additional flags for the operation.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <exception cref="MarshalAs(UnmanagedType.Bool)">Thrown when the credential could not be deleted.</exception>
        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        /// <summary>
        /// Frees memory allocated for a credential.
        /// </summary>
        /// <param name="credential">A pointer to the credential to free.</param>
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credential);
    }
}
