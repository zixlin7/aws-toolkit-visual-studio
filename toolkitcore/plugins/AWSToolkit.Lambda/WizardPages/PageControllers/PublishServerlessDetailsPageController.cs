using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    public class PublishServerlessDetailsPageController : IAWSWizardPageController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(PublishServerlessDetailsPageController));

        private PublishServerlessDetailsPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get { return "Enter the details about the AWS Serverless application."; }
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
                return "Publish AWS Serverless Application";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PublishServerlessDetailsPage(this);
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

        public bool QueryTemplateParametersPageButtonEnablement()
        {
            var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;
            return wrapper != null && wrapper.ContainsUserVisibleParameters;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            // if we have no parameters, disable Next to try and indicate this is the last page, 
            // and the Upload page will start processing
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, QueryTemplateParametersPageButtonEnablement());
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

            var previousSetStackName = HostingWizard[UploadFunctionWizardProperties.StackName] as string;

            if(!this._pageUI.IsNewStack && !string.IsNullOrEmpty(previousSetStackName) && !string.Equals(previousSetStackName, _pageUI.StackName, StringComparison.Ordinal))
                HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters] = null;

            HostingWizard[UploadFunctionWizardProperties.StackName] = _pageUI.StackName;
            HostingWizard[UploadFunctionWizardProperties.IsNewStack] = _pageUI.IsNewStack;
            HostingWizard[UploadFunctionWizardProperties.Configuration] = _pageUI.Configuration;
            HostingWizard[UploadFunctionWizardProperties.Framework] = _pageUI.Framework;
            HostingWizard[UploadFunctionWizardProperties.S3Bucket] = _pageUI.S3Bucket;
            HostingWizard[UploadFunctionWizardProperties.SaveSettings] = _pageUI.SaveSettings;


            return true;
        }

        private void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }
    }
}
