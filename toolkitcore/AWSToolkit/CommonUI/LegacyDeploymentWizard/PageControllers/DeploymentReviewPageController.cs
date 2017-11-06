using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers
{
    public class DeploymentReviewPageController : IAWSWizardPageController
    {
        DeploymentReviewPage _pageUI = null;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return DeploymentWizardPageGroups.ReviewGroup; }
        }

        public string PageTitle
        {
            get { return "Review"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Review the information below, then click Finish to start deployment."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new DeploymentReviewPage();

#if VS2017
                if (this.HostingWizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) is string)
                {
                    var isNETCoreProjectType = (this.HostingWizard.GetProperty(DeploymentWizardProperties.SeedData.propkey_ProjectType) as string)
                                            .Equals(DeploymentWizardProperties.NetCoreWebProject, StringComparison.OrdinalIgnoreCase);

                    _pageUI.IsNETCoreProjectType = isNETCoreProjectType;
                }
#endif
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            this._pageUI.ClearPanels();

            AddAccountAndServiceReviewPanel();

            IEnumerable<ServiceReviewPanelInfo> servicePanels = null;
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels))
                servicePanels = HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_ServiceReviewPanels] as IEnumerable<ServiceReviewPanelInfo>;
            if (servicePanels != null)
            {
                foreach (ServiceReviewPanelInfo panel in servicePanels)
                {
                    _pageUI.AddReviewPanel(panel.ReviewPanelHeader, panel.ReviewPanel);
                }
            }

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Deploy");
            HostingWizard.RequestFinishEnablement(this);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.finishPressed)
            {
                if (_pageUI != null)
                {
                    HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose] = _pageUI.OpenStatusOnClose;
                    HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_ConfigFileDestination] = _pageUI.ConfigFileDestination;
                    HostingWizard[DeploymentWizardProperties.ReviewProperties.propkey_SaveBeanstalkTools] = _pageUI.SaveBeanstalkTools;
                }
            }
            else
                if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                    HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Finish");

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        void AddAccountAndServiceReviewPanel()
        {
            TextBlock tb = new TextBlock();
            tb.TextWrapping = System.Windows.TextWrapping.Wrap;

            StringBuilder sb = new StringBuilder();

            AccountViewModel selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            RegionEndPointsManager.RegionEndPoints rep
                = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;

            sb.AppendFormat("Publish to {0} in region '{2}' ({3}) using account credentials from profile '{1}'.",
                                service == DeploymentServiceIdentifiers.CloudFormationServiceName 
                                    ? "AWS CloudFormation" : "AWS Elastic Beanstalk",
                                selectedAccount.AccountDisplayName, 
                                rep.DisplayName, 
                                rep.SystemName);

            tb.Text = sb.ToString();

            _pageUI.AddReviewPanel("Profile", tb);
        }
    }

    /// <summary>
    /// Class carrying the header and panel text for some aspect of service deployment. Pseudo review pages
    /// for each deployment service post a list of these prior to arriving at the final review page.
    /// </summary>
    public class ServiceReviewPanelInfo
    {
        public string ReviewPanelHeader { get; set; }
        public FrameworkElement ReviewPanel { get; set; }
    }
}
