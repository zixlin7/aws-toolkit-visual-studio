using System.Collections.Generic;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class ProfileValidatorTests
    {
        private readonly ProfileValidator _profileValidator;

        /// <summary>
        /// The mock file reader will serve up the profiles from <see cref="_profiles"/> that
        /// tests define as available (from <see cref="_availableProfiles"/>).
        /// </summary>
        private readonly Mock<ICredentialFileReader> _fileReader = new Mock<ICredentialFileReader>();
        private readonly List<string> _availableProfiles = new List<string>();
        private readonly Dictionary<string, CredentialProfile> _profiles = new Dictionary<string, CredentialProfile>();

        public ProfileValidatorTests()
        {
            _fileReader.SetupGet(mock => mock.ProfileNames).Returns(() => _availableProfiles);
            _fileReader.Setup(mock => mock.Load());
            _fileReader.Setup(mock => mock.GetCredentialProfileOptions(It.IsAny<string>()))
                .Returns<string>(profileName =>
                {
                    if (!_availableProfiles.Contains(profileName) || !_profiles.ContainsKey(profileName))
                    {
                        return null;
                    }

                    return _profiles[profileName]?.Options;
                });
            _fileReader.Setup(mock => mock.GetCredentialProfile(It.IsAny<string>()))
                .Returns<string>(profileName =>
                {
                    if (!_availableProfiles.Contains(profileName) || !_profiles.ContainsKey(profileName))
                    {
                        return null;
                    }

                    return _profiles[profileName];
                });

            _profileValidator = new ProfileValidator(_fileReader.Object);

            PopulateSampleProfiles();
        }

        [Fact]
        public void EmptyCredentials()
        {
            var profiles = _profileValidator.Validate();
            Assert.Empty(profiles.ValidProfiles);
            Assert.Empty(profiles.InvalidProfiles);
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.BasicProfileName)]
        [InlineData(CredentialProfileTestHelper.SessionProfileName)]
        [InlineData(CredentialProfileTestHelper.CredentialProcessProfileName)]
        [InlineData(CredentialProfileTestHelper.SSOProfileName)]
        public void ValidCredentials(string profileName)
        {
            _availableProfiles.Add(profileName);
            var profiles = _profileValidator.Validate();

            Assert.Single(profiles.ValidProfiles);
            Assert.Empty(profiles.InvalidProfiles);
        }

        [Theory]
        [InlineData(CredentialProfileTestHelper.InvalidBasicProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidSessionProfileName)]
        [InlineData(CredentialProfileTestHelper.InvalidProcessProfileName)]
        [InlineData("non_existing_profile")]
        [InlineData(CredentialProfileTestHelper.InvalidSdkProfileName)]
        public void InvalidCredentials(string invalidProfileName)
        {
            _availableProfiles.Add(CredentialProfileTestHelper.BasicProfileName);
            _availableProfiles.Add(invalidProfileName);

            var profiles = _profileValidator.Validate();

            Assert.Single(profiles.ValidProfiles);
            Assert.Single(profiles.InvalidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(invalidProfileName));
        }


        [Fact]
        public void AssumeRole()
        {
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRoleProfile.Options.SourceProfile);
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRoleProfileName);

            var profiles = _profileValidator.Validate();

            Assert.True(profiles.ValidProfiles.ContainsKey(CredentialProfileTestHelper.AssumeRoleProfileName));
            Assert.Empty(profiles.InvalidProfiles);
        }

        [Fact]
        public void AssumeRole_MissingSourceProfile()
        {
            var profileName = CredentialProfileTestHelper.InvalidAssumeRoleProfileNoSourceProfile.Name;
            _availableProfiles.Add(profileName);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(profileName));
            Assert.Contains("missing required property", profiles.InvalidProfiles[profileName]);
        }

        [Fact]
        public void AssumeRole_NonexistentReference()
        {
            var profileName = CredentialProfileTestHelper.InvalidAssumeRoleProfileBadSourceProfile.Name;
            _availableProfiles.Add(profileName);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(profileName));
            Assert.Contains("missing profile", profiles.InvalidProfiles[profileName]);
        }

        [Fact]
        public void AssumeRole_Cycle()
        {
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileB.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileC.Name);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(
                profiles.InvalidProfiles.ContainsKey(CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name));
            Assert.Contains("Cycle detected",
                profiles.InvalidProfiles[CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name]);

            var expectedCycleText = string.Join(" -> ",
                new string[]
                {
                    CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name,
                    CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileB.Name,
                    CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileC.Name,
                    CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name
                });
            Assert.Contains(expectedCycleText,
                profiles.InvalidProfiles[CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA.Name]);
        }

        [Fact]
        public void InvalidSsoCredentials()
        {
            const string missingPropertiesSubstr = "missing one or more properties";

            _availableProfiles.Add(CredentialProfileTestHelper.InvalidSSOProfileOnlyAccount.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidSSOProfileOnlyRegion.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidSSOProfileOnlyRole.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.InvalidSSOProfileOnlyUrl.Name);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.Equal(4, profiles.InvalidProfiles.Count);

            var validation = Assert.Contains(CredentialProfileTestHelper.InvalidSSOProfileOnlyAccount.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoRegion), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRoleName), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.InvalidSSOProfileOnlyRegion.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoAccountId), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRoleName), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.InvalidSSOProfileOnlyRole.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoAccountId), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRegion), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.InvalidSSOProfileOnlyUrl.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoAccountId), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRegion), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRoleName), validation);
        }

        private void PopulateSampleProfiles()
        {
            new List<CredentialProfile>()
            {
                CredentialProfileTestHelper.BasicProfile,
                CredentialProfileTestHelper.SessionProfile,
                CredentialProfileTestHelper.CredentialProcessProfile,
                CredentialProfileTestHelper.InvalidCredentialProcess,
                CredentialProfileTestHelper.InvalidBasicProfile,
                CredentialProfileTestHelper.InvalidSessionProfile,
                CredentialProfileTestHelper.InvalidSdkProfile,
                CredentialProfileTestHelper.AssumeRoleProfile,
                CredentialProfileTestHelper.InvalidAssumeRoleProfileNoSourceProfile,
                CredentialProfileTestHelper.InvalidAssumeRoleProfileBadSourceProfile,
                CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileA,
                CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileB,
                CredentialProfileTestHelper.InvalidAssumeRoleCycleProfileC,
                CredentialProfileTestHelper.SSOProfile,
                CredentialProfileTestHelper.InvalidSSOProfileOnlyAccount,
                CredentialProfileTestHelper.InvalidSSOProfileOnlyRegion,
                CredentialProfileTestHelper.InvalidSSOProfileOnlyRole,
                CredentialProfileTestHelper.InvalidSSOProfileOnlyUrl,
            }.ForEach(profile => _profiles[profile.Name] = profile);

            _profiles[CredentialProfileTestHelper.InvalidSdkProfileName] = null;
        }
    }
}
