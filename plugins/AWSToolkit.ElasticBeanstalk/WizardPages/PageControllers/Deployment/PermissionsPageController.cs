using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers.Deployment
{
    internal class PermissionsPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PermissionsPageController));

        private PermissionsPage _pageUI;
        private string _lastSeenAccount = string.Empty;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription
        {
            get { return "Select roles granting permissions to your deployed application and for the service to monitor resources."; }
        }

        public string PageGroup
        {
            get { return DeploymentWizardPageGroups.PermissionsGroup; }
        }

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public string PageTitle
        {
            get { return "Permissions"; }
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
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
            {
                var selectedAccount = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                if (!string.Equals(selectedAccount.AccountDisplayName, _lastSeenAccount, StringComparison.CurrentCulture))
                {
                    _lastSeenAccount = selectedAccount.AccountDisplayName;

                    // todo: initialize role dropdowns
                }
            }
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new PermissionsPage(this);
                _pageUI.PropertyChanged += OnPagePropertyChanged;
            }

            return _pageUI;
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

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            // all of this page's data is optional
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, true);
        }

        private void StorePageData()
        {

        }

        private void OnPagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }
    }
}
