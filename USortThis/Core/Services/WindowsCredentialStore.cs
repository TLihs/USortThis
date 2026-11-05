using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace USortThis.Core.Services
{
    public sealed class WindowsCredentialStore
    {
        private const uint GenericCredential = 1;
        private const uint LocalMachinePersistence = 2;

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

        [DllImport("advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredWrite(ref NativeCredential credential, uint flags);

        [DllImport("advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredRead(string target, uint type, uint flags, out IntPtr credential);

        [DllImport("advapi32.dll", EntryPoint = "CredDeleteW", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CredDelete(string target, uint type, uint flags);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern void CredFree(IntPtr credential);
    }
}
