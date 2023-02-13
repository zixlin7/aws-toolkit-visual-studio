using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;

using Xunit;

namespace AWSToolkit.Tests.Credentials.Utils
{
    public class CredentialProfileOptionsExtensionMethodsTests
    {
        public static TheoryData<CredentialProfileOptions> GetIsResolvedWithSsoInputs()
        {
            return new TheoryData<CredentialProfileOptions>()
            {
                CredentialProfileTestHelper.Sso.ValidProfile.Options,
                CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesSsoBasedSsoSession.Options,
                CredentialProfileTestHelper.SsoSession.Valid.SdkHydratedProfileReferencesSsoBasedSsoSession.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.SsoProfileWithDifferentSsoRegions.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.SsoProfileWithDifferentSsoUrl.Options,
            };
        }

        [Theory]
        [MemberData(nameof(GetIsResolvedWithSsoInputs))]
        public void IsResolvedWithSso(CredentialProfileOptions profileOptions)
        {
            Assert.True(profileOptions.IsResolvedWithSso());
        }

        public static TheoryData<CredentialProfileOptions> GetIsNotResolvedWithSsoInputs()
        {
            return new TheoryData<CredentialProfileOptions>()
            {
                CredentialProfileTestHelper.SsoSession.Valid.SsoSessionProfile.Options,
                CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesTokenBasedSsoSession.Options,
                CredentialProfileTestHelper.SsoSession.Valid.SdkHydratedProfileReferencesTokenBasedSsoSession.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRegion.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasUrl.Options,
            };
        }

        [Theory]
        [MemberData(nameof(GetIsNotResolvedWithSsoInputs))]
        public void IsNotResolvedWithSso(CredentialProfileOptions profileOptions)
        {
            Assert.False(profileOptions.IsResolvedWithSso());
        }

        public static TheoryData<CredentialProfileOptions> GetIsResolvedWithTokenProviderInputs()
        {
            return new TheoryData<CredentialProfileOptions>()
            {
                CredentialProfileTestHelper.SsoSession.Valid.ProfileReferencesTokenBasedSsoSession.Options,
                CredentialProfileTestHelper.SsoSession.Valid.SdkHydratedProfileReferencesTokenBasedSsoSession.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceDoesNotExist.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoRegion.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionReferencingProfiles.ReferenceMissingSsoStartUrl.Options,
            };
        }

        [Theory]
        [MemberData(nameof(GetIsResolvedWithTokenProviderInputs))]
        public void IsResolvedWithTokenProvider(CredentialProfileOptions profileOptions)
        {
            Assert.True(profileOptions.IsResolvedWithTokenProvider());
        }

        public static TheoryData<CredentialProfileOptions> GetIsNotResolvedWithTokenProviderInputs()
        {
            return new TheoryData<CredentialProfileOptions>()
            {
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoRegion.Options,
                CredentialProfileTestHelper.SsoSession.Invalid.SsoSessionProfiles.MissingSsoStartUrl.Options,
                CredentialProfileTestHelper.Sso.ValidProfile.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasAccount.Options,
                CredentialProfileTestHelper.Sso.Invalid.MissingProperties.HasRole.Options,
            };
        }

        [Theory]
        [MemberData(nameof(GetIsNotResolvedWithTokenProviderInputs))]
        public void IsNotResolvedWithTokenProvider(CredentialProfileOptions profileOptions)
        {
            Assert.False(profileOptions.IsResolvedWithTokenProvider());
        }
    }
}
