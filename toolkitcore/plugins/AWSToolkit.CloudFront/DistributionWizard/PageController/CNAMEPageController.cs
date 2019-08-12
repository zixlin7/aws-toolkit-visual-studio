using System.Windows.Controls;
using Amazon.AWSToolkit.CloudFront.Controller;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController
{
    public class CNAMEPageController : IAWSWizardPageController
    {
        BaseDistributionConfigEditorController _controller;
        CNAMEPage _pageUI;

        public CNAMEPageController(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
        }


        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "CNAMEs";

        public string ShortPageTitle => null;

        public string PageDescription => "Set the CNAMEs associated with the distribution.";

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
                _pageUI = new CNAMEPage(this);
                _pageUI.DataContext = this._controller.BaseModel;
            }
            HostingWizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // can get here direct from quick launch page if a seed ami was set, so do the same
            // as the ami selector page and turn on the wizard buttons that quick launch turned off
            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, true);
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Next");
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, false);
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Review");
            HostingWizard.RequestFinishEnablement(this);

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
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
        }

        bool IsForwardsNavigationAllowed => true;
    }
}
