using Amazon.Runtime.CredentialManagement;

namespace AWSToolkit.Tests.Credentials
{
    public class CredentialProfileTestHelper
    {
        public const string BasicProfileName = "basic_profile";
        public const string CredentialProcessProfileName = "process_profile";
        public const string SessionProfileName = "session_profile";
        public const string AssumeRoleProfileName = "assume_role_profile";
        public const string InvalidSessionProfileName = "invalid_session_profile";
        public const string InvalidProcessProfileName = "invalid_process_profile";
        public const string InvalidBasicProfileName = "invalid_basic_profile";
        public const string InvalidSdkProfileName = "invalid_sdk_profile";
        public const string InvalidAssumeRoleProfileNoSourceProfileName = "assume_role_no_source";
        public const string InvalidAssumeRoleProfileBadSourceProfileName = "assume_role_bad_source";
        public const string MFAProfileName = "mfa_profile";
        public const string MFAExternalSessionProfileName = "mfa_external_session_profile";
        public const string SSOProfileName = "sso_profile";
        public const string InvalidProfileName = "invalid_profile";

        public static readonly CredentialProfile BasicProfile = new CredentialProfile(BasicProfileName,
            new CredentialProfileOptions {AccessKey = "access_key", SecretKey = "secret_key"});

        public static readonly CredentialProfile InvalidBasicProfile = new CredentialProfile(InvalidBasicProfileName,
            new CredentialProfileOptions {AccessKey = "access_key"});

        public static readonly CredentialProfile InvalidSessionProfile = new CredentialProfile(
            InvalidSessionProfileName,
            new CredentialProfileOptions {AccessKey = "session_access_key", Token = "token"});


        public static readonly CredentialProfile SessionProfile = new CredentialProfile(SessionProfileName,
            new CredentialProfileOptions
            {
                AccessKey = "session_access_key", SecretKey = "session_secret_key", Token = "token"
            });

        public static readonly CredentialProfile InvalidProfile = new CredentialProfile(InvalidProfileName,
            new CredentialProfileOptions {SecretKey = "secret_key"});

        public static readonly CredentialProfile InvalidCredentialProcess =
            new CredentialProfile(InvalidProcessProfileName, new CredentialProfileOptions());

        public static readonly CredentialProfile CredentialProcessProfile = new CredentialProfile(
            CredentialProcessProfileName, new
                CredentialProfileOptions {CredentialProcess = "process"});

        public static readonly CredentialProfile InvalidSdkProfile = new CredentialProfile(InvalidSdkProfileName,
            new CredentialProfileOptions {AccessKey = "sdk_access_key", SecretKey = "sdk_secret_key"});

        public static readonly CredentialProfile MFAProfile = new CredentialProfile(MFAProfileName,
            new CredentialProfileOptions
            {
                MfaSerial = "mfa_serial", RoleArn = "role_arn", SourceProfile = "basic_profile"
            });

        public static readonly CredentialProfile MFAExternalSessionProfile = new CredentialProfile(MFAExternalSessionProfileName,
            new CredentialProfileOptions
            {
                MfaSerial = "mfa_serial",
                RoleArn = "role_arn",
                SourceProfile = "basic_profile",
                RoleSessionName = "role_session",
                ExternalID = "external_id"
            });

        public static readonly CredentialProfile SSOProfile = new CredentialProfile(SSOProfileName,
            new CredentialProfileOptions
            {
                SsoAccountId = "account-id", SsoRegion = "sso-region", SsoRoleName = "sso-role", SsoStartUrl = "sso-url",
            });

        public static readonly CredentialProfile InvalidSSOProfileOnlyAccount = new CredentialProfile("sso-only-account",
            new CredentialProfileOptions
            {
                SsoAccountId = "account-id",
            });

        public static readonly CredentialProfile InvalidSSOProfileOnlyRegion = new CredentialProfile("sso-only-region",
            new CredentialProfileOptions
            {
                SsoRegion = "sso-region",
            });

        public static readonly CredentialProfile InvalidSSOProfileOnlyRole = new CredentialProfile("sso-only-role",
            new CredentialProfileOptions
            {
                SsoRoleName = "sso-role",
            });

        public static readonly CredentialProfile InvalidSSOProfileOnlyUrl = new CredentialProfile("sso-only-url",
            new CredentialProfileOptions
            {
                SsoStartUrl = "sso-url",
            });

        public static readonly CredentialProfile AssumeRoleProfile = CreateSampleAssumeRoleProfile(AssumeRoleProfileName);

        public static readonly CredentialProfile InvalidAssumeRoleProfileNoSourceProfile = new CredentialProfile(InvalidAssumeRoleProfileNoSourceProfileName,
            new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = string.Empty });
        public static readonly CredentialProfile InvalidAssumeRoleProfileBadSourceProfile = new CredentialProfile(InvalidAssumeRoleProfileBadSourceProfileName,
            new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "some-fake-profile" });

        // Assume Role Cycle: A -> B -> C -> A
        public static readonly CredentialProfile InvalidAssumeRoleCycleProfileA = new CredentialProfile("invalid_assume_role_cycle_profile_a",
            new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_b" });
        public static readonly CredentialProfile InvalidAssumeRoleCycleProfileB = new CredentialProfile("invalid_assume_role_cycle_profile_b",
            new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_c" });
        public static readonly CredentialProfile InvalidAssumeRoleCycleProfileC = new CredentialProfile("invalid_assume_role_cycle_profile_c",
            new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_a" });

        public static CredentialProfile CreateSampleAssumeRoleProfile(string profileName)
        {
            return new CredentialProfile(profileName,
                new CredentialProfileOptions
                {
                    RoleArn = "role-arn",
                    SourceProfile = BasicProfileName,
                    ExternalID = "some-external-id",
                    RoleSessionName = "some-role-session",
                });
        }
    }
}
