using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Regions;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Definitions of wizard property keys used by pages that span multiple wizards or plugin contributors
    /// </summary>
    public static class CommonWizardProperties
    {
        /// <summary>
        /// Boolean. True (default) if the wizard ran it's usual UI under end-user control.
        /// </summary>
        public static readonly string propkey_InteractiveMode = "interactiveMode";

        /// <summary>
        /// Navigator's root model, value is instance of AWSViewModel
        /// </summary>
        public static readonly string propkey_NavigatorRootViewModel = "navigatorRootViewModel";

        /// <summary>
        /// The logical VS shell version number: '2013', '2015' or '2017'
        /// </summary>
        public static readonly string propkey_HostShellVersion = "hostShellVersion";

        public static class AccountSelection
        {
            /// <summary>
            /// AccountViewModel instance of the selected account 
            /// </summary>
            public static readonly string propkey_SelectedAccount = "selected_account";

            /// <summary>
            /// ToolkitRegion instance for the region to deploy into
            /// </summary>
            public static readonly string propkey_SelectedRegion = "selectedRegion";

            public static AccountViewModel GetSelectedAccount(IDictionary<string, object> properties)
            {
                if (properties.TryGetValue(propkey_SelectedAccount, out var value))
                {
                    return (AccountViewModel) Convert.ChangeType(value, typeof(AccountViewModel));
                }

                return null;
            }

            public static ToolkitRegion GetSelectedRegion(IDictionary<string, object> properties)
            {
                if (properties.TryGetValue(propkey_SelectedRegion, out var value))
                {
                    return (ToolkitRegion) Convert.ChangeType(value, typeof(ToolkitRegion));
                }

                return null;
            }
        }
    }
}
