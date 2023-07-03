using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Settings;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

using Amazon.Runtime.CredentialManagement.Internal;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Exceptions;

namespace Amazon.AWSToolkit.Credentials.Utils
{
    public static class ProfilePropertiesExtensionMethods
    {
        /// <summary>
        /// Determines the generalized type of credentials represented by the given profile properties.
        /// </summary>
        public static CredentialType GetCredentialType(this ProfileProperties properties)
        {
            if (properties == null) { return CredentialType.Undefined; }

            // Spec: If SSO account id or SSO role name are filled in, the profile is considered an SSO based profile
            if (!string.IsNullOrWhiteSpace(properties.SsoAccountId) ||
                !string.IsNullOrWhiteSpace(properties.SsoRoleName))
            {
                return CredentialType.SsoProfile;
            }

            // Otherwise if SsoSession is filled in, the profile is treated like a token provider
            if (!string.IsNullOrWhiteSpace(properties.SsoSession))
            {
                return CredentialType.BearerToken;
            }

            // If any other SSO field is filled in, the profile is considered an SSO based profile
            if (!string.IsNullOrWhiteSpace(properties.SsoRegion) ||
                !string.IsNullOrWhiteSpace(properties.SsoStartUrl))
            {
                return CredentialType.SsoProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.EndpointName))
            {
                return CredentialType.AssumeSamlRoleProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.RoleArn))
            {
                if (!string.IsNullOrWhiteSpace(properties.MfaSerial))
                {
                    return CredentialType.AssumeMfaRoleProfile;
                }
                else if (!string.IsNullOrWhiteSpace(properties.CredentialSource) && properties.CredentialSource == CredentialSourceType.Ec2InstanceMetadata.ToString())
                {
                    return CredentialType.AssumeEc2InstanceRoleProfile;
                }
                else
                {
                    return CredentialType.AssumeRoleProfile;
                }
            }

            if (!string.IsNullOrWhiteSpace(properties.Token))
            {
                return CredentialType.StaticSessionProfile;
            }

            if (!string.IsNullOrWhiteSpace(properties.AccessKey)) { return CredentialType.StaticProfile; }

            if (!string.IsNullOrWhiteSpace(properties.CredentialProcess))
            {
                return CredentialType.CredentialProcessProfile;
            }

            return CredentialType.Unknown;
        }

        public static Task<bool> ValidateConnectionAsync(this ProfileProperties @this, ToolkitContext toolkitContext)
        {
            // Use MemoryCredential classes to validate connection of ProfileProperties without having to persist a Credential

            // Setup and initialize memory provider factory and mapping
            if (!MemoryCredentialProviderFactory.TryCreateFactory(toolkitContext.ToolkitHost, out var factory))
            {
                // This should never happen
                throw new ToolkitException($"Cannot create {nameof(MemoryCredentialProviderFactory)}", ToolkitException.CommonErrorCode.UnexpectedError);
            }
            factory.Initialize();

            var factoryMapping = new Dictionary<string, ICredentialProviderFactory>()
            {
                { MemoryCredentialProviderFactory.MemoryProfileFactoryId, factory }
            };

            // Setup a connection manager isolated from all others to host MemoryCredentials
            var connectionManager = new AwsConnectionManager(
                toolkitContext.ConnectionManager.IdentityResolver,
                new CredentialManager(factoryMapping),
                toolkitContext.TelemetryLogger,
                toolkitContext.RegionProvider,
                new AppDataToolkitSettingsRepository());

            // Setup a handler to listen for ConnectionStateChanged to update TaskCompletionSource to support blocking/await
            var taskSource = new TaskCompletionSource<bool>();

            EventHandler<ConnectionStateChangeArgs> handler = null;
            handler = (object sender, ConnectionStateChangeArgs e) =>
            {
                if (e.State.IsTerminal || e.State.GetType() == typeof(ConnectionState.IncompleteConfiguration))
                {
                    connectionManager.ConnectionStateChanged -= handler;
                    taskSource.SetResult(ConnectionState.IsValid(e.State));
                }
            };
            connectionManager.ConnectionStateChanged += handler;

            // Create a memory profile from ProfileProperties and attempt to connect to it with the connection manager we just setup
            var credId = new MemoryCredentialIdentifier(@this.Name);
            var region = toolkitContext.RegionProvider.GetRegion(@this.Region);
            connectionManager.CredentialManager.CredentialSettingsManager.CreateProfile(credId, @this);
            connectionManager.ChangeConnectionSettings(credId, region);

            return taskSource.Task;
        }
    }
}
