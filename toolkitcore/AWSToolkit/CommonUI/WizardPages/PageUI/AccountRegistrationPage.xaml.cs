using System;
using Amazon.AWSToolkit.Account.Controller;
using Amazon.AWSToolkit.Account.View;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

namespace Amazon.AWSToolkit.CommonUI.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for AccountRegistrationPage.xaml
    /// </summary>
    public partial class AccountRegistrationPage
    {
        RegisterAccountController _registerAccountController = null;

        public AccountRegistrationPage()
        {
            InitializeComponent();
        }

        public IAWSWizardPageController PageController { get; set; }
        
        public void PeristAccount()
        {
            _registerAccountController.Persist();
        }

        private void Grid_Initialized(object sender, EventArgs e)
        {
            if (_registerAccountController == null)
            {
                _registerAccountController = new RegisterAccountController();
                RegisterAccountControl control = new RegisterAccountControl(_registerAccountController);
                control.SetMandatoryFieldsReadyCallback(MandatoryFieldsReadinessChange);
                this._grid.Children.Add(control);
            }

        }

        private void MandatoryFieldsReadinessChange(bool allCompleted)
        {
            // bit of a kludge to work around the fact we can't see into the control, 
            // therefore calling TestForwardTransitionEnablement on the page controller
            // gets us no-where
            PageController.HostingWizard.SetNavigationEnablement(PageController, AWSWizardConstants.NavigationButtons.Forward, allCompleted);
        }
    }
}
