using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace Amazon.AWSToolkit.VisualStudio.TeamExplorer.CredentialManagement
{
    /// <summary>
    /// Wrapper around git credentials that are persisted to the OS
    /// credential store
    /// </summary>
    public class GitCredentials : IDisposable
    {
        #region Private Members
        const int maxPasswordLengthInBytes = NativeMethods.CREDUI_MAX_PASSWORD_LENGTH * 2;

        static readonly object _lockObject = new object();
        static readonly SecurityPermission _unmanagedCodePermission;

        CredentialType _credentialType;
        string _target;
        SecureString _password;
        string _username;
        string _description;
        DateTime _lastWriteTime;
        PersistenceType _persistenceType;

        static GitCredentials()
        {
            lock (_lockObject)
            {
                _unmanagedCodePermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            }
        }

        #endregion

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
            _lastWriteTime = DateTime.MinValue;
        }

        bool _disposed;
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_disposed) return;
                SecurePassword.Clear();
                SecurePassword.Dispose();
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CheckNotDisposed()
        {
            if (_disposed)
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
        
        public string Password
        {
            get { return CreateString(SecurePassword); }
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

        public DateTime LastWriteTime
        {
            get { return LastWriteTimeUtc.ToLocalTime(); }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                CheckNotDisposed();
                return _lastWriteTime;
            }
            private set { _lastWriteTime = value; }
        }

        /*
        public CredentialType Type
        {
            get
            {
                CheckNotDisposed();
                return _type;
            }
            set
            {
                CheckNotDisposed();
                _type = value;
            }
        }

        public PersistenceType PersistenceType
        {
            get
            {
                CheckNotDisposed();
                return _persistenceType;
            }
            set
            {
                CheckNotDisposed();
                _persistenceType = value;
            }
        }
        */

        public bool Save()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            byte[] passwordBytes = Encoding.Unicode.GetBytes(Password);
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
            if (!result)
            {
                return false;
            }
            LastWriteTimeUtc = DateTime.UtcNow;
            return true;
        }

        public bool Save(byte[] passwordBytes)
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            ValidatePasswordLength(passwordBytes);

            var blob = Marshal.AllocCoTaskMem(passwordBytes.Length);
            Marshal.Copy(passwordBytes, 0, blob, passwordBytes.Length);

            var credential = new NativeMethods.CREDENTIAL
            {
                TargetName = Target,
                UserName = Username,
                CredentialBlob = blob,
                CredentialBlobSize = passwordBytes.Length,
                Comment = Description,
                Type = (int)_credentialType,
                Persist = (int)_persistenceType
            };

            var result = NativeMethods.CredWrite(ref credential, 0);
            Marshal.FreeCoTaskMem(blob);
            if (!result)
            {
                return false;
            }
            LastWriteTimeUtc = DateTime.UtcNow;
            return true;
        }

        public bool Delete()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException("Target must be specified to delete a credential.");
            }

            var target = string.IsNullOrEmpty(Target) ? new StringBuilder() : new StringBuilder(Target);
            return NativeMethods.CredDelete(target, _credentialType, 0);
        }

        public static bool Delete(string target, CredentialType credentialType)
        {
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(target))
            {
                throw new ArgumentException("target must be specified to delete a credential.");
            }

            return NativeMethods.CredDelete(new StringBuilder(target), credentialType, 0);
        }

        public bool Load()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            IntPtr credPointer;
            bool result = NativeMethods.CredRead(Target, _credentialType, 0, out credPointer);
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
                throw new InvalidOperationException("Target must be specified to check existance of a credential.");
            }

            using (var existing = new GitCredentials { Target = Target })
            {
                return existing.Load();
            }
        }

        internal void LoadInternal(NativeMethods.CREDENTIAL credential)
        {
            Username = credential.UserName;
            if (credential.CredentialBlobSize > 0)
            {
                Password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }
            Target = credential.TargetName;
            _credentialType = (CredentialType)credential.Type;
            _persistenceType = (PersistenceType)credential.Persist;
            Description = credential.Comment;
            LastWriteTimeUtc = DateTime.FromFileTimeUtc(credential.LastWritten);
        }

        static void ValidatePasswordLength(byte[] passwordBytes)
        {
            if (passwordBytes.Length > maxPasswordLengthInBytes)
            {
                var message = string.Format(CultureInfo.InvariantCulture,
                    "The password length ({0} bytes) exceeds the maximum password length ({1} bytes).",
                    passwordBytes.Length,
                    maxPasswordLengthInBytes);
                throw new ArgumentOutOfRangeException(message);
            }
        }

        internal static SecureString CreateSecureString(string plainString)
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

        internal static string CreateString(SecureString secureString)
        {
            string str;
            var zero = IntPtr.Zero;
            if ((secureString == null) || (secureString.Length == 0))
            {
                return string.Empty;
            }
            try
            {
                zero = Marshal.SecureStringToBSTR(secureString);
                str = Marshal.PtrToStringBSTR(zero);
            }
            finally
            {
                if (zero != IntPtr.Zero)
                {
                    Marshal.ZeroFreeBSTR(zero);
                }
            }
            return str;
        }
    }
}
