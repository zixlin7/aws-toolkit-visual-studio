using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.WizardPages.PageUI;

using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CommonUI.WizardPages.PageControllers
{
    /// <summary>
    /// 'Landing' page for all wizards, used in scenarios where the toolkit
    /// has no prior account registration data.
    /// </summary>
    public class AccountRegistrationPageController : IAWSWizardPageController
    {
        AccountRegistrationPage _pageUI;

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Profile Registration";

        public string ShortPageTitle => null;

        public string PageDescription => "Please create a profile to register an AWS account with the toolkit before proceeding. You can publish using this account profile or register and use more accounts on the next page.";

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            var viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;
            return viewModel.RegisteredAccounts.Count == 0;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return _pageUI ?? (_pageUI = new AccountRegistrationPage {PageController = this});
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                _pageUI.PeristAccount();

                var viewModel = HostingWizard[CommonWizardProperties.propkey_NavigatorRootViewModel] as AWSViewModel;
                viewModel.Refresh();

                HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = viewModel.RegisteredAccounts[0];
            }

            HostingWizard.ResetFirstActivePage(); // user won't see this page again
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            // we don't enable finish on this page, so no direct involvement
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            // by definition we can't short the process, as the
            // process can't start until we have some accounts...
            return false;
        }

        public void ResetPage()
        {

        }

        // todo: come back and fix up when we have fields
        bool IsForwardsNavigationAllowed => true;
    }
}
