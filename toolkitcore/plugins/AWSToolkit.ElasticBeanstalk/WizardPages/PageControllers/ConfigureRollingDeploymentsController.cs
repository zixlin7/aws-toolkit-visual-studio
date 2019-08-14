using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using log4net;
using ConfigureRollingDeploymentsPage = Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment.ConfigureRollingDeploymentsPage;



namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers
{
    internal class ConfigureRollingDeploymentsController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ConfigureRollingDeploymentsController));

        readonly object _syncLock = new object();
        private ConfigureRollingDeploymentsPage _pageUI;

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => DeploymentWizardPageGroups.AWSOptionsGroup;

        public string PageTitle => "Rolling Deployments";

        public string ShortPageTitle => "Updates";

        public string PageDescription => "Configure rolling deployments for application and environment configuration changes to avoid downtime during redeployments.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
            {
                var isRedeploying = (bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy];
                if (isRedeploying)
                    return false;
            }

            if (HostingWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnableRollingDeployments))
            {
                return (bool)HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnableRollingDeployments];
            }

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ConfigureRollingDeploymentsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
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
            var enableNext = IsForwardsNavigationAllowed;

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, enableNext);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, enableNext);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return true;

                return (!_pageUI.EnableConfigRollingDeployment || (_pageUI.MaximumBatchSize != null && _pageUI.MinInstanceInService != null)) &&
                        (_pageUI.AppBatchType != null && _pageUI.AppBatchSize != null);

            }
        }

        void StorePageData()
        {
            // if user short-circuiting wizard, our ui may not have been shown
            if (_pageUI != null)
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_EnableConfigRollingDeployment] = this._pageUI.EnableConfigRollingDeployment;

                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_ConfigMaximumBatchSize] = this._pageUI.MaximumBatchSize;
                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_ConfigMinInstanceInServices] = this._pageUI.MinInstanceInService;


                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchType] = this._pageUI.AppBatchType;
                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchSize] = this._pageUI.AppBatchSize;
            }
            else
            {
                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_EnableConfigRollingDeployment] = false;

                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchType] = "Percentage";
                HostingWizard[BeanstalkDeploymentWizardProperties.RollingDeployments.propKey_AppBatchSize] = 100;
            }
        }

        void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

    }
}
