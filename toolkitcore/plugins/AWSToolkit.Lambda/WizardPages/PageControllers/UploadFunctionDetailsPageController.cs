﻿using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using log4net;
using System.Windows.Controls;

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

            HostingWizard[UploadFunctionWizardProperties.FunctionName] = _pageUI.FunctionName;
            HostingWizard[UploadFunctionWizardProperties.Description] = _pageUI.Description;
            HostingWizard[UploadFunctionWizardProperties.Configuration] = _pageUI.Configuration;
            HostingWizard[UploadFunctionWizardProperties.Runtime] = _pageUI.RuntimeValue;

            // Other languages (like nodejs) do not have a valid framework value
            HostingWizard[UploadFunctionWizardProperties.Framework] =
                _pageUI.Runtime.IsNetCore ? _pageUI.Framework : string.Empty;

            HostingWizard[UploadFunctionWizardProperties.Handler] = _pageUI.FormattedHandler;
            HostingWizard[UploadFunctionWizardProperties.SourcePath] = _pageUI.SourcePath;
            HostingWizard[UploadFunctionWizardProperties.SaveSettings] = _pageUI.SaveSettings;

            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
   }
}
