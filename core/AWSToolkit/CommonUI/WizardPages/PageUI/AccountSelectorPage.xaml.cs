using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CommonUI.WizardPages.PageUI
{
    /// <summary>
    /// Interaction logic for AccountSelectorPage.xaml
    /// </summary>
    public partial class AccountSelectorPage : UserControl
    {
        public AccountSelectorPage()
        {
            InitializeComponent();
        }

        public AccountSelectorPage(IAWSWizardPageController controller)
            : this()
        {
            PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public AWSViewModel RootViewModel
        {
            set { this._accountSelector.DataContext = value; }
        }

        public bool IsRegisterNewAccountChecked
        {
            get { return _btnRegisterNewAccount.IsChecked == true; }
        }

        public AccountViewModel SelectedAccount
        {
            get { return this._accountSelector.SelectedItem as AccountViewModel; }
            set 
            { 
                this._accountSelector.SelectedItem = value;
                if (value == null && this._btnRegisterNewAccount != null)
                {
                    this._btnRegisterNewAccount.IsChecked = true;
                    this._btnUseExistingAccount.IsEnabled = false;
                }
            }
        }

        public string DisplayName
        {
            get { return this._ctlDisplayName.Text; }
        }

        public string AccessKey
        {
            get { return this._ctlAccessKey.Text; }
        }

        public string SecretKey
        {
            get { return this._ctlSecretKey.Text; }
        }

        public string AccountNumber
        {
            get { return this._ctlAccountNumber.Text; }
        }

        private void _btnRegisterNewAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (PageController != null)
                PageController.TestForwardTransitionEnablement();
        }

        private void _btnUseExistingAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!this.IsInitialized)
                return;

            if (PageController != null)
                PageController.TestForwardTransitionEnablement();
        }

        // shared handler for text fields that get changed; forward to validator if set so
        // Next button can be enabled appropriately for mandatory fields
        private void OnTextFieldChanged(object sender, RoutedEventArgs e)
        {
            if (PageController != null)
                PageController.TestForwardTransitionEnablement();
        }
    }
}
