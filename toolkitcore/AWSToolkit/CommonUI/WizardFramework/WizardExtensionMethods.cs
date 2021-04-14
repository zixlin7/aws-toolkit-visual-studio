using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    public static class WizardExtensionMethods
    {
        private static class PropertyKeys
        {
            /// <summary>
            /// AccountViewModel instance of the selected account 
            /// </summary>
            public static readonly string SelectedAccount = CommonWizardProperties.AccountSelection.propkey_SelectedAccount;

            /// <summary>
            /// ToolkitRegion instance for the region to deploy into
            /// </summary>
            public static readonly string SelectedRegion = CommonWizardProperties.AccountSelection.propkey_SelectedRegion;
        }

        public static AccountViewModel GetSelectedAccount(this IAWSWizard hostingWizard)
        {
            return hostingWizard?.GetProperty<AccountViewModel>(PropertyKeys.SelectedAccount);
        }

        public static void SetSelectedAccount(this IAWSWizard hostingWizard, AccountViewModel accountViewModel)
        {
            hostingWizard[PropertyKeys.SelectedAccount] = accountViewModel;
        }

        public static ToolkitRegion GetSelectedRegion(this IAWSWizard hostingWizard)
        {
            return hostingWizard?.GetProperty<ToolkitRegion>(PropertyKeys.SelectedRegion);
        }

        public static void SetSelectedRegion(this IAWSWizard hostingWizard, ToolkitRegion region)
        {
            hostingWizard[PropertyKeys.SelectedRegion] = region;
        }

        /// <summary>
        /// Overload to get selected account from the hosting wizard using the provided property key
        /// </summary>
        public static AccountViewModel GetSelectedAccount(this IAWSWizard hostingWizard, string selectedAccountKey)
        {
            if (string.IsNullOrWhiteSpace(selectedAccountKey))
            {
                return null;
            }
            return hostingWizard?.GetProperty<AccountViewModel>(selectedAccountKey);
        }

        /// <summary>
        /// Overload to set selected account for the hosting wizard using the provided property key
        /// </summary>
        public static void SetSelectedAccount(this IAWSWizard hostingWizard, AccountViewModel accountViewModel, string selectedAccountKey)
        {
            hostingWizard[selectedAccountKey] = accountViewModel;
        }

        /// <summary>
        /// Overload to get selected region from the hosting wizard using the provided property key
        /// </summary>
        public static ToolkitRegion GetSelectedRegion(this IAWSWizard hostingWizard, string selectedRegionKey)
        {
            if (string.IsNullOrWhiteSpace(selectedRegionKey))
            {
                return null;
            }
            return hostingWizard?.GetProperty<ToolkitRegion>(selectedRegionKey);
        }

        /// <summary>
        /// Overload to set selected region for the hosting wizard using the provided property key
        /// </summary>
        public static void SetSelectedRegion(this IAWSWizard hostingWizard, ToolkitRegion region, string selectedRegionKey)
        {
            hostingWizard[selectedRegionKey] = region;
        }
    }
}
