using System;
using System.IO;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Runtime.CredentialManagement;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class SharedCredentialFileTestFixture : IDisposable
    {
        protected readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        private const string CredentialsFileName = "credentials";
        private const string ConfigFileName = "config";

        private readonly string _credentialsFilePath;
        private readonly string _configFilePath;
        private string _directoryPath;
        public SharedCredentialsFile CredentialsFile { get; set; }

        public SharedCredentialFileTestFixture()
        {
            _directoryPath = TestLocation.TestFolder;
            _credentialsFilePath = Path.Combine(TestLocation.TestFolder, CredentialsFileName);
            _configFilePath = Path.Combine(TestLocation.TestFolder, ConfigFileName);
            CredentialsFile = new SharedCredentialsFile(_credentialsFilePath);
            SetupFileContents(null);
        }

        public void SetupFileContents(string credentialsFileContents, string configFileContents = null)
        {
            if (string.IsNullOrEmpty(credentialsFileContents))
            {
                credentialsFileContents = "";
            }

            File.WriteAllText(_credentialsFilePath, credentialsFileContents);

            if (configFileContents != null)
            {
                File.WriteAllText(_configFilePath, configFileContents);
            }
        }

        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }
}
