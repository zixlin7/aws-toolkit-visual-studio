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

        public static IEnumerable<object[]> GetValidProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Valid.Token.Name };
            yield return new object[] { CredentialProfileTestHelper.CredentialProcess.ValidProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.Sso.ValidProfile.Name };
            yield return new object[] { CredentialProfileTestHelper.Saml.ValidProfile.Name };
        }

        [Theory]
        [MemberData(nameof(GetValidProfileNames))]
        public void ValidCredentials(string profileName)
        {
            _availableProfiles.Add(profileName);
            var profiles = _profileValidator.Validate();

            Assert.Single(profiles.ValidProfiles);
            Assert.Empty(profiles.InvalidProfiles);
        }

        public static IEnumerable<object[]> GetInvalidProfileNames()
        {
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey.Name };
            yield return new object[] { CredentialProfileTestHelper.CredentialProcess.InvalidProfile.Name };
            yield return new object[] { "non_existing_profile" };
            yield return new object[] { CredentialProfileTestHelper.Saml.InvalidProfile.Name };
        }

        [Theory]
        [MemberData(nameof(GetInvalidProfileNames))]
        public void InvalidCredentials(string invalidProfileName)
        {
            _availableProfiles.Add(CredentialProfileTestHelper.Basic.Valid.AccessAndSecret.Name);
            _availableProfiles.Add(invalidProfileName);

            var profiles = _profileValidator.Validate();

            Assert.Single(profiles.ValidProfiles);
            Assert.Single(profiles.InvalidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(invalidProfileName));
        }


        [Fact]
        public void AssumeRole()
        {
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRole.ValidProfile.Options.SourceProfile);
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRole.ValidProfile.Name);

            var profiles = _profileValidator.Validate();

            Assert.True(profiles.ValidProfiles.ContainsKey(CredentialProfileTestHelper.AssumeRole.ValidProfile.Name));
            Assert.Empty(profiles.InvalidProfiles);
        }

        [Fact]
        public void AssumeRole_MissingSourceProfile()
        {
            var profileName = CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.Missing.Name;
            _availableProfiles.Add(profileName);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(profileName));
            Assert.Contains("missing required property", profiles.InvalidProfiles[profileName]);
        }

        [Fact]
        public void AssumeRole_NonexistentReference()
        {
            var profileName = CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.BadReference.Name;
            _availableProfiles.Add(profileName);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(profiles.InvalidProfiles.ContainsKey(profileName));
            Assert.Contains("missing profile", profiles.InvalidProfiles[profileName]);
        }

        [Fact]
        public void AssumeRole_Cycle()
        {
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileB.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileC.Name);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.True(
                profiles.InvalidProfiles.ContainsKey(CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name));
            Assert.Contains("Cycle detected",
                profiles.InvalidProfiles[CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name]);

            var expectedCycleText = string.Join(" -> ",
                new string[]
                {
                    CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name,
                    CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileB.Name,
                    CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileC.Name,
                    CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name
                });
            Assert.Contains(expectedCycleText,
                profiles.InvalidProfiles[CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA.Name]);
        }

        [Fact]
        public void InvalidSsoCredentials()
        {
            const string missingPropertiesSubstr = "missing one or more properties";

            _availableProfiles.Add(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRegion.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole.Name);
            _availableProfiles.Add(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasUrl.Name);

            var profiles = _profileValidator.Validate();

            Assert.Empty(profiles.ValidProfiles);
            Assert.Equal(4, profiles.InvalidProfiles.Count);

            var validation = Assert.Contains(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoRegion), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRoleName), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRegion.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoAccountId), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRoleName), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole.Name,
                profiles.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains(missingPropertiesSubstr, validation);
            Assert.Contains(nameof(ProfileProperties.SsoAccountId), validation);
            Assert.Contains(nameof(ProfileProperties.SsoRegion), validation);
            Assert.Contains(nameof(ProfileProperties.SsoStartUrl), validation);

            validation = Assert.Contains(CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasUrl.Name,
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
                CredentialProfileTestHelper.Basic.Valid.AccessAndSecret,
                CredentialProfileTestHelper.Basic.Valid.Token,
                CredentialProfileTestHelper.CredentialProcess.ValidProfile,
                CredentialProfileTestHelper.Saml.ValidProfile,
                CredentialProfileTestHelper.CredentialProcess.InvalidProfile,
                CredentialProfileTestHelper.Basic.Invalid.MissingAccessKey,
                CredentialProfileTestHelper.Basic.Invalid.MissingSecretKey,
                CredentialProfileTestHelper.Basic.Invalid.TokenMissingSecretKey,
                CredentialProfileTestHelper.Saml.InvalidProfile,
                CredentialProfileTestHelper.AssumeRole.ValidProfile,
                CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.Missing,
                CredentialProfileTestHelper.AssumeRole.Invalid.SourceProfile.BadReference,
                CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileA,
                CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileB,
                CredentialProfileTestHelper.AssumeRole.Invalid.CyclicReference.ProfileC,
                CredentialProfileTestHelper.Sso.ValidProfile,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRegion,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasUrl,
            }.ForEach(profile => _profiles[profile.Name] = profile);
        }
    }
}
