﻿using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace AWSToolkit.Tests.Credentials
{
    public class CredentialProfileTestHelper
    {
        public static class Basic
        {
            public static class Valid
            {
                public static readonly CredentialProfile AccessAndSecret = new CredentialProfile("basic_profile",
                    new CredentialProfileOptions { AccessKey = "access_key", SecretKey = "secret_key" });


                public static readonly CredentialProfile Token = new CredentialProfile("session_profile",
                    new CredentialProfileOptions
                    {
                        AccessKey = "session_access_key",
                        SecretKey = "session_secret_key",
                        Token = "token"
                    });
            }

            public static class Invalid
            {
                public static readonly CredentialProfile MissingSecretKey = new CredentialProfile("invalid_basic_profile",
                    new CredentialProfileOptions { AccessKey = "access_key" });

                public static readonly CredentialProfile TokenMissingSecretKey = new CredentialProfile(
                    "invalid_session_profile",
                    new CredentialProfileOptions { AccessKey = "session_access_key", Token = "token" });

                public static readonly CredentialProfile MissingAccessKey = new CredentialProfile("invalid_profile",
                    new CredentialProfileOptions { SecretKey = "secret_key" });
            }
        }

        public static class CredentialProcess
        {
            public static readonly CredentialProfile ValidProfile = new CredentialProfile(
                "process_profile", new
                    CredentialProfileOptions
                    { CredentialProcess = "process" });

            public static readonly CredentialProfile InvalidProfile =
                new CredentialProfile("invalid_process_profile", new CredentialProfileOptions());
        }

        public static class Saml
        {
            public static readonly CredentialProfile ValidProfile = new CredentialProfile("saml_profile",
                new CredentialProfileOptions
                {
                    RoleArn = "role_arn",
                    EndpointName = "endpoint_name",
                });

            public static readonly CredentialProfile InvalidProfile = new CredentialProfile("invalid_saml_profile",
                new CredentialProfileOptions { EndpointName = "endpoint" });
        }

        public static class Mfa
        {
            public static class Valid
            {
                public static readonly CredentialProfile MfaReference = new CredentialProfile("mfa_profile",
                    new CredentialProfileOptions
                    {
                        MfaSerial = "mfa_serial",
                        RoleArn = "role_arn",
                        SourceProfile = "basic_profile"
                    });

                public static readonly CredentialProfile ExternalSession = new CredentialProfile("mfa_external_session_profile",
                    new CredentialProfileOptions
                    {
                        MfaSerial = "mfa_serial",
                        RoleArn = "role_arn",
                        SourceProfile = "basic_profile",
                        RoleSessionName = "role_session",
                        ExternalID = "external_id"
                    });
            }
        }

        public static class Sso
        {
            public static readonly CredentialProfile ValidProfile = new CredentialProfile("sso_profile",
                new CredentialProfileOptions
                {
                    SsoAccountId = "account-id",
                    SsoRegion = "sso-region",
                    SsoRoleName = "sso-role",
                    SsoStartUrl = "sso-url",
                });

            public static class Invalid
            {
                public static class MissingProperties
                {
                    public static readonly CredentialProfile HasAccount = new CredentialProfile("sso-only-account",
                        new CredentialProfileOptions
                        {
                            SsoAccountId = "account-id",
                        });

                    public static readonly CredentialProfile HasRegion = new CredentialProfile("sso-only-region",
                        new CredentialProfileOptions
                        {
                            SsoRegion = "sso-region",
                        });

                    public static readonly CredentialProfile HasRole = new CredentialProfile("sso-only-role",
                        new CredentialProfileOptions
                        {
                            SsoRoleName = "sso-role",
                        });

                    public static readonly CredentialProfile HasUrl = new CredentialProfile("sso-only-url",
                        new CredentialProfileOptions
                        {
                            SsoStartUrl = "sso-url",
                        });
                }
            }
        }

        public static class SsoSession
        {
            public static class Valid
            {
                // This sample represents what the SDK hydrates when loading a profile
                public static readonly CredentialProfile SdkResolvedSsoSessionReferencingProfile = new CredentialProfile("my_sso_bearer_session_from_sdk",
                    new CredentialProfileOptions
                    {
                        SsoSession = "sso_bearer",
                        SsoRegion = "sso-region",
                        SsoStartUrl = "sso-url",
                    });

                // This sample represents a raw definition as it would appear in the file
                public static readonly CredentialProfile SsoSessionReferencingProfile = new CredentialProfile("my_sso_bearer_session",
                    new CredentialProfileOptions
                    {
                        SsoSession = "sso_bearer",
                    });

                public static readonly CredentialProfile SsoSessionProfile = new CredentialProfile("sso-session sso_bearer",
                    new CredentialProfileOptions
                    {
                        SsoRegion = "sso-region",
                        SsoStartUrl = "sso-url",
                    });
            }

            public static class Invalid
            {
                public static class SsoSessionReferencingProfiles
                {
                    public static readonly CredentialProfile ProfileHasAdditionalSsoProperties = new CredentialProfile("my_sso_bearer_session_extra_props",
                        new CredentialProfileOptions
                        {
                            SsoSession = "sso_bearer",
                            SsoRegion = "sso-region",
                            SsoStartUrl = "sso-url",
                        });

                    public static readonly CredentialProfile ReferenceDoesNotExist = new CredentialProfile("sso_session_reference_not_exist",
                        new CredentialProfileOptions
                        {
                            SsoSession = "sso_session_does_not_exist",
                        });

                    public static readonly CredentialProfile ReferenceMissingSsoRegion = new CredentialProfile("sso_session_reference_missing_region",
                        new CredentialProfileOptions
                        {
                            SsoSession = "missing_region",
                        });

                    public static readonly CredentialProfile ReferenceMissingSsoStartUrl = new CredentialProfile("sso_session_reference_missing_starturl",
                        new CredentialProfileOptions
                        {
                            SsoSession = "missing_starturl",
                        });
                }

                public static class SsoSessionProfiles
                {
                    public static readonly CredentialProfile MissingSsoRegion = new CredentialProfile(
                        "sso-session missing_region",
                        new CredentialProfileOptions { SsoStartUrl = "sso-url", });

                    public static readonly CredentialProfile MissingSsoStartUrl = new CredentialProfile(
                        "sso-session missing_starturl",
                        new CredentialProfileOptions { SsoRegion = "sso-region", });
                }
            }
        }

        public static class AssumeRole
        {
            public static class Valid
            {
                public static readonly CredentialProfile SourceProfile = CreateSampleProfile("assume_role_source_profile");
                public static readonly CredentialProfile CredentialSource = new CredentialProfile("assume_role_credential_source",
                    new CredentialProfileOptions
                    {
                        RoleArn = "role-arn",
                        CredentialSource = CredentialSourceType.Ec2InstanceMetadata.ToString(),
                    });
            }

            public static class Invalid
            {
                public static class SourceProfile
                {
                    public static readonly CredentialProfile Missing = new CredentialProfile("assume_role_no_source",
                        new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = string.Empty });
                    public static readonly CredentialProfile BadReference = new CredentialProfile("assume_role_bad_source",
                        new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "some-fake-profile" });

                    public static readonly CredentialProfile AndCredentialSource = new CredentialProfile(
                        "assume_role_both_sources",
                        new CredentialProfileOptions
                        {
                            RoleArn = "role-arn",
                            SourceProfile = "some-fake-profile",
                            CredentialSource = CredentialSourceType.Ec2InstanceMetadata.ToString()
                        });
                }

                public static class CredentialSource
                {
                    public static readonly CredentialProfile InvalidValue = new CredentialProfile(
                        "assume_role_invalid_source",
                        new CredentialProfileOptions
                        {
                            RoleArn = "role-arn",
                            CredentialSource = "non-enum-value"
                        });

                    public static readonly CredentialProfile Unsupported = new CredentialProfile(
                        "assume_role_environment",
                        new CredentialProfileOptions
                        {
                            RoleArn = "role-arn",
                            CredentialSource = CredentialSourceType.Environment.ToString()
                        });
                }

                /// <summary>
                /// Assume Role Cycle: A -> B -> C -> A
                /// </summary>
                public static class CyclicReference
                {
                    public static readonly CredentialProfile ProfileA = new CredentialProfile("invalid_assume_role_cycle_profile_a",
                        new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_b" });
                    public static readonly CredentialProfile ProfileB = new CredentialProfile("invalid_assume_role_cycle_profile_b",
                        new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_c" });
                    public static readonly CredentialProfile ProfileC = new CredentialProfile("invalid_assume_role_cycle_profile_c",
                        new CredentialProfileOptions { RoleArn = "role-arn", SourceProfile = "invalid_assume_role_cycle_profile_a" });
                }
            }

            public static CredentialProfile CreateSampleProfile(string profileName)
            {
                return new CredentialProfile(profileName,
                    new CredentialProfileOptions
                    {
                        RoleArn = "role-arn",
                        SourceProfile = "basic_profile",
                        ExternalID = "some-external-id",
                        RoleSessionName = "some-role-session",
                    });
            }
        }
    }
}
