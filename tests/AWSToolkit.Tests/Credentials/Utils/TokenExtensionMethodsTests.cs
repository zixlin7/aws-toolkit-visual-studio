using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Runtime.Credentials.Internal;
using Amazon.Util.Internal;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class FakeFileHandler : IFile
    {
        public bool FileExists;
        public string FileText = string.Empty;

        public bool Exists(string path)
        {
            return FileExists;
        }

        public string ReadAllText(string path)
        {
            return FileText;
        }

        public void WriteAllText(string path, string contents)
        {
            FileText = contents;
        }

        public Task<string> ReadAllTextAsync(string path, CancellationToken token = new CancellationToken())
        {
            return Task.FromResult(ReadAllText(path));
        }

        public Task WriteAllTextAsync(string path, string contents, CancellationToken token = new CancellationToken())
        {
            WriteAllText(path, contents);
            return Task.CompletedTask;
        }
    }

    public class FakeDirectoryHandler : IDirectory
    {
        private readonly TemporaryTestLocation _testLocation;

        public FakeDirectoryHandler(TemporaryTestLocation testLocation)
        {
            _testLocation = testLocation;
        }

        public DirectoryInfo CreateDirectory(string path)
        {
            return new DirectoryInfo(_testLocation.TestFolder);
        }
    }

    public class TokenExtensionMethodsTests : IDisposable
    {
        private readonly ToolkitContextFixture _toolkitContext = new ToolkitContextFixture();
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly FakeFileHandler _fileHandler = new FakeFileHandler();
        private readonly FakeDirectoryHandler _directoryHandler;
        private readonly ICredentialIdentifier _credentialsId = new SonoCredentialIdentifier("default");

        public TokenExtensionMethodsTests()
        {
            _directoryHandler = new FakeDirectoryHandler(_testLocation);
        }

        [Fact]
        public void ShouldNotBeValidWithNoCache()
        {
            // Set up the SDK to believe there is no token cache
            _fileHandler.FileExists = false;

            Assert.False(HasValidToken());
        }

        [Fact]
        public void ShouldNotBeValidWithExpiredToken()
        {
            SsoToken ssoToken = new SsoToken()
            {
                AccessToken = "access-token",
                ExpiresAt = DateTime.UtcNow.AddYears(-2),
                Region = SonoProperties.DefaultTokenProviderRegion.SystemName,
                StartUrl = SonoProperties.StartUrl,
            };

            _fileHandler.FileExists = true;
            _fileHandler.FileText = SsoTokenUtils.ToJson(ssoToken);

            Assert.False(HasValidToken());
        }

        // we do not exercise the "valid token" path - this requires an integration test, and UI automation (for the web login flow)

        public void Dispose()
        {
            _testLocation?.Dispose();
        }

        private bool HasValidToken()
        {
            return _credentialsId.HasValidToken(_toolkitContext.ToolkitHost.Object, _fileHandler, _directoryHandler);
        }
    }
}
