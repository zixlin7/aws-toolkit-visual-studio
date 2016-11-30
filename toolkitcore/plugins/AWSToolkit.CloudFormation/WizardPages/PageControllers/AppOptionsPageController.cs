using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageWorkers;

using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    /// <summary>
    /// Implements the retrieval and population of application settings for an app
    /// deployed too or about to be deployed to CloudFormation
    /// </summary>
    public class AppOptionsPageController : CommonAppOptionsPageControllerBase
    {
        protected override bool OnQueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return IsWizardInCloudFormationMode;
        }

        protected override void OnPageActivated(AWSWizardConstants.NavigationReason navigationReason, bool isRedeploying)
        {
            _pageUI.EmailControlsVisible = false;
        }

        bool IsWizardInCloudFormationMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.CloudFormationServiceName;
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

                _pageUI.DataLoadPending = true;
                new FetchStackConfigWorker(HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel,
                                           HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints,
                                           deployment.Tag as Stack,
                                           LOGGER,
                                           new FetchStackConfigWorker.DataAvailableCallback(FetchConfigWorkerCompleted));
            }
            else
                _pageUI.SetAppParams(null, DefaultHealthCheckUrl, null, false);

            TestForwardTransitionEnablement();
        }

        void FetchConfigWorkerCompleted(JsonData configData)
        {
            // if the worker failed, we'll get a null configData object
            string lookupResultsMsg = null;
            Dictionary<string, string> appParams = new Dictionary<string, string>();
            string healthCheckUrl = DefaultHealthCheckUrl;
            try
            {
                // this is just to get us a slightly better error handling below, instead
                // of reporting NullReferenceException
                if (configData != null)
                {
                    // these don't get sent to the page but held back and used in StorePageData
                    // if user select 'use last keys' on page
                    _lastDeployedAccessKey = string.Empty;
                    _lastDeployedSecretKey = string.Empty;

                    JsonData env = (configData["Application"])["Environment Properties"];
                    if (env != null)
                    {
                        foreach (string paramKey in AppParamKeys)
                        {
                            if (env[paramKey] != null)
                                appParams.Add(paramKey, (string)env[paramKey]);
                        }

                        if (env["AWSAccessKey"] != null)
                        {
                            _lastDeployedAccessKey = (string)env["AWSAccessKey"];
                            _lastDeployedSecretKey = (string)env["AWSSecretKey"];
                        }
                    }

                    JsonData app = (configData["AWSDeployment"])["Application"];
                    if (app != null)
                    {
                        if (app["Application Healthcheck URL"] != null)
                            healthCheckUrl = (string)app["Application Healthcheck URL"];
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
                _pageUI.SetAppParams(appParams, healthCheckUrl, null, true);
                _pageUI.DataLoadPending = false;
            }

            if (!string.IsNullOrEmpty(lookupResultsMsg))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Pre-deployment Inspection Failed", lookupResultsMsg);
            }

            TestForwardTransitionEnablement();
        }
    }
}
