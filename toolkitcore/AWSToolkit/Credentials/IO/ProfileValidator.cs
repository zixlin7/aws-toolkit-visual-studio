using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Class responsible for determining valid and invalid profiles as read by the <see cref="ICredentialFileReader"/>
    /// </summary>
    public class ProfileValidator
    {
        static readonly string[] AssumeRoleDependentProperties = { ProfilePropertyConstants.CredentialSource, ProfilePropertyConstants.SourceProfile };
        private readonly ICredentialFileReader _fileReader;

        public ProfileValidator(ICredentialFileReader fileReader)
        {
            _fileReader = fileReader;
        }

        public Profiles Validate()
        {
            var validProfiles = new Dictionary<string, CredentialProfile>();
            var invalidProfiles = new Dictionary<string, string>();

            _fileReader.Load();
            _fileReader.ProfileNames
                .Where(profileName => !ProfileName.IsSsoSession(profileName))
                .ToList()
                .ForEach(profileName =>
                {
                    try
                    {
                        var profileOptions = _fileReader.GetCredentialProfileOptions(profileName);
                        if (profileOptions != null)
                        {
                            var message = ValidateProfile(profileOptions, profileName);
                            if (string.IsNullOrWhiteSpace(message))
                            {
                                var credentialProfile = _fileReader.GetCredentialProfile(profileName);
                                if (credentialProfile != null)
                                {
                                    validProfiles[profileName] = credentialProfile;
                                }
                                else
                                {
                                    invalidProfiles[profileName] =
                                        $"Profile {profileName} is not recognized by the AWS SDK. The Toolkit is unable to use it.";
                                }
                            }
                            else
                            {
                                invalidProfiles[profileName] = message;
                            }
                        }
                        else
                        {
                            invalidProfiles[profileName] =
                                $"Profile {profileName} is not recognized by the AWS SDK. The Toolkit is unable to use it.";
                        }
                    }
                    catch (Exception e)
                    {
                        invalidProfiles[profileName] = e.Message;
                    }
                });

            return new Profiles {ValidProfiles = validProfiles, InvalidProfiles = invalidProfiles};
        }

        private string ValidateProfile(CredentialProfileOptions profileOptions, string profileName)
        {
            // eg: profile is [sso-session foo]
            if (ProfileName.IsSsoSession(profileName))
            {
                return ValidateReferencedSsoSession(profileOptions, profileName, "(credentials file)");
            }

            if (profileOptions.IsResolvedWithSso())
            {
                return ValidateSso(profileOptions, profileName);
            }

            if (profileOptions.IsResolvedWithTokenProvider())
            {
                return ValidateTokenProvider(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.EndpointName))
            {
                return ValidateSaml(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.RoleArn))
            {
                return ValidateAssumeRole(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.Token))
            {
                return ValidateStaticSession(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.AccessKey))
            {
                return ValidateBasic(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.CredentialProcess))
            {
                return ValidateCredentialProcess(profileOptions, profileName);
            }

            return $"Profile {profileName} is not using role-based, session-based, process-based, or basic credentials";
        }

        private string ValidateBasic(CredentialProfileOptions profileOptions, string profileName)
        {
            var errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(profileOptions.AccessKey))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.AccessKey}");
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SecretKey))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.SecretKey}");
            }

            return errorMessage.ToString();
        }

        private string ValidateStaticSession(CredentialProfileOptions profileOptions, string profileName)
        {
            var errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(profileOptions.AccessKey))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.AccessKey}");
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SecretKey))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.SecretKey}");
            }

            if (string.IsNullOrWhiteSpace(profileOptions.Token))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.Token}");
            }

            return errorMessage.ToString();
        }

        private string ValidateCredentialProcess(CredentialProfileOptions profileOptions, string profileName)
        {
            var errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(profileOptions.CredentialProcess))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.CredentialProcess}");
            }

            return errorMessage.ToString();
        }

        private string ValidateAssumeRole(CredentialProfileOptions profileOptions, string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileOptions.RoleArn))
            {
                return $"Profile {profileName} is missing required property {ProfilePropertyConstants.RoleArn}";
            }

            var validation = ValidateAssumeRoleReferencingProperties(profileOptions, profileName);

            if (!string.IsNullOrWhiteSpace(validation))
            {
                return validation;
            }

            return ValidateAssumeRoleChain(profileOptions, profileName);
        }

        private string ValidateAssumeRoleReferencingProperties(CredentialProfileOptions profileOptions, string profileName)
        {
            if (!string.IsNullOrWhiteSpace(profileOptions.SourceProfile) && !string.IsNullOrWhiteSpace(profileOptions.CredentialSource))
            {
                return
                    $"Assume Role Profile {profileName} can only have one of the following properties: {string.Join(", ", AssumeRoleDependentProperties)}";
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SourceProfile) && string.IsNullOrWhiteSpace(profileOptions.CredentialSource))
            {
                return
                    $"Assume Role Profile {profileName} is missing one of the following properties: {string.Join(", ", AssumeRoleDependentProperties)}";
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.CredentialSource))
            {
                return ValidateAssumeRoleCredentialSource(profileOptions, profileName);
            }

            if (!string.IsNullOrWhiteSpace(profileOptions.SourceProfile))
            {
                if (_fileReader.GetCredentialProfileOptions(profileOptions.SourceProfile) == null)
                {
                    return
                        $"Assume Role Profile {profileName} references missing profile {profileOptions.SourceProfile}";
                }
            }

            return null;
        }

        private static string ValidateAssumeRoleCredentialSource(
            CredentialProfileOptions profileOptions, string currentProfileName)
        {
            if (!Enum.TryParse(profileOptions.CredentialSource, true, out CredentialSourceType credentialSourceType))
            {
                return $"Assume Role Profile {currentProfileName} does not have a valid value for {ProfilePropertyConstants.CredentialSource}.";
            }

            if (credentialSourceType != CredentialSourceType.Ec2InstanceMetadata)
            {
                return $"Assume Role Profile {currentProfileName} property {ProfilePropertyConstants.CredentialSource} only supports {CredentialSourceType.Ec2InstanceMetadata}.";
            }

            return null;
        }

        /// <summary>
        /// Validates the chain of profiles referenced by assume role profiles
        /// </summary>
        private string ValidateAssumeRoleChain(CredentialProfileOptions profileOptions, string profileName)
        {
            var traversedProfileNames = new List<string>();
            string PrintCycleChain() => string.Join(" -> ", traversedProfileNames);

            var currentProfile = profileOptions;
            var currentProfileName = profileName;

            while (!string.IsNullOrWhiteSpace(currentProfile.RoleArn))
            {
                if (traversedProfileNames.Contains(currentProfileName))
                {
                    // Cycle detected
                    traversedProfileNames.Add(currentProfileName);
                    return $"Cycle detected in profile references for Assume Role Profile {profileName}: {PrintCycleChain()}";
                }

                traversedProfileNames.Add(currentProfileName);

                var currentProfileReferencingValidation = ValidateAssumeRoleReferencingProperties(currentProfile, currentProfileName);
                if (!string.IsNullOrWhiteSpace(currentProfileReferencingValidation))
                {
                    return
                        $"Assume Role Profile {profileName} references {currentProfileName} ({PrintCycleChain()}), which fails validation: {currentProfileReferencingValidation}";
                }

                var referencedProfileName = currentProfile.SourceProfile;
                if (string.IsNullOrWhiteSpace(referencedProfileName))
                {
                    return null;
                }

                var referencedProfile = _fileReader.GetCredentialProfileOptions(referencedProfileName);

                if (referencedProfile == null)
                {
                    return
                        $"Assume Role Profile {currentProfileName} references missing profile {referencedProfileName}";
                }

                currentProfileName = referencedProfileName;
                currentProfile = referencedProfile;
            }

            return null;
        }

        private string ValidateSso(CredentialProfileOptions profileOptions, string profileName)
        {
            // Validate the reference to an sso-session profile, if there is one
            if (profileOptions.ReferencesSsoSessionProfile())
            {
                var validation = ValidateSsoSessionReference(profileOptions, profileName);
                if (!string.IsNullOrWhiteSpace(validation)) { return validation; }
            }

            return ValidateIamIdentityCenter(profileOptions, profileName);
        }

        /// <summary>
        /// Validates profiles that reference "sso-session foo" profiles using a sso_session property
        /// </summary>
        private string ValidateSsoSessionReference(CredentialProfileOptions profileOptions, string profileName)
        {
            // Check that the referenced sso_session exists
            var referencedProfileName = ProfileName.CreateSsoSessionProfileName(profileOptions.SsoSession);
            var referencedProfile = _fileReader.GetCredentialProfileOptions(referencedProfileName);

            if (referencedProfile == null)
            {
                return $"{profileName} references missing SSO Session profile: {referencedProfileName}";
            }

            // Spec: If the profile and the (referenced) sso-session both contain sso_region or sso_start_url then the values
            // in both the profile and sso-session must match.
            bool IsValueDifferent(string a, string b) => !string.IsNullOrWhiteSpace(a) && !string.IsNullOrWhiteSpace(b) && !a.Equals(b);

            if (IsValueDifferent(profileOptions.SsoRegion, referencedProfile.SsoRegion))
            {
                return
                    $"{profileName} cannot have a different SSO Region value ({profileOptions.SsoRegion}) than the referenced SSO Session ({referencedProfile.SsoRegion})";
            }

            if (IsValueDifferent(profileOptions.SsoStartUrl, referencedProfile.SsoStartUrl))
            {
                return
                    $"{profileName} cannot have a different SSO StartUrl value ({profileOptions.SsoStartUrl}) than the referenced SSO Session ({referencedProfile.SsoStartUrl})";
            }

            // Check that the referenced sso_session is valid
            return ValidateReferencedSsoSession(referencedProfile, referencedProfileName, profileName);
        }

        private string ValidateReferencedSsoSession(CredentialProfileOptions profileOptions, string profileName, string referencingProfileName)
        {
            var missingProperties = new List<string>();

            if (string.IsNullOrWhiteSpace(profileOptions.SsoRegion))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoRegion);
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SsoStartUrl))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoStartUrl);
            }

            if (!missingProperties.Any())
            {
                return string.Empty;
            }

            var missingPropertiesStr = string.Join(", ", missingProperties.OrderBy(x => x));
            return $"{referencingProfileName}: References SSO Session {profileName}, missing one or more properties: {missingPropertiesStr}";
        }

        /// <summary>
        /// Validates the AWS IAM Identity Center (AWS SSO) set of properties
        /// </summary>
        private string ValidateIamIdentityCenter(CredentialProfileOptions profileOptions, string profileName)
        {
            var missingProperties = new List<string>();

            if (string.IsNullOrWhiteSpace(profileOptions.SsoAccountId))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoAccountId);
            }

            if (!profileOptions.ReferencesSsoSessionProfile() && string.IsNullOrWhiteSpace(profileOptions.SsoRegion))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoRegion);
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SsoRoleName))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoRoleName);
            }

            if (!profileOptions.ReferencesSsoSessionProfile() && string.IsNullOrWhiteSpace(profileOptions.SsoStartUrl))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoStartUrl);
            }

            if (!missingProperties.Any())
            {
                return string.Empty;
            }

            var missingPropertiesStr = string.Join(", ", missingProperties.OrderBy(x => x));
            return $"SSO-based profile {profileName} is missing one or more properties: {missingPropertiesStr}";
        }

        private string ValidateTokenProvider(CredentialProfileOptions profileOptions, string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName))
            {
                return $"Profile {profileName} is missing required property {ProfilePropertyConstants.SsoSession}";
            }

            // Check that the referenced sso_session exists
            var referencedProfileName = ProfileName.CreateSsoSessionProfileName(profileOptions.SsoSession);
            var referencedProfile = _fileReader.GetCredentialProfileOptions(referencedProfileName);

            if (referencedProfile == null)
            {
                return $"{profileName} references missing SSO Session profile: {referencedProfileName}";
            }

            // Check that the referenced sso_session is valid
            return ValidateReferencedSsoSession(referencedProfile, referencedProfileName, profileName);
        }

        private string ValidateSaml(CredentialProfileOptions profileOptions, string profileName)
        {
            var errorMessage = new StringBuilder();
            if (string.IsNullOrWhiteSpace(profileOptions.EndpointName))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.EndpointName}");
            }

            if (string.IsNullOrWhiteSpace(profileOptions.RoleArn))
            {
                errorMessage.AppendLine(
                    $"Profile {profileName} is missing required property {ProfilePropertyConstants.RoleArn}");
            }

            return errorMessage.ToString();
        }
    }
}
