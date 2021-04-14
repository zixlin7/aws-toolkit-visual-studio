using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ECS.WizardPages.PageUI;
using System.Windows.Controls;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.ECS.WizardPages.PageControllers
{
    public class PushImageToECRPageController : IAWSWizardPageController
    {
        private PushImageToECRPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "Select the Amazon ECR Repository to push the Docker image to.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Publish Container to AWS";

        public string ShortPageTitle => null;

        public bool AllowShortCircuit()
        {
            return true;
        }

        public void ResetPage()
        {

        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PushImageToECRPage(this, ToolkitFactory.Instance.ToolkitContext);
                _pageUI.PropertyChanged += _pageUI_PropertyChanged;
                _pageUI.Connection.PropertyChanged += _pageUI_PropertyChanged;
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
            HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode] = _pageUI.DeploymentOption.Mode;

            // don't stand in the way of our previous sibling pages!
            return IsForwardsNavigationAllowed;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            if(this._pageUI.DeploymentOption?.Mode == Constants.DeployMode.PushOnly)
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            else
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

        AccountViewModel _lastSavedAccount;
        ToolkitRegion _lastSavedRegion;

        bool StorePageData()
        {
            if (_pageUI == null)
                return false;

            bool resetForwardPages = false;
            if (_lastSavedAccount != _pageUI.SelectedAccount || _lastSavedRegion != _pageUI.SelectedRegion)
                resetForwardPages = true;

            _lastSavedAccount = _pageUI.SelectedAccount;
            _lastSavedRegion = _pageUI.SelectedRegion;


            HostingWizard.SetSelectedAccount(_pageUI.SelectedAccount, PublishContainerToAWSWizardProperties.UserAccount);
            HostingWizard.SetSelectedRegion(_pageUI.SelectedRegion, PublishContainerToAWSWizardProperties.Region);

            HostingWizard[PublishContainerToAWSWizardProperties.Configuration] = _pageUI.Configuration;
            HostingWizard[PublishContainerToAWSWizardProperties.DockerRepository] = _pageUI.DockerRepository.ToLower();
            HostingWizard[PublishContainerToAWSWizardProperties.DockerTag] = _pageUI.DockerTag.ToLower();

            HostingWizard[PublishContainerToAWSWizardProperties.DockerBuildWorkingDirectory] = _pageUI.DockerBuildWorkingDirectory?.ToLower();

            HostingWizard[PublishContainerToAWSWizardProperties.DeploymentMode] = _pageUI.DeploymentOption.Mode;

            HostingWizard[PublishContainerToAWSWizardProperties.PersistSettingsToConfigFile] = _pageUI.PersistSettingsToConfigFile;
            HostingWizard[PublishContainerToAWSWizardProperties.IsFargateSupported] = true;
            if (resetForwardPages)
            {
                this.HostingWizard.NotifyForwardPagesReset(this);
            }

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
