using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.Credentials.IO;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;
using log4net;
using static Amazon.Runtime.CredentialManagement.Internal.CredentialProfileType;
using Environment = System.Environment;

namespace Amazon.AWSToolkit.Credentials.Core 
{
    /// <summary>
    /// Class contains credential factory functionality common to <see cref= "SDKCredentialProviderFactory"/> and <see cref="SharedCredentialProviderFactory"/>
    /// </summary>
    public abstract class ProfileCredentialProviderFactory : ICredentialProviderFactory, ICredentialProfileProcessor
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ProfileCredentialProviderFactory));

        private bool _disposed = false;

        protected readonly IAWSToolkitShellProvider ToolkitShell;
        protected readonly ProfileValidator Validator;
        protected readonly IProfileHolder ProfileHolder;
        protected readonly ICredentialFileReader FileReader;
        protected readonly ICredentialFileWriter FileWriter;
        public event EventHandler<CredentialChangeEventArgs> CredentialsChanged;

        protected ProfileCredentialProviderFactory(IProfileHolder holder, ICredentialFileReader fileReader,
            ICredentialFileWriter fileWriter, IAWSToolkitShellProvider toolkitShell)
        {
            ToolkitShell = toolkitShell;
            ProfileHolder = holder;
            FileReader = fileReader;
            FileWriter = fileWriter;
            Validator = new ProfileValidator(FileReader);
        }

        public void Initialize()
        {
            LoadProfiles(true);
            SetupProfileWatcher();
        }

        public bool IsLoginRequired(ICredentialIdentifier id)
        {
            var name = id.ProfileName;
            var profile = ProfileHolder.GetProfile(name);
            if (profile == null)
            {
                return false;
            }

            var type = CredentialProfileTypeDetector.DetectProfileType(profile.Options);


            return type.Equals(CredentialProfileType.SSO) ||
                   type.Equals(CredentialProfileType.CredentialProcess) ||
                   IsMFACredentialType(type);
        }

        public bool Supports(ICredentialIdentifier id, AwsConnectionType connectionType)
        {
            var profile = ProfileHolder.GetProfile(id.ProfileName);
            if (profile == null)
            {
                return false;
            }

            switch (connectionType)
            {
                case AwsConnectionType.AwsToken:
                    return SupportsToken(profile);
                case AwsConnectionType.AwsCredentials:
                    return SupportsAwsCredentials(profile);
                default:
                    return false;
            }
        }

        private bool SupportsToken(CredentialProfile profile)
        {
            var type = CredentialProfileTypeDetector.DetectProfileType(profile.Options);

            return type.Equals(CredentialProfileType.SSO)
                   && profile.Options.ReferencesSsoSessionProfile();
        }

        private bool SupportsAwsCredentials(CredentialProfile profile)
        {
            var type = CredentialProfileTypeDetector.DetectProfileType(profile.Options);

            return !type.Equals(CredentialProfileType.SSO)
                   || !profile.Options.ReferencesSsoSessionProfile();
        }

        public void LoadProfiles(bool initialLoad)
        {
            //create a copy to avoid referenced dictionary changes
            var previousProfiles = ProfileHolder.GetCurrentProfiles()
                .ToDictionary(profiles => profiles.Key, profiles => profiles.Value);
            Profiles newProfiles;
            try
            {
                newProfiles = Validator.Validate();
            }
            catch (Exception e)
            {
                NotifyUserOfLoadFailure(e);
                LOGGER.Error("Failed to load AWS Profiles.", e);
                return;
            }

            EnsureUniqueKeyAssigned(newProfiles);

            ProfileHolder.UpdateProfiles(newProfiles.ValidProfiles);
            CreateCredentialChangeEvent(previousProfiles, newProfiles);
            NotifyUserOfResult(newProfiles, initialLoad);
        }

        public List<ICredentialIdentifier> GetCredentialIdentifiers()
        {
            return ProfileHolder.GetCurrentProfiles().Select(x => CreateCredentialIdentifier(x.Value)).ToList();
        }

        public ICredentialProfileProcessor GetCredentialProfileProcessor()
        {
            return this;
        }

        protected void CreateCredentialChangeEvent(Dictionary<string, CredentialProfile> previousProfiles,
            Profiles newProfiles)
        {
            var profilesAdded = new List<ICredentialIdentifier>();
            var profilesModified = new List<ICredentialIdentifier>();
            var profilesRemoved = new List<ICredentialIdentifier>();
            newProfiles.ValidProfiles.ToList().ForEach(x =>
            {
                var result = previousProfiles.TryGetValue(x.Key, out var profileValue);
                if (!result)
                {
                    profilesAdded.Add(CreateCredentialIdentifier(x.Value));
                }
                else
                {
                    if (!Equals(profileValue, x.Value))
                    {
                        profilesModified.Add(CreateCredentialIdentifier(x.Value));
                    }
                }
            });

            var remaining = previousProfiles.Where(x => !newProfiles.ValidProfiles.ContainsKey(x.Key))
                .Select(x => CreateCredentialIdentifier(x.Value)).ToList();

            profilesRemoved.AddRange(remaining);

            var args = new CredentialChangeEventArgs
            {
                Added = profilesAdded,
                Modified = profilesModified,
                Removed = profilesRemoved
            };

            RaiseCredentialsChanged(args);
        }

        public abstract string Id { get; }

        protected abstract void SetupProfileWatcher();

        public abstract ToolkitCredentials CreateToolkitCredentials(ICredentialIdentifier credentialIdentifier,
            ToolkitRegion region);

        protected abstract ICredentialIdentifier CreateCredentialIdentifier(CredentialProfile profile);

        protected abstract ToolkitCredentials CreateSaml(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier);

        protected void HandleFileChangeEvent(object sender, EventArgs e)
        {
             LoadProfiles(false);
        }

        ///raise a credential change event and updates CredentialManager and other subscribers  of updated credential identifiers.
        protected void RaiseCredentialsChanged(CredentialChangeEventArgs args)
        {
            CredentialsChanged?.Invoke(this, args);
        }

        protected ToolkitCredentials CreateToolkitCredentials(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            if (profile.Options.ReferencesSsoSessionProfile())
            {
                return CreateSsoSession(profile, credentialIdentifier);
            }

            if (profile.Options.ContainsSsoProperties())
            {
                return CreateSso(profile, credentialIdentifier);
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.EndpointName))
            {
                return CreateSaml(profile, credentialIdentifier);
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.RoleArn))
            {
                return CreateAssumeRole(profile, credentialIdentifier, region);
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.Token))
            {
                return CreateStaticSession(profile, credentialIdentifier);
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.AccessKey))
            {
                return CreateBasic(profile, credentialIdentifier);
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.CredentialProcess))
            {
                return CreateCredentialProcess(profile, credentialIdentifier);
            }

            throw new ArgumentException(
                $"Profile {profile.Name} is not using role-based, session-based, process-based, or basic credentials");
        }

        protected void NotifyUserOfLoadFailure(Exception e)
        {
            ToolkitFactory.Instance.ShellProvider.ShowError("Failed to load AWS Profiles: " + e.Message);
        }

        protected void NotifyUserOfResult(Profiles newProfiles, bool initialLoad)
        {
            var totalProfiles = newProfiles.ValidProfiles.Count + newProfiles.InvalidProfiles.Count;
            var notifyTitle = "Reloaded AWS Credential Profiles";
            var baseMessage = $"{notifyTitle}{Environment.NewLine}Profiles found: {totalProfiles}";
            //all credentials are valid
            if (newProfiles.InvalidProfiles.Count == 0)
            {
                //do not report credentials are loaded on start to avoid spam
                if (!initialLoad)
                {
                    //report no of profiles found with this dialog
                    LOGGER.Info(baseMessage);
                    ToolkitShell.OutputToHostConsole(baseMessage, false);
                }
            }
            else
            {
                var invalidProfiles = newProfiles.InvalidProfiles
                    .OrderBy(x => x.Key)
                    .ToList();

                var errorLogMessage = string.Join(Environment.NewLine,
                    invalidProfiles.Select(x => $"{x.Key}: {x.Value}"));
                var outputErrorMessage = string.Join(", ", invalidProfiles.Select(x => x.Key));
                LOGGER.Info(baseMessage);
                LOGGER.Error($"The following credentials could not be loaded: {Environment.NewLine}{errorLogMessage}");

                ToolkitShell.OutputToHostConsole(baseMessage, false);
                ToolkitShell.OutputToHostConsole(
                    $"The following credentials could not be loaded: {outputErrorMessage}. Check the Toolkit logs for more details.", false);
            }
        }

        /// <summary>
        /// Ensures unique key is assigned to each of the valid profiles found
        /// </summary>
        /// <param name="newProfiles"></param>
        protected void EnsureUniqueKeyAssigned(Profiles profiles)
        {
            if (profiles.ValidProfiles.Count != 0)
            {
                profiles.ValidProfiles.Values.ToList().ForEach(x => FileWriter.EnsureUniqueKeyAssigned(x));
            }
        }

        protected static void ValidateRequiredProperty(string propertyValue, string propertyName, string profileName)
        {
            if (string.IsNullOrWhiteSpace(propertyValue))
            {
                throw new ArgumentException($"Profile {profileName} is missing required property {propertyName}");
            }
        }

        private ToolkitCredentials CreateBasic(CredentialProfile profile, ICredentialIdentifier credentialIdentifier)
        {
            ValidateRequiredProperty(profile.Options.AccessKey, ProfilePropertyConstants.AccessKey, profile.Name);
            ValidateRequiredProperty(profile.Options.SecretKey, ProfilePropertyConstants.SecretKey, profile.Name);
            var credentials = new BasicAWSCredentials(profile.Options.AccessKey, profile.Options.SecretKey);
            return new ToolkitCredentials(credentialIdentifier, credentials);
        }

        private ToolkitCredentials CreateCredentialProcess(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier)
        {
            ValidateRequiredProperty(profile.Options.CredentialProcess, ProfilePropertyConstants.CredentialProcess,
                profile.Name);
            var credentials = new ProcessAWSCredentials(profile.Options.CredentialProcess);
            return new ToolkitCredentials(credentialIdentifier, credentials);
        }

        private ToolkitCredentials CreateStaticSession(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier)
        {
            ValidateRequiredProperty(profile.Options.AccessKey, ProfilePropertyConstants.AccessKey, profile.Name);
            ValidateRequiredProperty(profile.Options.SecretKey, ProfilePropertyConstants.SecretKey, profile.Name);
            ValidateRequiredProperty(profile.Options.Token, ProfilePropertyConstants.Token, profile.Name);
            var credentials = new SessionAWSCredentials(profile.Options.AccessKey, profile.Options.SecretKey,
                profile.Options.Token);

            return new ToolkitCredentials(credentialIdentifier, credentials);
        }

        private ToolkitCredentials CreateAssumeRole(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier, ToolkitRegion region)
        {
            if (region == null)
            {
                throw new ArgumentNullException(nameof(region));
            }

            ValidateRequiredProperty(profile.Options.RoleArn, ProfilePropertyConstants.RoleArn, profile.Name);

            var sourceCredentials = CreateAssumeRoleSourceCredentials(profile, region);

            var options = new ToolkitAssumeRoleAwsCredentials.ToolkitAssumeRoleAwsCredentialsOptions()
            {
                ExternalId = profile.Options.ExternalID,
                MfaSerialNumber = profile.Options.MfaSerial,
                Region = region.Id,
            };

            var roleSessionName = profile.Options.RoleSessionName;

            if (string.IsNullOrWhiteSpace(roleSessionName))
            {
                roleSessionName = $"aws-toolkit-visualstudio-{DateTime.UtcNow.Ticks}";
            }

            var credentials = new ToolkitAssumeRoleAwsCredentials(profile,
                sourceCredentials,
                roleSessionName,
                options,
                ToolkitShell);

            return new ToolkitCredentials(credentialIdentifier, credentials);
        }

        private AWSCredentials CreateAssumeRoleSourceCredentials(CredentialProfile profile, ToolkitRegion region)
        {
            if (string.IsNullOrWhiteSpace(profile.Options.SourceProfile) &&
                string.IsNullOrWhiteSpace(profile.Options.CredentialSource))
            {
                throw new ArgumentException(
                    $"Profile {profile.Name} is missing required property (one of {ProfilePropertyConstants.SourceProfile} and {ProfilePropertyConstants.CredentialSource})");
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.SourceProfile) &&
                !string.IsNullOrWhiteSpace(profile.Options.CredentialSource))
            {
                throw new ArgumentException(
                    $"Profile {profile.Name} can only have one of the {ProfilePropertyConstants.SourceProfile} and {ProfilePropertyConstants.CredentialSource} properties");
            }

            if (!string.IsNullOrWhiteSpace(profile.Options.SourceProfile))
            {
                return CreateSourceProfileCredentials(profile, region);
            }
            else if (!string.IsNullOrWhiteSpace(profile.Options.CredentialSource))
            {
                return CreateCredentialSourceCredentials(profile);
            }

            throw new ArgumentException($"Profile {profile.Name} is not a valid configuration.");
        }

        private AWSCredentials CreateSourceProfileCredentials(CredentialProfile profile, ToolkitRegion region)
        {
            var sourceProfileName = profile.Options.SourceProfile;
            var sourceProfile = ProfileHolder.GetProfile(sourceProfileName) ??
                                throw new ArgumentException(
                                    $"Source Profile not found: {sourceProfileName}, referenced by: {profile.Name}");

            var sourceCredentialsId = CreateCredentialIdentifier(sourceProfile);
            var toolkitCredentials = CreateToolkitCredentials(sourceProfile, sourceCredentialsId, region);

            if (!toolkitCredentials.Supports(AwsConnectionType.AwsCredentials))
            {
                throw new ArgumentException($"{profile.Name} references incompatible profile {sourceProfileName}");
            }

            return toolkitCredentials.GetAwsCredentials();
        }

        private static AWSCredentials CreateCredentialSourceCredentials(CredentialProfile profile)
        {
            if (!Enum.TryParse(profile.Options.CredentialSource, true,
                    out CredentialSourceType credentialSourceType))
            {
                throw new ArgumentException(
                    $"Profile {profile.Name} property {ProfilePropertyConstants.CredentialSource} is not set to a known value.");
            }

            if (credentialSourceType != CredentialSourceType.Ec2InstanceMetadata)
            {
                throw new ArgumentException(
                    $"Profile {profile.Name} property {ProfilePropertyConstants.CredentialSource} value {credentialSourceType} is not supported. Supported Values: {CredentialSourceType.Ec2InstanceMetadata}");
            }

            return ToolkitDefaultEc2InstanceCredentials.Instance;
        }

        private ToolkitCredentials CreateSsoSession(CredentialProfile profile,
            ICredentialIdentifier credentialIdentifier)
        {
            ValidateRequiredProperty(profile.Options.SsoRegion, ProfilePropertyConstants.SsoRegion, profile.Name);
            ValidateRequiredProperty(profile.Options.SsoStartUrl, ProfilePropertyConstants.SsoStartUrl, profile.Name);

            // Treat all profiles referenced using sso_session as Sono based credentials.
            // If we have to handle different credential providers on a per-startUrl basis,
            // we will need to redesign the system to handle different token provider resolution.
            var tokenProvider = SonoTokenProviderBuilder.Create()
                .WithCredentialIdentifier(credentialIdentifier)
                .WithToolkitShell(ToolkitShell)
                .WithTokenProviderRegion(RegionEndpoint.GetBySystemName(profile.Options.SsoRegion))
                .WithStartUrl(profile.Options.SsoStartUrl)
                .Build();

            return new ToolkitCredentials(credentialIdentifier, tokenProvider);
        }

        private ToolkitCredentials CreateSso(CredentialProfile profile, ICredentialIdentifier credentialIdentifier)
        {
            ValidateRequiredProperty(profile.Options.SsoAccountId, ProfilePropertyConstants.SsoAccountId, profile.Name);
            ValidateRequiredProperty(profile.Options.SsoRegion, ProfilePropertyConstants.SsoRegion, profile.Name);
            ValidateRequiredProperty(profile.Options.SsoRoleName, ProfilePropertyConstants.SsoRoleName, profile.Name);
            ValidateRequiredProperty(profile.Options.SsoStartUrl, ProfilePropertyConstants.SsoStartUrl, profile.Name);

            var credentials = new AwsSsoCredentials(profile, ToolkitShell);
            return new ToolkitCredentials(credentialIdentifier, credentials);
        }

        public void CreateProfile(ICredentialIdentifier identifier, ProfileProperties properties)
        {
            var profile = CreateCredentialProfile(identifier.ProfileName, properties);
            FileWriter.CreateOrUpdateProfile(profile);
        }

        public void RenameProfile(ICredentialIdentifier oldIdentifier, ICredentialIdentifier newIdentifier)
        {
            FileWriter.RenameProfile(oldIdentifier.ProfileName, newIdentifier.ProfileName);
        }

        public void DeleteProfile(ICredentialIdentifier identifier)
        {
            FileWriter.DeleteProfile(identifier.ProfileName);
        }

        public void UpdateProfile(ICredentialIdentifier identifier, ProfileProperties properties)
        {
            var profile = CreateCredentialProfile(identifier.ProfileName, properties);
            FileWriter.CreateOrUpdateProfile(profile);
        }

        public ProfileProperties GetProfileProperties(ICredentialIdentifier identifier)
        {
            var profile = FileReader.GetCredentialProfile(identifier.ProfileName);
            if (profile == null)
            {
                throw new ArgumentException($"Profile {identifier.ProfileName} is not found.");
            }

            return profile.AsProfileProperties();
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose anything managed by this class here
            }

            _disposed = true;
        }

        /// <summary>
        /// Method to create a <see cref="CredentialProfile"/> using <see cref="ProfileProperties"/>
        /// </summary>
        /// <param name="name"></param>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected CredentialProfile CreateCredentialProfile(string name, ProfileProperties properties)
        {
            if (properties == null) return null;
            var profileOptions = new CredentialProfileOptions
            {
                AccessKey = properties.AccessKey,
                SecretKey = properties.SecretKey
            };

            var profile = new CredentialProfile(name, profileOptions);
            if (!string.IsNullOrEmpty(properties.Region))
            {
                profile.Region = RegionEndpoint.GetBySystemName(properties.Region);
            }

            SetUniqueKey(profile, properties.UniqueKey);
            return profile;
        }

        private void SetUniqueKey(CredentialProfile profile, string uniqueKey)
        {
            if (!string.IsNullOrEmpty(uniqueKey))
            {
                CredentialProfileUtils.SetUniqueKey(profile, new Guid(uniqueKey));
            }
        }

        private bool IsMFACredentialType(CredentialProfileType? type)
        {
           return type.Equals(AssumeRoleMFA)
                || type.Equals(AssumeRoleMFASessionName)
                || type.Equals(AssumeRoleExternalMFA)
                || type.Equals(AssumeRoleExternalMFASessionName);
        }
    }
}
