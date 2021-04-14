using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Controls;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Regions;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class FastTrackRepublishPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(FastTrackRepublishPageController));

        private readonly ToolkitContext _toolkitContext;

        FastTrackRepublishPage _pageUI = null;
        AccountViewModel _account;
        ToolkitRegion _region;
        readonly object _syncLock = new object();

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
                _pageUI.PropertyChanged += OnPropertyChanged;

                // one-time translate the seed data to be a complete record of what a republish pass through
                // the full wizard would have set up - first gather the seed data
                var viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;
                var accountGuid = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid] as string;
                _account = viewModel.AccountFromIdentityKey(accountGuid);
                
                var regionName = HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo] as string;
                _region = _toolkitContext.RegionProvider.GetRegion(regionName);

                var projectType = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectType] as string;
                if(projectType == DeploymentWizardProperties.NetCoreWebProject)
                {
                    _pageUI.CoreCLRVisible = System.Windows.Visibility.Visible;

                    var targetRuntime = HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] as string;
                    var availableFrameworks = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectFrameworks] as Dictionary<string, string>;
                    _pageUI.SetDefaultRuntimesOrFrameworks(targetRuntime, availableFrameworks);
                }
                else
                {
                    _pageUI.CoreCLRVisible = System.Windows.Visibility.Collapsed;
                }

                var bdh = DeploymentWizardHelper.DeploymentHistoryForAccountAndRegion(_account, _region.Id, HostingWizard.CollectedProperties);

                // work around any loss of default location due to user switching between incremental/non-incremental (shouldn't
                // happen, but...). Note that we always set a default; don't care if it's used or not :-)
                var incrementalDeploymentLocn = bdh.IncrementalPushRepositoryLocation;
                if (string.IsNullOrEmpty(incrementalDeploymentLocn))
                {
                    var vsProjectGuid = HostingWizard[DeploymentWizardProperties.SeedData.propkey_VSProjectGuid] as string;
                    incrementalDeploymentLocn = DeploymentWizardHelper.ComputeDeploymentArtifactFolder(vsProjectGuid);
                }

                _pageUI.IISAppPath = AWSDeployment.CommonParameters.DefaultIisAppPathFormat;

                // now pass the whole seed data into the final properties the deployment engine will look at (except version,
                // which the user can change in the dialog)
                HostingWizard.SetSelectedAccount(_account);
                HostingWizard.SetSelectedRegion(_region);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, true);
                
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName, bdh.ApplicationName);
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName, bdh.EnvironmentName);
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner, DeploymentServiceIdentifiers.BeanstalkServiceName);
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment, bdh.IsIncrementalDeployment);
                HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation, incrementalDeploymentLocn);

                var buildConfigurations = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ProjectBuildConfigurations] as IDictionary<string, string>;
                var activeBuildConfiguration = HostingWizard[DeploymentWizardProperties.SeedData.propkey_ActiveBuildConfiguration] as string;

                _pageUI.BuildConfigurations = new ObservableCollection<string>(buildConfigurations.Keys);
                _pageUI.SelectedBuildConfiguration = DeploymentWizardHelper.SelectDeploymentBuildConfiguration(buildConfigurations.Keys, bdh.BuildConfiguration, activeBuildConfiguration);

                // this is just to satisfy code that runs this wizard and the full one
                var deployment = new ExistingServiceDeployment
                {
                    DeploymentName = bdh.ApplicationName,
                    DeploymentService = DeploymentServiceIdentifiers.BeanstalkServiceName
                };
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, deployment);

                // fetch the environment details so we can display some useful context
                // need to fetch the Stack instance so we can post it into the output properties
                var bw = new BackgroundWorker();
                bw.DoWork += FetchEnvironmentWorker;
                bw.RunWorkerCompleted += FetchEnvironmentWorkerCompleted;
                bw.RunWorkerAsync(new object[] 
                {
                    _account,
                    _region,
                    bdh,
                    LOGGER
                });

                _pageUI.SetDeploymentVersionLabelInfo(HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string,
                                                      bdh.IsIncrementalDeployment);
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

        void StorePageData()
        {
            string targetFramework = _pageUI.TargetFramework;
            if (!string.IsNullOrEmpty(targetFramework))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = targetFramework;
            else
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = null;

            HostingWizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] = _pageUI.SelectedBuildConfiguration;
            HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel, _pageUI.DeploymentVersionLabel);
            if (string.IsNullOrEmpty(_pageUI.IISAppPath))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = AWSDeployment.CommonParameters.DefaultIisAppPathFormat;
            else
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] = _pageUI.IISAppPath;

            if (string.IsNullOrEmpty(_pageUI.TargetFramework))
                HostingWizard[DeploymentWizardProperties.AppOptions.propkey_TargetRuntime] = _pageUI.TargetFramework;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                lock (_syncLock)
                {
                    // must have retrieved last-environment details as well as validated version 
                    // before we can proceed
                    var nextEnabled = HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance);

                    if (nextEnabled)
                    {
                        nextEnabled = !_pageUI.VersionFetchPending && _pageUI.IsSelectedVersionValid;
                    }
                
                    return nextEnabled;
                }
            }
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // only one property can be changed, version label
            TestForwardTransitionEnablement();
        }

        void FetchEnvironmentWorker(object sender, DoWorkEventArgs e)
        {
            var args = e.Argument as object[];
            var account = args[0] as AccountViewModel;
            var region = args[1] as ToolkitRegion;
            var bdh = args[2] as BeanstalkDeploymentHistory;
            var logger = args[3] as ILog;

            EnvironmentDescription envDescription = null;
            try
            {
                var beanstalkClient = account.CreateServiceClient<AmazonElasticBeanstalkClient>(region);

                var response = beanstalkClient.DescribeEnvironments(new DescribeEnvironmentsRequest
                {
                    ApplicationName = bdh.ApplicationName, 
                    EnvironmentNames = new List<string> { bdh.EnvironmentName }
                });
                envDescription = response.Environments[0];
            }
            catch (Exception exc)
            {

                logger.Error(GetType().FullName + ", exception in FetchEnvironmentWorker", exc);
            }

            e.Result = envDescription;
        }

        void FetchEnvironmentWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            var envDescription = e.Result as EnvironmentDescription;
            if (envDescription != null)
            {
                var deployment = new ExistingServiceDeployment();
                deployment.DeploymentName = envDescription.ApplicationName;
                deployment.DeploymentService = DeploymentServiceIdentifiers.BeanstalkServiceName;
                deployment.Tag = envDescription; // not required but may be useful one day
                HostingWizard.SetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_RedeploymentInstance, deployment);

                _pageUI.SetRedeploymentMessaging(_account, _region, envDescription);
            }

            TestForwardTransitionEnablement();
        }
    }
}
