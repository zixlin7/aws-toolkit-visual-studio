using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    public static class AWSWizardConstants
    {
        /// <summary>
        /// 'Special' references to certain pages in a wizard, allowing first/last etc to
        /// be specified without the need for explicit references to the actual controllers
        /// of those pages.
        /// </summary>
        public static class WizardPageReferences
        {
            public static readonly string FirstPageID = "FirstPageID";
            public static readonly string LastPageID = "LastPageID";
        }

        /// <summary>
        /// For wizards that do not use page grouping, use this name when registering
        /// page controllers
        /// </summary>
        public static string DefaultPageGroup = string.Empty;

        /// <summary>
        /// Enumeration describing the possible navigation buttons on a wizard; not all
        /// wizard types will necessarily have the full set of buttons
        /// </summary>
        public enum NavigationButtons
        {
            Help,
            Cancel,
            Back,
            Forward,
            Finish
        }

        /// <summary>
        /// Expresses the reason for page transition
        /// </summary>
        public enum NavigationReason
        {
            movingBack,
            movingForward,
            finishPressed
        }

        /// <summary>
        /// Set of keys that can be used to customize the wizard environment by including
        /// them with properties before the wizard runs
        /// </summary>
        public static class WizardOptions
        {
            /// <summary>
            /// String, the resource key of the background brush for the navigation buttons container.
            /// Default background is transparent.
            /// </summary>
            public static readonly string propkey_NavContainerBackground = "navContainerBackground";
        }
    }
}
