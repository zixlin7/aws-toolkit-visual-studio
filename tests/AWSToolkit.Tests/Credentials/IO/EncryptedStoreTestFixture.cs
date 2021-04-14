using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Runtime.Internal.Settings;
using AWSToolkit.Tests.Credentials.IO;

namespace AWSToolkit.Tests.Credentials
{
    public class EncryptedStoreTestFixture :IDisposable
    {
        protected readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();

        private string OriginalSettingsStoreFolder { get; set; }
        private bool OriginalUserCryptAvailable { get; set; }
        private HashSet<string> OriginalEncryptedKeys { get; set; }

        public string DirectoryPath { get; private set; }
        public string MainFilename { get; private set; }

        public EncryptedStoreTestFixture(string filename)
            : this(filename, null)
        {
        }

        public EncryptedStoreTestFixture(string filename, string fileContents)
            : this(filename, fileContents, true)
        {
        }

        public EncryptedStoreTestFixture(string filename, string fileContents, bool userCryptAvailable)
        {
            MainFilename = filename;
            DirectoryPath = TestLocation.TestFolder;
            MockSettingsStoreFolder();
            MockEncryptedKeys();
            MockUserCryptAvailable(userCryptAvailable);

            SetFileContents(fileContents);
        }

        public void SetFileContents(string fileContents)
        {
            SetFileContents(MainFilename, fileContents);
        }

        public void SetFileContents(string filename, string fileContents)
        {
            if (fileContents != null)
            {
                File.WriteAllText(Path.Combine(DirectoryPath, filename), fileContents);
            }
        }

        public void Dispose()
        {
            // Don't clean up files if the test is being debugged.
            if (!Debugger.IsAttached)
            {
                TestLocation.Dispose();
            }
            UnMockSettingsStoreFolder();
            UnMockEncryptedKeys();
            UnMockUserCryptAvailable();
        }

        private void MockSettingsStoreFolder()
        {
            OriginalSettingsStoreFolder = (string)ReflectionHelpers.Invoke(typeof(PersistenceManager), "SettingsStoreFolder");
            ReflectionHelpers.Invoke(typeof(PersistenceManager), "SettingsStoreFolder", DirectoryPath);
        }

        private void UnMockSettingsStoreFolder()
        {
            ReflectionHelpers.Invoke(typeof(PersistenceManager), "SettingsStoreFolder", OriginalSettingsStoreFolder);
        }

        private void MockEncryptedKeys()
        {
            // mock _encryptedKeys to be empty so that we can easily look at the file for unit testing
            OriginalEncryptedKeys = (HashSet<string>)ReflectionHelpers.Invoke(typeof(PersistenceManager), "ENCRYPTEDKEYS");
            ReflectionHelpers.Invoke(typeof(PersistenceManager), "ENCRYPTEDKEYS", new HashSet<string>());
        }

        private void UnMockEncryptedKeys()
        {
            ReflectionHelpers.Invoke(typeof(PersistenceManager), "ENCRYPTEDKEYS", OriginalEncryptedKeys);
        }


        private void MockUserCryptAvailable(bool userCryptAvailable)
        {
            OriginalUserCryptAvailable = UserCrypto.IsUserCryptAvailable;
            ReflectionHelpers.Invoke(typeof(UserCrypto), "_isUserCryptAvailable", userCryptAvailable);
        }

        private void UnMockUserCryptAvailable()
        {
            ReflectionHelpers.Invoke(typeof(UserCrypto), "_isUserCryptAvailable", OriginalUserCryptAvailable);
        }
    }
}
