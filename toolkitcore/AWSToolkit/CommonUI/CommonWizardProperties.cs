using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            /// RegionEndPointsManager.RegionEndPoints instance for the region to deploy into
            /// </summary>
            public static readonly string propkey_SelectedRegion = "selectedRegion";
        }
    }
}
