using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using log4net;
using System.Windows.Controls;
using Amazon.Lambda;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    /// <summary>
    /// The first page of the Upload Function wizard, gathering runtime selection
    /// and account details (depending on launch source) and basic details of the
    /// function to upload.
    /// </summary>
    public class UploadFunctionDetailsPageController : IAWSWizardPageController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(UploadFunctionDetailsPageController));

        private UploadFunctionDetailsPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "Enter the details about the function you want to upload.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle
        {
            get
            {
                var originator = (Controller.UploadFunctionController.UploadOriginator)this.HostingWizard.CollectedProperties[UploadFunctionWizardProperties.UploadOriginator];

                switch (originator)
                {
                    case Controller.UploadFunctionController.UploadOriginator.FromAWSExplorer:
                        return "Create new Lambda Function";
                    case Controller.UploadFunctionController.UploadOriginator.FromFunctionView:
                        return "Update Lambda Function";
                    default:
                        return "Upload Lambda Function";
                }
            }
        }

        public string ShortPageTitle => null;

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
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
                _pageUI = new UploadFunctionDetailsPage(this);
                _pageUI.ViewModel.PropertyChanged += _pageUI_PropertyChanged;
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

            HostingWizard[UploadFunctionWizardProperties.UserAccount] = _pageUI.SelectedAccount;
            HostingWizard[UploadFunctionWizardProperties.Region] = _pageUI.SelectedRegion;

            var xrayEndpoint = _pageUI.SelectedRegion.GetEndpoint(RegionEndPointsManager.XRAY_ENDPOINT_LOOKUP);
            HostingWizard.SetProperty(UploadFunctionWizardProperties.XRayAvailable, xrayEndpoint != null);

            HostingWizard[UploadFunctionWizardProperties.FunctionName] = _pageUI.ViewModel.FunctionName;
            HostingWizard[UploadFunctionWizardProperties.Description] = _pageUI.ViewModel.Description;
            HostingWizard[UploadFunctionWizardProperties.PackageType] = _pageUI.ViewModel.PackageType;

            if (_pageUI.ViewModel.PackageType.Equals(PackageType.Zip))
            {
                HostingWizard[UploadFunctionWizardProperties.Configuration] = _pageUI.ViewModel.Configuration;
                HostingWizard[UploadFunctionWizardProperties.Runtime] = _pageUI.ViewModel.Runtime.Value;

                // Other languages (like nodejs) do not have a valid framework value
                HostingWizard[UploadFunctionWizardProperties.Framework] =
                    _pageUI.ViewModel.Runtime.IsNetCore ? _pageUI.ViewModel.Framework : string.Empty;

                HostingWizard[UploadFunctionWizardProperties.Handler] = _pageUI.ViewModel.Handler;
                HostingWizard[UploadFunctionWizardProperties.SourcePath] = _pageUI.ViewModel.SourceCodeLocation;
            }
            else
            {
                HostingWizard[UploadFunctionWizardProperties.Dockerfile] = _pageUI.ViewModel.Dockerfile;
                HostingWizard[UploadFunctionWizardProperties.ImageCommand] = _pageUI.ViewModel.ImageCommand;
                HostingWizard[UploadFunctionWizardProperties.ImageRepo] = _pageUI.ViewModel.ImageRepo;
                HostingWizard[UploadFunctionWizardProperties.ImageTag] = _pageUI.ViewModel.ImageTag;
            }
            HostingWizard[UploadFunctionWizardProperties.SaveSettings] = _pageUI.ViewModel.SaveSettings;
            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(_pageUI.ViewModel.FunctionName))
            {
                HostingWizard.SetProperty(UploadFunctionWizardProperties.IsSelectedFunctionExisting,
                    _pageUI.ViewModel.FunctionExists);
            }

            TestForwardTransitionEnablement();
        }
   }
}
