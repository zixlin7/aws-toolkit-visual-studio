using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;

using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.LegacyDeployment
{
    /// <summary>
    /// Implements the retrieval and population of application settings for an app
    /// deployed too or about to be deployed to Beanstalk
    /// </summary>
    public class AppOptionsPageController : CommonAppOptionsPageControllerBase
    {
        // used to stop us reloaded app config on user paging back and forth
        string _lastSeenKey = string.Empty;

        // this is a 'cache' of the queried config settings for an app on redeployment, to help us 
        // detect if the user really did change anything (to avoid restriction in current beanstalk 
        // api whereby we have to update config and version in separate calls)
        string _lastConfigValues = null;

        protected override bool OnQueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return IsWizardInBeanstalkMode;
        }

        protected override void OnPageActivated(AWSWizardConstants.NavigationReason navigationReason, bool isRedeploying)
        {
            _pageUI.EmailControlsVisible = true;
            if (IsWizardInBeanstalkMode && !(bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy])
            {
                // if we're redeploying, we'll set up health check field after we recover the environment settings, with
                // fallback to having it visible in case of retrieval error
                _pageUI.UseEC2InstanceStatusForHealthChecks = DeploymentWizardHelper.IsSingleInstanceEnvironment(HostingWizard);
                return;
            }

            _pageUI.UseEC2InstanceStatusForHealthChecks = false;
        }

        // only called if all tests on the common base have succeeded
        protected override bool OnQueryForwardsNavigationAllowed 
        { 
            get 
            {
                if (!string.IsNullOrEmpty(_pageUI.NotificationEmail))
                    return _pageUI.NotificationEmailIsValid;
                else
                    return true; 
            } 
        }

        protected override void OnStorePageData()
        {
            HostingWizard[BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_NotificationEmail] = _pageUI.NotificationEmail;

            // we don't care what's changed, just that something has (trying to avoid two UpdateEnvironment calls
            // due to restriction in beanstalk's current api)
            bool configChanged = false;
            if (_lastConfigValues != null)  // null on initial app deployment
            {
                string currentConfigValues = ConstructConfigValues();
                configChanged = string.Compare(currentConfigValues, _lastConfigValues) != 0;
            }

            HostingWizard[BeanstalkDeploymentWizardProperties.AppOptionsProperties.propkey_AppOptionsUpdated] = configChanged;
        }

        bool IsWizardInBeanstalkMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.BeanstalkServiceName;
            }
        }

        /// <summary>
        /// If redeploying, download the last-used config, decrypt and populate app environment and
        /// credential parameters appropriately (using a worker)
        /// </summary>
        /// <param name="isRedeploying"></param>
        protected override void PopulateAppParams(bool isRedeploying)
        {
            if (isRedeploying)
            {
                ExistingServiceDeployment deployment
                        = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance] as ExistingServiceDeployment;

                string appName;
                if (deployment != null)
                    appName = deployment.DeploymentName;
                else
                    appName = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

                var account = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var region = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
                string envName = HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string;

                string lastSeenKey = ConstructLastSeenKey(account, region.SystemName, appName, envName);

                if (string.Compare(lastSeenKey, _lastSeenKey) != 0)
                {
                    _lastSeenKey = lastSeenKey;
                    _pageUI.DataLoadPending = true;
                    new QueryEnvironmentSettingsWorker(account,
                                                       region,
                                                       appName,
                                                       envName,
                                                       LOGGER,
                                                       new QueryEnvironmentSettingsWorker.DataAvailableCallback(FetchSettingsWorkerCompleted));
                }
            }
            else
            {
                _pageUI.SetAppParams(null, DefaultHealthCheckUrl, null, false);
                _lastConfigValues = null;
            }

            TestForwardTransitionEnablement();
        }

        string ConstructLastSeenKey(AccountViewModel account, string region, string appName, string envName)
        {
            var c = account.Credentials.GetCredentials();
            return string.Format("{0}#{1}#{2}#{3}", c.AccessKey, region, appName, envName);
        }

        void FetchSettingsWorkerCompleted(IEnumerable<ConfigurationOptionSetting> settings)
        {
            string lookupResultsMsg = null;
            Dictionary<string, string> appParams = new Dictionary<string, string>();
            string healthCheckUrl = DefaultHealthCheckUrl;
            string notificationEmail = string.Empty;

            try
            {
                // these don't get sent to the page but held back and used in StorePageData
                // if user select 'use last keys' on page
                _lastDeployedAccessKey = string.Empty;
                _lastDeployedSecretKey = string.Empty;

                if (settings != null && settings.Count<ConfigurationOptionSetting>() > 0)
                {
                    foreach (ConfigurationOptionSetting optionSetting in settings)
                    {
                        if (optionSetting.Namespace == "aws:elasticbeanstalk:application:environment")
                        {
                            if (optionSetting.OptionName == "AWS_ACCESS_KEY_ID")
                                _lastDeployedAccessKey = optionSetting.Value;
                            else if (optionSetting.OptionName == "AWS_SECRET_KEY")
                                _lastDeployedSecretKey = optionSetting.Value;
                            else if (optionSetting.OptionName.StartsWith("PARAM"))
                                appParams.Add(optionSetting.OptionName, optionSetting.Value);

                            continue;
                        }

                        if (optionSetting.Namespace == BeanstalkConstants.ENVIRONMENT_NAMESPACE)
                        {
                            if (optionSetting.OptionName == BeanstalkConstants.ENVIRONMENTTYPE_OPTION)
                            {
                                _pageUI.UseEC2InstanceStatusForHealthChecks 
                                    = !string.IsNullOrEmpty(optionSetting.Value) 
                                        && optionSetting.Value.Equals(BeanstalkConstants.EnvType_SingleInstance, 
                                                                      StringComparison.Ordinal);
                                continue;
                            }
                        }

                        if (optionSetting.Namespace == "aws:elasticbeanstalk:application")
                        {
                            if (optionSetting.OptionName == "Application Healthcheck URL")
                                healthCheckUrl = optionSetting.Value;

                            continue;
                        }

                        if (optionSetting.Namespace == "aws:elasticbeanstalk:sns:topics")
                        {
                            if (optionSetting.OptionName == "Notification Endpoint")
                                notificationEmail = optionSetting.Value;

                            continue;
                        }
                    }
                }
                else
                    lookupResultsMsg = "An error occurred, or an instance did not respond, whilst querying for the settings that were used previously for this deployment; settings unavailable.";
            }
            catch (Exception e)
            {
                lookupResultsMsg = string.Format("Error whilst reading prior deployment settings - {0}", e.Message);
                LOGGER.ErrorFormat(lookupResultsMsg);
            }
            finally
            {
                _pageUI.SetAppParams(appParams, healthCheckUrl, notificationEmail, true);
                _lastConfigValues = ConstructConfigValues();
                _pageUI.DataLoadPending = false;
            }

            if (!string.IsNullOrEmpty(lookupResultsMsg))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Pre-deployment Inspection Failed", lookupResultsMsg);
            }

            TestForwardTransitionEnablement();
        }

        // forms all app options into one string so we can inspect simply to determine if anything changed
        string ConstructConfigValues()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("{0};", _pageUI.TargetRuntime); 
            sb.AppendFormat("{0};", _pageUI.Enable32BitApplications);
            sb.AppendFormat("{0};", _pageUI.HealthCheckURL);

            sb.AppendFormat("{0};", _pageUI.GetAppParam(CommonAppOptionsPage.APPPARAM1_KEY));
            sb.AppendFormat("{0};", _pageUI.GetAppParam(CommonAppOptionsPage.APPPARAM2_KEY));
            sb.AppendFormat("{0};", _pageUI.GetAppParam(CommonAppOptionsPage.APPPARAM3_KEY));
            sb.AppendFormat("{0};", _pageUI.GetAppParam(CommonAppOptionsPage.APPPARAM4_KEY));
            sb.AppendFormat("{0};", _pageUI.GetAppParam(CommonAppOptionsPage.APPPARAM5_KEY));

            switch (_pageUI.AppCredentialsMode)
            {
                case CommonAppOptionsPage.AppCredentials.UseNone:
                    sb.Append(";;");
                    break;

                case CommonAppOptionsPage.AppCredentials.ReuseLast:
                    {
                        if (!string.IsNullOrEmpty(_lastDeployedAccessKey))
                            sb.AppendFormat("{0};{1};", _lastDeployedAccessKey, _lastDeployedSecretKey);
                        else
                            sb.Append(";;");
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.UseSpecified:
                    sb.AppendFormat("{0};{1};", _pageUI.AccessKey, _pageUI.SecretKey);
                    break;

                case CommonAppOptionsPage.AppCredentials.UseDeploymentAccount:
                    {
                        AccountViewModel selectedAccount 
                            = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                        var keys = selectedAccount.Credentials.GetCredentials();
                        sb.AppendFormat("{0};{1};", keys.AccessKey, keys.SecretKey);
                    }
                    break;

                case CommonAppOptionsPage.AppCredentials.UseIAM:
                    sb.AppendFormat("{0};{1};", _pageUI.SelectedIAMUserKey, _secretKeyCollection[_pageUI.SelectedIAMUserKey]);
                    break;
            }

            sb.AppendFormat("{0};", _pageUI.NotificationEmail);

            return sb.ToString();
        }
    }
}
