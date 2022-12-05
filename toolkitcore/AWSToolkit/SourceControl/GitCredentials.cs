using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace Amazon.AWSToolkit.SourceControl
{
    /// <summary>
    /// Wrapper around git credentials that are persisted to the OS
    /// credential store
    /// </summary>
    public class GitCredentials : IDisposable
    {
        private static readonly Encoding _passwordEncoding = Encoding.Unicode;
        private static readonly int _maxPasswordLengthInBytes;
        // TODO: Verify a good UX if Demand() is denied
        private static readonly SecurityPermission _unmanagedCodePermission;

        private CredentialType _credentialType;
        private string _target;
        private SecureString _password;
        private string _username;
        private string _description;
        private DateTime _lastWriteTimeUtc;
        private PersistenceType _persistenceType;

        public enum PersistenceType : uint
        {
            Session = 1,
            LocalComputer = 2,
            Enterprise = 3
        }

        public enum CredentialType : uint
        {
            None = 0,
            Generic = 1,
            DomainPassword = 2,
            DomainCertificate = 3,
            DomainVisiblePassword = 4
        }

        static GitCredentials()
        {
            // Use static ctor instead of field initialization when static methods use static fields to avoid issues with BeforeFieldInit
            // https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/static-constructors
            _unmanagedCodePermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            _maxPasswordLengthInBytes = _passwordEncoding.GetMaxByteCount(NativeMethods.CREDUI_MAX_PASSWORD_LENGTH);
        }

        private GitCredentials()
        {
            _credentialType = CredentialType.Generic;
            _persistenceType = PersistenceType.LocalComputer;
        }

        public GitCredentials(string username, string password, string target)
        {
            Username = username;
            Password = password;
            Target = target;

            _credentialType = CredentialType.Generic;
            _persistenceType = PersistenceType.LocalComputer;
            _lastWriteTimeUtc = DateTime.MinValue;
        }

        private bool _isDisposed;
        private void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                SecurePassword.Clear();
                SecurePassword.Dispose();

                _isDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CheckNotDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("Credential object is already disposed.");
            }
        }

        public string Username
        {
            get
            {
                CheckNotDisposed();
                return _username;
            }
            set
            {
                CheckNotDisposed();
                _username = value;
            }
        }

        // TODO: Converting SecureString to String creates potentially long lived, insecure plain-text password in VS memory.  Fix this.
        // Also, maybe just don't use SecureString at all.  https://docs.microsoft.com/en-us/dotnet/api/system.security.securestring?view=netframework-4.7.2#remarks
        public string Password
        {
            get => CreateString(SecurePassword);
            set
            {
                CheckNotDisposed();
                SecurePassword = CreateSecureString(string.IsNullOrEmpty(value) ? string.Empty : value);
            }
        }
        
        public SecureString SecurePassword
        {
            get
            {
                CheckNotDisposed();
                _unmanagedCodePermission.Demand();
                return _password?.Copy() ?? new SecureString();
            }
            set
            {
                CheckNotDisposed();
                if (_password != null)
                {
                    _password.Clear();
                    _password.Dispose();
                }
                _password = value?.Copy() ?? new SecureString();
            }
        }

        public string Target
        {
            get
            {
                CheckNotDisposed();
                return _target;
            }
            set
            {
                CheckNotDisposed();
                _target = value;
            }
        }

        public string Description
        {
            get
            {
                CheckNotDisposed();
                return _description;
            }
            set
            {
                CheckNotDisposed();
                _description = value;
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                CheckNotDisposed();
                return _lastWriteTimeUtc;
            }
            private set
            {
                CheckNotDisposed();
                _lastWriteTimeUtc = value;
            }
        }

        public bool Save()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            byte[] passwordBytes = _passwordEncoding.GetBytes(Password);
            ValidatePasswordLength(passwordBytes);

            var credential = new NativeMethods.CREDENTIAL
            {
                TargetName = Target,
                UserName = Username,
                CredentialBlob = Marshal.StringToCoTaskMemUni(Password),
                CredentialBlobSize = passwordBytes.Length,
                Comment = Description,
                Type = (int)_credentialType,
                Persist = (int)_persistenceType
            };

            var result = NativeMethods.CredWrite(ref credential, 0);
            if (result)
            {
                LastWriteTimeUtc = DateTime.UtcNow;
            }
            return result;
        }

        public static bool Delete(string target, CredentialType credentialType)
        {
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("Target must be specified to delete a credential.");
            }

            return NativeMethods.CredDelete(new StringBuilder(target), credentialType, 0);
        }

        public bool Load()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            bool result = NativeMethods.CredRead(Target, _credentialType, 0, out IntPtr credPointer);
            if (!result)
            {
                return false;
            }
            using (var credentialHandle = new NativeMethods.CriticalCredentialHandle(credPointer))
            {
                LoadInternal(credentialHandle.GetCredential());
            }
            return true;
        }

        public bool Exists()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException("Target must be specified to check existence of a credential.");
            }

            using (var existing = new GitCredentials { Target = Target })
            {
                return existing.Load();
            }
        }

        private void LoadInternal(NativeMethods.CREDENTIAL credential)
        {
            Username = credential.UserName;
            if (credential.CredentialBlobSize > 0)
            {
                // Specifying the character length will return strings with embedded \0 in them.  Unclear
                // if this is desired or not.  Ideally, this would marshall the byte array and use _passwordEncoding
                // to decode.
                //
                // "This API reflects the Windows definition of Unicode, which is a UTF-16 2-byte encoding."
                // https://docs.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.marshal.ptrtostringuni?view=netframework-4.7.2#system-runtime-interopservices-marshal-ptrtostringuni(system-intptr-system-int32) 
                Password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }
            Target = credential.TargetName;
            _credentialType = (CredentialType)credential.Type;
            _persistenceType = (PersistenceType)credential.Persist;
            Description = credential.Comment;
            LastWriteTimeUtc = DateTime.FromFileTimeUtc(credential.LastWritten);
        }

        private static void ValidatePasswordLength(byte[] passwordBytes)
        {
            if (passwordBytes.Length > _maxPasswordLengthInBytes)
            {
                throw new ArgumentOutOfRangeException(
                    $"The password length ({passwordBytes.Length} bytes) exceeds the maximum password length ({_maxPasswordLengthInBytes} bytes).");
            }
        }

        private static SecureString CreateSecureString(string plainString)
        {
            var str = new SecureString();
            if (!string.IsNullOrEmpty(plainString))
            {
                foreach (char c in plainString)
                {
                    str.AppendChar(c);
                }
            }
            return str;
        }

        private static string CreateString(SecureString secureString)
        {
            if ((secureString == null) || (secureString.Length == 0))
            {
                return string.Empty;
            }

            string str;
            var strptr = IntPtr.Zero;

            try
            {
                strptr = Marshal.SecureStringToBSTR(secureString);
                str = Marshal.PtrToStringBSTR(strptr);
            }
            finally
            {
                if (strptr != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(strptr);
                }
            }

            return str;
        }
    }
}
