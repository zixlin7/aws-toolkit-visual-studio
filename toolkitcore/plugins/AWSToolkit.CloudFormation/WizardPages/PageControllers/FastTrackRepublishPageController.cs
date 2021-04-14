using System;
using System.Collections.Generic;
using System.Windows.Controls;
using System.ComponentModel;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.Navigator.Node;
using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageWorkers;
using ThirdParty.Json.LitJson;
using System.Threading;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class FastTrackRepublishPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(FastTrackRepublishPageController));

        private readonly ToolkitContext _toolkitContext;

        FastTrackRepublishPage _pageUI;
        AccountViewModel _account;
        ToolkitRegion _region;

        // recovered details from the last deployment; must re-specify or we lose them
        string _lastDeployedAccessKey;
        string _lastDeployedSecretKey;
        Dictionary<string, string> appParams = new Dictionary<string, string>();
        string healthCheckUrl = CommonAppOptionsPageControllerBase.DefaultHealthCheckUrl;

        // keep in lockstep with CommonAppOptionsPageControllerBase.AppParamKeys
        string[] _paramKeys = new[]
            {
                CommonAppOptionsPage.APPPARAM1_KEY,
                CommonAppOptionsPage.APPPARAM2_KEY,
                CommonAppOptionsPage.APPPARAM3_KEY,
                CommonAppOptionsPage.APPPARAM4_KEY,
                CommonAppOptionsPage.APPPARAM4_KEY,
            };


        object _syncLock = new object();
        int _pendingWorkers = 0;
        protected bool HasPendingWorkers
        {
            get
            {
                bool ret;
                lock (_syncLock)
                    ret = _pendingWorkers != 0;
                return ret;
            }
        }

        protected void RegisterPendingWorker()
        {
            Interlocked.Increment(ref _pendingWorkers);
        }

        protected void ClearPendingWorker()
        {
            Interlocked.Decrement(ref _pendingWorkers);
        }

        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Republish";

        public string ShortPageTitle => null;

        public string PageDescription => "Verify the details of the last deployment.";

        public FastTrackRepublishPageController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new FastTrackRepublishPage(this);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(onPropertyChanged);

                // one-time translate the seed data to be a complete record of what a republish pass through
                // the full wizard would have set up - first gather the seed data
                AWSViewModel viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;
                string accountGuid = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid] as string;
                _account = viewModel.AccountFromIdentityKey(accountGuid);

                string regionName = HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo] as string;
                _region = _toolkitContext.RegionProvider.GetRegion(regionName);

                CloudFormationDeploymentHistory cfdh = DeploymentHistoryForAccountAndRegion(_account, _region);

                string seedVersion = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;

                // now pass the whole seed data into the final properties the deployment engine will look at (except version,
                // which the user can change in the dialog)
                HostingWizard.SetSelectedAccount(_account);
                HostingWizard.SetSelectedRegion(_region);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, true);

                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName, cfdh.LastStack);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner, DeploymentServiceIdentifiers.CloudFormationServiceName);

                // need to fetch the Stack instance so we can post it into the output properties
                RegisterPendingWorker();
                BackgroundWorker bw = new BackgroundWorker();
                bw.DoWork += new DoWorkEventHandler(FetchStackWorker);
                bw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(FetchStackWorkerCompleted);
                bw.RunWorkerAsync(new object[] 
                {
                    _account,
                    _region,
                    cfdh.LastStack,
                    LOGGER
                });
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // Leave this visible for now, looks odd to have Cancel floating way off left
            //HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, false);
            //HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Deploy");

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        #endregion

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (HasPendingWorkers)
                    return false;

                return HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance);
            }
        }

        void StorePageData()
        {
            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl] = healthCheckUrl;

            IDictionary<string, string> appSettings;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings))
                appSettings = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] as IDictionary<string, string>;
            else
            {
                appSettings = new Dictionary<string, string>();
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_EnvAppSettings] = appSettings;
            }

            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvAccessKey] = _lastDeployedAccessKey;
            appSettings[DeploymentWizardProperties.AppOptions.propkey_EnvSecretKey] = _lastDeployedSecretKey;

            for (int i = 0; i < CommonAppOptionsPageControllerBase.AppParamKeys.Length; i++)
            {
                if (appParams.ContainsKey(CommonAppOptionsPageControllerBase.AppParamKeys[i]))
                {
                    appSettings[CommonAppOptionsPageControllerBase.AppParamKeys[i]] = appParams[CommonAppOptionsPageControllerBase.AppParamKeys[i]];
                }
            }
        }

        /// <summary>
        /// Returns deployment record matching the selected account and region. One must exist by definition of this page being used.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        CloudFormationDeploymentHistory DeploymentHistoryForAccountAndRegion(AccountViewModel account, ToolkitRegion region)
        {
            Dictionary<string, object> allPreviousDeployments
                    = HostingWizard[DeploymentWizardProperties.SeedData.propkey_PreviousDeployments] as Dictionary<string, object>;

            DeploymentHistories<CloudFormationDeploymentHistory> cloudFormationDeployments
                = allPreviousDeployments[DeploymentServiceIdentifiers.CloudFormationServiceName] as DeploymentHistories<CloudFormationDeploymentHistory>;

            // deployments within service organised by accountid: <region, T>
            Dictionary<string, CloudFormationDeploymentHistory> accountDeployments = cloudFormationDeployments.DeploymentsForAccount(account.SettingsUniqueKey);
            return accountDeployments[region.Id];
        }

        private void onPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        void FetchStackWorker(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            var account = args[0] as AccountViewModel;
            var region = args[1] as ToolkitRegion;
            var stackName = args[2] as string;
            var logger = args[3] as ILog;

            Stack stack = null;
            try
            {
                var cfClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);

                var response = cfClient.DescribeStacks(new DescribeStacksRequest() { StackName = stackName });
                stack = response.Stacks[0];
            }
            catch (Exception exc)
            {

                logger.Error(GetType().FullName + ", exception in FetchStackWorker", exc);
            }

            e.Result = stack;
        }

        void FetchStackWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            string lookupResultsMsg = null;
            try
            {
                Stack cfStack = e.Result as Stack;

                if (cfStack != null)
                {
                    ExistingServiceDeployment deployment = new ExistingServiceDeployment();
                    deployment.DeploymentName = cfStack.StackName;
                    deployment.DeploymentService = DeploymentServiceIdentifiers.CloudFormationServiceName;
                    deployment.Tag = cfStack;
                    HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, deployment);

                    _pageUI.SetRedeploymentMessaging(_account, _region, cfStack);

                    // we also need to recover app options from the stack so they can be re-applied (without this
                    // they get 'overwritten' with blanks - not what you might expect to happen if 'nothing' is specified)
                    RegisterPendingWorker();
                    new FetchStackConfigWorker(HostingWizard.GetSelectedAccount(),
                                               HostingWizard.GetSelectedRegion(),
                                               cfStack,
                                               LOGGER,
                                               new FetchStackConfigWorker.DataAvailableCallback(FetchConfigWorkerCompleted));
                }
            }
            catch (Exception exc)
            {
                lookupResultsMsg = string.Format("Error whilst querying deployed stack - {0}", exc.Message);
                LOGGER.ErrorFormat(lookupResultsMsg);
            }
            finally
            {
                ClearPendingWorker();
            }

            if (!string.IsNullOrEmpty(lookupResultsMsg))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Pre-deployment Inspection Failed", lookupResultsMsg);
            }

            TestForwardTransitionEnablement();
        }

        void FetchConfigWorkerCompleted(JsonData configData)
        {
            // if the worker failed, we'll get a null configData object
            string lookupResultsMsg = null;
            try
            {
                // this is just to get us a slightly better error handling below, instead
                // of reporting NullReferenceException
                if (configData != null)
                {
                    JsonData env = (configData["Application"])["Environment Properties"];
                    if (env != null)
                    {
                        foreach (string paramKey in CommonAppOptionsPageControllerBase.AppParamKeys)
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
                ClearPendingWorker();
            }

            if (!string.IsNullOrEmpty(lookupResultsMsg))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Pre-deployment Inspection Failed", lookupResultsMsg);
            }

            TestForwardTransitionEnablement();
        }
    }
}
