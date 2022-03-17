using System;
using System.Globalization;
using System.Windows;

using Amazon.AWSToolkit.Credentials.Control;
using Amazon.AWSToolkit.Shared;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.Internal;
using Amazon.Runtime.SharedInterfaces;
using log4net;

namespace Amazon.AWSToolkit.Credentials.Core
{
    /// <summary>
    /// Overrides AssumeRoleAWSCredentials to allow the caller to specify which region
    /// the Assume Role operation should take place.
    ///
    /// This class is intended as a stopgap measure until the SDK supports a way to
    /// specify regions when Assuming Roles.
    /// </summary>
    public class ToolkitAssumeRoleAwsCredentials : AssumeRoleAWSCredentials
    {
        /// <summary>
        /// Extension of AssumeRoleAWSCredentialsOptions to provide a region when Assuming Roles
        /// </summary>
        public class ToolkitAssumeRoleAwsCredentialsOptions : AssumeRoleAWSCredentialsOptions
        {
            /// <summary>
            /// The region code (eg: "us-west-2") where to perform the Assume Role operation
            /// </summary>
            public string Region { get; set; }
        }

        private static readonly ILog Logger = LogManager.GetLogger(typeof(ToolkitAssumeRoleAwsCredentials));

        private readonly RegionEndpoint DefaultSTSClientRegion = RegionEndpoint.USEast1;
        private CredentialProfile _profile;
        private IAWSToolkitShellProvider _toolkitShell;

        public new ToolkitAssumeRoleAwsCredentialsOptions Options { get; private set; }

        public ToolkitAssumeRoleAwsCredentials(CredentialProfile profile, AWSCredentials sourceCredentials,
            string roleSessionName, ToolkitAssumeRoleAwsCredentialsOptions options, IAWSToolkitShellProvider toolkitShell)
            : base(sourceCredentials, profile.Options.RoleArn, roleSessionName, options)
        {
            _profile = profile;
            _toolkitShell = toolkitShell;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            RegisterMfaCallBack();
        }

        protected override CredentialsRefreshState GenerateNewCredentials()
        {
            // This is the divergence from AssumeRoleAWSCredentials
            var configuredRegion = Options.Region;
            // END divergence. The rest of this function is the same parent class implementation.
            var region = string.IsNullOrEmpty(configuredRegion)
                ? DefaultSTSClientRegion
                : RegionEndpoint.GetBySystemName(configuredRegion);
            ICoreAmazonSTS coreSTSClient = null;
            try
            {
                var stsConfig = ServiceClientHelpers.CreateServiceConfig(ServiceClientHelpers.STS_ASSEMBLY_NAME,
                    ServiceClientHelpers.STS_SERVICE_CONFIG_NAME);
                stsConfig.RegionEndpoint = region;

                if (Options != null && Options.ProxySettings != null)
                {
                    stsConfig.SetWebProxy(Options.ProxySettings);
                }

                coreSTSClient = ServiceClientHelpers.CreateServiceFromAssembly<ICoreAmazonSTS>(
                    ServiceClientHelpers.STS_ASSEMBLY_NAME, ServiceClientHelpers.STS_SERVICE_CLASS_NAME,
                    SourceCredentials, stsConfig);
            }
            catch (Exception e)
            {
                var msg = string.Format(CultureInfo.CurrentCulture,
                    "Assembly {0} could not be found or loaded. This assembly must be available at runtime to use Amazon.Runtime.AssumeRoleAWSCredentials.",
                    ServiceClientHelpers.STS_ASSEMBLY_NAME);
                var exception = new InvalidOperationException(msg, e);
                Logger.Error(exception.Message, exception);
                throw exception;
            }
            // This is the divergence from AssumeRoleAWSCredentials
            AssumeRoleImmutableCredentials credentials = null;
            try
            {
                credentials = coreSTSClient.CredentialsFromAssumeRoleAuthentication(RoleArn, RoleSessionName, Options);
            }
            catch (Exception e)
            {
                _toolkitShell.OutputToHostConsole(
                    $"Error authenticating AWS Assume Role profile {_profile.Name}: {e.Message}", false);
                //concatenate inner exception message if present (helps display MFA based inner exception messages)
                if (e.InnerException != null)
                {
                    Logger.Error(e);
                    throw new Exception($"{e.Message}{Environment.NewLine}{e.InnerException.Message}", e);
                }
                throw;
            }
            // END divergence.
            Logger.InfoFormat("New credentials created for assume role that expire at {0}",
                credentials.Expiration.ToString("yyyy-MM-ddTHH:mm:ss.fffffffK", CultureInfo.InvariantCulture));
            return new CredentialsRefreshState(credentials, credentials.Expiration);
        }

        /// <summary>
        /// Registers  MFA Token Callback handler
        /// </summary>
        private void RegisterMfaCallBack()
        {
            if (!string.IsNullOrEmpty(Options.MfaSerialNumber))
            {
                Options.MfaTokenCodeCallback = PromptForMfaToken;
            }
        }

        /// <summary>
        /// Callback handler which prompts to enter mfa token
        /// </summary>
        /// <returns></returns>
        private string PromptForMfaToken()
        {
            try
            {
                var viewModel = new MfaPromptViewModel
                {
                    MfaSerialNumber = Options.MfaSerialNumber, ProfileName = _profile.Name
                };

                MfaPromptControl control;
                _toolkitShell.ExecuteOnUIThread(() =>
                {
                    control = new MfaPromptControl(viewModel);
                    if (!_toolkitShell.ShowInModalDialogWindow(control, MessageBoxButton.OKCancel))
                    {
                        throw new InvalidOperationException("MFA Login cancelled");
                    }

                    _toolkitShell.OutputToHostConsole($"MFA Login started for profile {_profile.Name}", false);
                });
                return viewModel.MfaToken;
            }
            catch (Exception e)
            {
                _toolkitShell.OutputToHostConsole($"Login failed for AWS MFA based profile {_profile.Name}: {e.Message}", false);
                throw;
            }
        }
    }
}
