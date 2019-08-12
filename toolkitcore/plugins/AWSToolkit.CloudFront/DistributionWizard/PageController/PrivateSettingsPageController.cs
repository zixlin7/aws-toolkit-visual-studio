using System.Windows.Controls;

using Amazon.AWSToolkit.CloudFront.Model;
using Amazon.AWSToolkit.CloudFront.Controller;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CloudFront.DistributionWizard.PageUI;

namespace Amazon.AWSToolkit.CloudFront.DistributionWizard.PageController
{
    public class PrivateSettingsPageController : IAWSWizardPageController
    {
        BaseDistributionConfigEditorController _controller;
        PrivateSettingsPage _pageUI;

        public PrivateSettingsPageController(BaseDistributionConfigEditorController controller)
        {
            this._controller = controller;
        }


        #region IAWSWizardPageController Members

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Private Distribution Settings";

        public string ShortPageTitle => null;

        public string PageDescription => "Additional settings for setting up a private distribution.";

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return this.IsValidToSet;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PrivateSettingsPage(this);
                this._controller.BaseModel.PropertyChanged += this.onPropertyChanged;
                this._pageUI.DataContext = this._controller.BaseModel;
            }
            HostingWizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);
            this._pageUI.IsEnabled = this.IsValidToSet;

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
            return true;
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

        bool IsValidToSet
        {
            get
            {
                DistributionConfigModel model = this._controller.BaseModel as DistributionConfigModel;
                if (model == null || model.S3OriginSelected)
                    return true;

                return false;
            }
        }

        public BaseDistributionConfigEditorController EditorController => this._controller;

        void onPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        void StorePageData()
        {
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if(!this._controller.BaseModel.IsPrivateDistributionEnabled)
                    return true;

                return this._controller.BaseModel.SelectedOriginAccessIdentityWrapper != null;
            }
        }
    }
}
