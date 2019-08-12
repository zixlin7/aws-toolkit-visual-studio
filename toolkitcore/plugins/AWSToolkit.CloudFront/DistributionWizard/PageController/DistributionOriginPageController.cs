using System.Windows.Controls;
using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController
{
    public class DistributionOriginPageController : IAWSWizardPageController
    {
        CreateDistributionController _controller;
        DistributionOriginPage _pageUI;
        bool _needsReviewStep = false;

        public DistributionOriginPageController(CreateDistributionController controller)
        {
            this._controller = controller;
        }

        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Origin";

        public string ShortPageTitle => null;

        public string PageDescription => "Origin of the CloudFront distribution.";

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
                _pageUI = new DistributionOriginPage(this);
                _pageUI.DataContext = this._controller.BaseModel;
                this._controller.BaseModel.PropertyChanged += this.onPropertyChanged;
                this._pageUI.Initialize(this._controller);
                HostingWizard.SetShortCircuitPage(string.Empty);
                this._needsReviewStep = false;
            }
            // If we are coming back to this page from visiting the other tabs then we should review before creating.
            else
            {
                HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Review");
                HostingWizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);
                this._needsReviewStep = true;
            }


            return _pageUI;
        }

        void onPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // can get here direct from quick launch page if a seed ami was set, so do the same
            // as the ami selector page and turn on the wizard buttons that quick launch turned off
            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, false);
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Next");

            if (!this._needsReviewStep)
            {
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, false);
                HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Create");
            }

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

        bool IsForwardsNavigationAllowed
        {
            get
            {
                bool enable = false; ;
                if (this._controller.Model.S3OriginSelected)
                {
                    enable = !string.IsNullOrEmpty(this._controller.Model.S3BucketOrigin);
                }
                else
                {
                    if (string.IsNullOrEmpty(this._controller.Model.CustomOriginDNSName))
                        enable = false;
                    else
                        enable = true;
                }

                if (enable)
                {
                    HostingWizard.RequestFinishEnablement(this);
                }
                else
                {
                    HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, false);
                }

                return enable;
            }
        }
    }
}
