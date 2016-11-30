using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.WizardPages.PageUI;
using Amazon.AWSToolkit.Persistence;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.CommonUI.WizardPages.PageControllers
{
    public class AccountSelectorPageController : IAWSWizardPageController
    {
        AccountSelectorPage _pageUI = null;
        bool initToLastUsedAccount = true;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public bool GetPageDescriptorInfo(out string pageTitle, out string pageDescription)
        {
            // todo: l18n
            pageTitle = "Account";
            pageDescription = "Select the AWS account to use during application deployment.";
            return true;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public System.Windows.Controls.UserControl PageActivating()
        {
            if (_pageUI == null)
            {
                _pageUI = new AccountSelectorPage(this);

                AWSViewModel viewModel = HostingWizard[CommonWizardProperties.propkey_RootViewModel] as AWSViewModel;
                _pageUI.RootViewModel = viewModel;
            }

            return _pageUI;
        }

        public void PageActivated()
        {
            // only want to do this on first invocation, not after user has picked a/c,
            // gone forward and then come back to us
            if (initToLastUsedAccount)
            {
                AWSViewModel viewModel = HostingWizard[CommonWizardProperties.propkey_RootViewModel] as AWSViewModel;
                var lastAccountId = PersistenceManager.Instance.GetSetting(NavigatorControl.LastAcountSelectedKey);
                AccountViewModel accountViewModel = null;
                if (!string.IsNullOrEmpty(lastAccountId))
                {
                    foreach (var account in viewModel.RegisteredAccounts)
                    {
                        if (account.SettingsUniqueKey == lastAccountId)
                        {
                            accountViewModel = account;
                            break;
                        }
                    }
                }

                if (accountViewModel == null && viewModel.RegisteredAccounts.Count > 0)
                {
                    accountViewModel = viewModel.RegisteredAccounts[0];
                }

                _pageUI.SelectedAccount = accountViewModel;

                initToLastUsedAccount = false;
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
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            bool nextEnabled = true;
            if (_pageUI.IsRegisterNewAccountChecked)
            {
                // these fields are mandatory for a new registration
                nextEnabled = !string.IsNullOrEmpty(this._pageUI.DisplayName)
                                   && !string.IsNullOrEmpty(this._pageUI.AccessKey)
                                   && !string.IsNullOrEmpty(this._pageUI.SecretKey);
            }

            HostingWizard.SetNavigationEnablement(AWSWizardConstants.NavigationButtons.Forward, nextEnabled);
        }

        public bool AllowShortCircuit()
        {
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
            //if (_pageUI != null)
            //{
            //    bool registerNew = _pageUI.IsRegisterNewAccountChecked;
            //    HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_RegisterNewAccount] = registerNew;
            //    if (!registerNew)
            //    {
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_SelectedAccount] = _pageUI.SelectedAccount;
            //        // useful to have
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_DisplayName] = _pageUI.SelectedAccount.AccountDisplayName;
            //    }
            //    else
            //    {
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_SelectedAccount] = null;
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_DisplayName] = this._pageUI.DisplayName;
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_AccessKey] = this._pageUI.AccessKey;
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_SecretKey] = this._pageUI.SecretKey;
            //        HostingWizard[CommonWizardProperties.AccountSelectorPage.propkey_AccountNumber] = this._pageUI.AccountNumber;
            //    }
            //}
        }
    }
}
