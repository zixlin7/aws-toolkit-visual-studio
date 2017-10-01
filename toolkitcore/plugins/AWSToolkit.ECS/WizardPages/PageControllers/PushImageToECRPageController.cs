using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class PushImageToECRPageController : IAWSWizardPageController
    {
        private PushImageToECRPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get
            {
                return "Select the Amazon ECR Repository to push the Docker image to.";
            }
        }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get
            {
                return "Publish Container to AWS";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PushImageToECRPage(this);
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
            }

            return _pageUI;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                return StorePageData();

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            // don't stand in the way of our previous sibling pages!
            return IsForwardsNavigationAllowed;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                return _pageUI.AllRequiredFieldsAreSet;
            }
        }

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

            HostingWizard[PublishContainerToAWSWizardProperties.UserAccount] = _pageUI.SelectedAccount;
            HostingWizard[PublishContainerToAWSWizardProperties.Region] = _pageUI.SelectedRegion;

            HostingWizard[PublishContainerToAWSWizardProperties.Configuration] = _pageUI.Configuration;
            HostingWizard[PublishContainerToAWSWizardProperties.DockerImageTag] = _pageUI.DockerImageTag;

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
