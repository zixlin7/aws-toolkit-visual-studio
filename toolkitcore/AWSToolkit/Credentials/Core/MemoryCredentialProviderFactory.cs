using System;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime.CredentialManagement;

using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    internal class MemoryCredentialProviderFactory : ProfileCredentialProviderFactory
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(MemoryCredentialProviderFactory));

        public const string MemoryProfileFactoryId = nameof(MemoryCredentialProviderFactory);

        private readonly MemoryCredentialsFile _file;

        public override string Id => MemoryProfileFactoryId;

        private MemoryCredentialProviderFactory(IAWSToolkitShellProvider toolkitShell)
            : this(new ProfileHolder(), new MemoryCredentialsFile(), toolkitShell)
        {
        }

        private MemoryCredentialProviderFactory(IProfileHolder profileHolder, MemoryCredentialsFile file, IAWSToolkitShellProvider toolkitShell)
            : this(profileHolder, new MemoryCredentialFileReader(file), new MemoryCredentialFileWriter(file), toolkitShell)
        {
            _file = file;
        }

        internal MemoryCredentialProviderFactory(IProfileHolder holder, ICredentialFileReader fileReader, ICredentialFileWriter fileWriter,
            IAWSToolkitShellProvider toolkitShell) : base(holder, fileReader, fileWriter, toolkitShell)
        {
        }

        public override ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            var memoryId = credentialIdentifier as MemoryCredentialIdentifier ??
                throw new ArgumentException($"{MemoryProfileFactoryId} expected {nameof(MemoryCredentialIdentifier)}, but received {credentialIdentifier.GetType()}");

            var profile = ProfileHolder.GetProfile(memoryId.ProfileName) ??
                throw new InvalidOperationException($"Profile not found: {memoryId.ProfileName}");

            return CreateToolkitCredentials(profile, memoryId, region);
        }

        public static bool TryCreateFactory(IAWSToolkitShellProvider toolkitShell, out MemoryCredentialProviderFactory factory)
        {
            factory = new MemoryCredentialProviderFactory(toolkitShell);
            return true;
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(CredentialProfile profile)
        {
            return new MemoryCredentialIdentifier(profile.Name);
        }

        protected override ToolkitCredentials CreateSaml(CredentialProfile profile, ICredentialIdentifier credentialIdentifier)
        {
            throw new NotSupportedException($"{nameof(MemoryCredentialProviderFactory)} doesn't support SAML profiles.");
        }

        protected override void SetupProfileWatcher()
        {
            _file.Changed += HandleFileChangeEvent;
        }
    }
}
