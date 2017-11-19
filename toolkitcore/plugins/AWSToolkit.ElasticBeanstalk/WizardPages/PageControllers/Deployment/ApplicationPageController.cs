using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.Deployment
{
    internal class ApplicationPageController : IAWSWizardPageController
    {
        private ApplicationPage _pageUI;

        public string PageID
        {
            get { return GetType().FullName; }
        }
        
        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return DeploymentWizardPageGroups.AppTargetGroup; }
        }

        public string PageTitle
        {
            get { return "Application Environment"; }
        }

        public string ShortPageTitle
        {
            get { return "Environment"; }
        }

        public string PageDescription
        {
            get { return "Enter the details for your new application environment. To create a new environment for an existing application, select the appropriate application."; }
        }

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

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new ApplicationPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                _pageUI.SetAvailableApplicationDeployments(ReadExistingDeploymentsFromHostWizard());
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedName))
                {
                    _pageUI.ApplicationName =
                        HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedName] as string;

                    if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion))
                    {
                        var redeployingAppVersion = (bool)HostingWizard[DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion];
                        if (redeployingAppVersion)
                            _pageUI.ConfigureForAppVersionRedeployment();
                    }
                }
            }

            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                if (!this._pageUI.IsCNameValid)
                {
                    Amazon.AWSToolkit.ToolkitFactory.Instance.ShellProvider.ShowError("Invalid CNAME", "CNAME is invalid");
                    return false;
                }
                else if (!this._pageUI.CheckCNAMEAvailability())
                {
                    Amazon.AWSToolkit.ToolkitFactory.Instance.ShellProvider.ShowError("Invalid CNAME", string.Format("CNAME {0} is unavailable.", this._pageUI.CName));
                    return false;
                }

                StorePageData();
            }

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            return false;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                if (string.IsNullOrEmpty(_pageUI.ApplicationName) || !_pageUI.IsApplicationNameValid)
                    return false;

                if (string.IsNullOrEmpty(_pageUI.EnvironmentName) || !_pageUI.IsEnvironmentNameValid)
                    return false;

                if (string.IsNullOrEmpty(_pageUI.CName) || !_pageUI.IsCNameValid)
                    return false;

                return true;
            }
        }

        void StorePageData()
        {
            HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = _pageUI.ApplicationName;
            HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] = _pageUI.EnvironmentName;

            HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CreateNewEnv] = true;
            HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvDescription] = string.Empty;
            HostingWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName] = _pageUI.CName;

            // setup now-unused keys so we can share deployment engine with legacy wizard
            var seedVersionLabel = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;
            HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel, seedVersionLabel);
            HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment, false);
            HostingWizard.SetProperty(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation, string.Empty);
        }

        private void OnPagePropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        public ICollection<DeployedApplicationModel> ReadExistingDeploymentsFromHostWizard()
        {
            ICollection<DeployedApplicationModel> existingDeployments = null;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_ExistingAppDeploymentsInRegion))
            {
                existingDeployments = HostingWizard.GetProperty(DeploymentWizardProperties.DeploymentTemplate.propkey_ExistingAppDeploymentsInRegion)
                        as ICollection<DeployedApplicationModel>;
            }

            return existingDeployments;
        }
    }
}
