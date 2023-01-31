using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.IO
{
    public class ProfileValidatorSsoSessionTests : IDisposable
    {
        // SDK RegisterProfile does not allow us to add sso-session profiles; they need to be written to the
        // config file instead of the credentials file, so we add these to the config file directly
        private static readonly string ConfigFileContents =
            EmitSsoSession(CredentialProfileTestHelper.SsoSession.Valid.SsoSessionProfile)
            + EmitSsoSession(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoRegion)
            + EmitSsoSession(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoStartUrl);

        private static string EmitSsoSession(CredentialProfile credentialProfile)
            => $"[{credentialProfile.Name}]\n"
               + (credentialProfile.Options.SsoRegion != null
                   ? $"sso_region = {credentialProfile.Options.SsoRegion}\n"
                   : string.Empty)
               + (credentialProfile.Options.SsoStartUrl != null
                   ? $"sso_start_url = {credentialProfile.Options.SsoStartUrl}\n"
                   : string.Empty);

        private readonly SharedCredentialFileTestFixture _credentialsFixture = new SharedCredentialFileTestFixture();
        private readonly SharedCredentialFileReader _sharedCredentialFileReader;
        private readonly ProfileValidator _sut;

        public ProfileValidatorSsoSessionTests()
        {
            _credentialsFixture.SetupFileContents(string.Empty, ConfigFileContents);
            _credentialsFixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesTokenBasedSsoSession);
            _credentialsFixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesSsoBasedSsoSession);
            _credentialsFixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoRegion);
            _credentialsFixture.CredentialsFile.RegisterProfile(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoStartUrl);

            _sharedCredentialFileReader = new SharedCredentialFileReader(_credentialsFixture.CredentialsFile);

            _sut = new ProfileValidator(_sharedCredentialFileReader);
        }

        /// <summary>
        /// This test checks if the SDK credentials implementation has been updated to match expected/desired behaviors.
        /// Here, we want SDK calls like ListProfileNames to not throw an error if the credentials files contain a reference to a
        /// non-existing sso-session profile.
        /// If any checks in this test start to fail, the Toolkit will not choke when handling invalid credentials configurations
        /// containing a reference to an sso-session profile that does not exist.
        /// </summary>
        [Fact]
        public void Sdk_Query_Profiles_With_Missing_Reference_Check()
        {
            var profileOfInterest = CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceDoesNotExist;
            _credentialsFixture.CredentialsFile.RegisterProfile(profileOfInterest);

            var sseSessionProfileName = ProfileName.CreateSsoSessionProfileName(profileOfInterest.Options.SsoSession);

            Assert.Null(_sharedCredentialFileReader.GetCredentialProfileOptions(sseSessionProfileName));

            Assert.Throws<AmazonClientException>(() => _credentialsFixture.CredentialsFile.ListProfileNames());
        }

        [Fact]
        public void Validate_ValidCredentials()
        {
            var validationResults = _sut.Validate();

            Assert.True(validationResults.ValidProfiles.ContainsKey(CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesSsoBasedSsoSession.Name));
            Assert.True(validationResults.ValidProfiles.ContainsKey(CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesTokenBasedSsoSession.Name));
        }

        [Fact]
        public void Validate_ReferencingProfileContainsDifferentRegion()
        {
            var referencingProfile = CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.SsoProfileWithDifferentSsoRegions;
            _credentialsFixture.CredentialsFile.RegisterProfile(referencingProfile);

            var validationResults = _sut.Validate();

            var validationMessage = Assert.Contains(referencingProfile.Name,
                validationResults.InvalidProfiles as IDictionary<string, string>);

            Assert.Contains("cannot have a different SSO Region value", validationMessage);
        }

        [Fact]
        public void Validate_ReferencingProfileContainsDifferentStartUrl()
        {
            var referencingProfile = CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.SsoProfileWithDifferentSsoUrl;
            _credentialsFixture.CredentialsFile.RegisterProfile(referencingProfile);

            var validationResults = _sut.Validate();

            var validationMessage = Assert.Contains(referencingProfile.Name,
                validationResults.InvalidProfiles as IDictionary<string, string>);

            Assert.Contains("cannot have a different SSO StartUrl value", validationMessage);
        }

        // CredentialProfile - the profile to test as invalid
        // string - the expected missing property to be reported
        public static TheoryData<CredentialProfile, string> GetSsoSessionProfilesWithMissingProperties()
        {
            var data = new TheoryData<CredentialProfile, string>();

            data.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoRegion, nameof(ProfileProperties.SsoRegion));
            data.Add(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoStartUrl, nameof(ProfileProperties.SsoStartUrl));

            return data;
        }

        [Theory]
        [MemberData(nameof(GetSsoSessionProfilesWithMissingProperties))]
        public void Validate_SsoSession_MissingProperty(CredentialProfile profile, string expectedMissingProperty)
        {
            const string missingPropertiesSubstr = ", missing one or more properties";

            var validationResults = _sut.Validate();

            var validationMessage = Assert.Contains(profile.Name,
                validationResults.InvalidProfiles as IDictionary<string, string>);

            Assert.Contains(profile.Name, validationMessage);
            Assert.Contains(profile.Options.SsoSession, validationMessage);
            Assert.Contains(missingPropertiesSubstr, validationMessage);
            Assert.Contains(expectedMissingProperty, validationMessage);
        }

        [Fact]
        public void Validate_SsoSession_NonexistentReference()
        {
            var profile = CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles
                .ReferenceDoesNotExist;

            var profileName = profile.Name;
            _credentialsFixture.CredentialsFile.RegisterProfile(profile);

            var validationResult = _sut.Validate();

            var validationMessage = Assert.Contains(profileName,
                validationResult.InvalidProfiles as IDictionary<string, string>);
            Assert.Contains("references missing SSO Session profile", validationMessage);
        }

        [Fact]
        public void ShouldNotValidateSsoSessionProfiles()
        {
            var validationResults = _sut.Validate();

            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Valid.SsoSessionProfile.Name, validationResults.ValidProfiles.Keys);
            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoRegion.Name, validationResults.ValidProfiles.Keys);
            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoStartUrl.Name, validationResults.ValidProfiles.Keys);

            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Valid.SsoSessionProfile.Name, validationResults.InvalidProfiles.Keys);
            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoRegion.Name, validationResults.InvalidProfiles.Keys);
            Assert.DoesNotContain(CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoStartUrl.Name, validationResults.InvalidProfiles.Keys);
        }

        public void Dispose()
        {
            _credentialsFixture.Dispose();
        }
    }
}
