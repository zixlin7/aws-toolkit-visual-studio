using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace AWSToolkit.Tests.Credentials.Core
{
    /// <summary>
    /// Test only derived class of ProfileCredentialProviderFactory
    /// </summary>
    public class TestProfileCredentialProviderFactory: ProfileCredentialProviderFactory
    {
        public TestProfileCredentialProviderFactory() : base(null,null, null, null)
        {
        }
        public TestProfileCredentialProviderFactory(ICredentialFileWriter fileWriter) : base(null, null, fileWriter, null)
        {
        }

        public TestProfileCredentialProviderFactory(ICredentialFileReader fileReader, string tokenCacheFolder) : base(null, fileReader, null, null, tokenCacheFolder)
        {
        }

        /// <summary>
        /// Exposes base class protected method which creates a credential change event
        /// </summary>
        /// <param name="previousProfiles"></param>
        /// <param name="newProfiles"></param>
        public void ExposedCreateCredentialChangeEvent(Dictionary<string, CredentialProfile> previousProfiles,
            Profiles newProfiles)
        {
            base.CreateCredentialChangeEvent(previousProfiles, newProfiles);
        }

        /// <summary>
        /// Exposes base class protected method which ensures unique key is assigned to each valid profile
        /// </summary>
        /// <param name="newProfiles"></param>
        public void ExposedEnsureUniqueKeyAssigned(Profiles newProfiles)
        {
           base.EnsureUniqueKeyAssigned(newProfiles);
        }

        /// <summary>
        /// Exposes base class protected method which creates credential profile using profile properties
        /// </summary>
        /// <param name="name"></param>
        /// <param name="properties"></param>
        public CredentialProfile ExposeCreateCredentialProfile(string name, ProfileProperties properties)
        {
            return base.CreateCredentialProfile(name, properties);
        }

        public override string Id { get; }

        protected override void SetupProfileWatcher()
        {
            throw new NotImplementedException();
        }

        public override ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            throw new NotImplementedException();
        }

        protected override ICredentialIdentifier CreateCredentialIdentifier(CredentialProfile profile)
        {
            return new SharedCredentialIdentifier(profile.Name);
        }

        protected override ToolkitCredentials CreateSaml(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier)
        {
            throw new NotImplementedException();
        }
    }
}
