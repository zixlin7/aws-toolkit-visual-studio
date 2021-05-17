using Amazon.Runtime.CredentialManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Credentials.Utils;
using System.Text;

namespace Amazon.AWSToolkit.Credentials.IO
{
    /// <summary>
    /// Class responsible for determining valid and invalid profiles as read by the <see cref="ICredentialFileReader"/>
    /// </summary>
    public class ProfileValidator
    {
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
            _fileReader.ProfileNames.ForEach(profileName =>
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
            if (profileOptions.ContainsSsoProperties())
            {
                return ValidateSso(profileOptions, profileName);
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

            return ValidateAssumeRoleChain(profileOptions, profileName);
        }

        /// <summary>
        /// Validates the chain of profiles referenced by assume role profiles
        /// </summary>
        private string ValidateAssumeRoleChain(CredentialProfileOptions profileOptions, string profileName)
        {
            var traversedProfileNames = new List<string>();

            var currentProfile = profileOptions;
            var currentProfileName = profileName;

            while (!string.IsNullOrWhiteSpace(currentProfile.RoleArn))
            {
                if (traversedProfileNames.Contains(currentProfileName))
                {
                    // Cycle detected
                    traversedProfileNames.Add(currentProfileName);
                    var cycle = string.Join(" -> ", traversedProfileNames);
                    return $"Cycle detected in profile references for Assume Role Profile {profileName}: {cycle}";
                }

                traversedProfileNames.Add(currentProfileName);

                if (string.IsNullOrWhiteSpace(profileOptions.SourceProfile))
                {
                    return
                        $"Assume Role Profile {currentProfileName} is missing required property {ProfilePropertyConstants.SourceProfile}";
                }

                var referencedProfileName = currentProfile.SourceProfile;
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
            var missingProperties = new List<string>();

            if (string.IsNullOrWhiteSpace(profileOptions.SsoAccountId))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoAccountId);
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SsoRegion))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoRegion);
            }

            if (string.IsNullOrWhiteSpace(profileOptions.SsoRoleName))
            {
                missingProperties.Add(ProfilePropertyConstants.SsoRoleName);
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
            return $"SSO-based profile {profileName} is missing one or more properties: {missingPropertiesStr}";
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
