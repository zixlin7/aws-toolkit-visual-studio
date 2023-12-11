using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoBearerTokenConnectedStepViewModel : StepViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoAwsCredentialConnectedStepViewModel));

        private IAddEditProfileWizardHost _host => ServiceProvider.RequireService<IAddEditProfileWizardHost>();

        private IConfigurationDetails _configDetails => ServiceProvider.RequireService<IConfigurationDetails>(CredentialType.SsoProfile.ToString());

        public override async Task ViewShownAsync()
        {
            await base.ViewShownAsync();

            if (_addEditProfileWizard.FeatureType == FeatureType.CodeWhisperer)
            {
                _configDetails.ProfileProperties.SsoRegistrationScopes = SonoProperties.CodeWhispererScopes;
            }

            var credId = _configDetails.IsAddNewProfile ?
                await SaveAsync() :
                _configDetails.SelectedCredentialIdentifier;

            _host.ShowCompleted(credId);
        }

        private async Task<ICredentialIdentifier> SaveAsync()
        {
            var actionResults = ActionResults.CreateFailed();

            try
            {
                _addEditProfileWizard.InProgress = true;

                var p = _configDetails.ProfileProperties;
                var saveAsyncResults = await _addEditProfileWizard.SaveAsync(p, CredentialFileType.Shared);
                actionResults = saveAsyncResults.ActionResults;

                if (!actionResults.Success)
                {
                    throw new ConnectionToolkitException($"Cannot save profile {p.Name}", ConnectionToolkitException.ConnectionErrorCode.UnexpectedErrorOnSave, actionResults.Exception);
                }

                return saveAsyncResults.CredentialIdentifier;
            }
            catch (Exception ex)
            {
                var msg = "Failed to save IAM Identity Center profile.";
                _logger.Error(msg, ex);
                ToolkitContext.ToolkitHost.ShowError(msg);
                actionResults = ActionResults.CreateFailed(ex);

                return null;
            }
            finally
            {
                _addEditProfileWizard.RecordAuthAddedConnectionsMetric(actionResults, actionResults.Success ? 1 : 0,
                    actionResults.Success ?
                        new HashSet<string>() { EnabledAuthConnectionTypes.IamIdentityCenterCodeWhisperer } :
                        Enumerable.Empty<string>());

                _addEditProfileWizard.InProgress = false;
            }
        }
    }
}
