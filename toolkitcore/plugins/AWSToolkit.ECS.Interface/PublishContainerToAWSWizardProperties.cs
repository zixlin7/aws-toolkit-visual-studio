﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Amazon.AWSToolkit.ECS
{
    public static class PublishContainerToAWSWizardProperties
    {
        /// <summary>
        /// Indicates the success or fail (= user closed) status of the wizard. Used
        /// because the 'review' page of the wizard does the actual upload and on
        /// success, if the auto-close-wizard option is set, invokes CancelRun to
        /// actually shut down the UI which in turn returns 'false' as the output
        /// from the wizard's Run() method.
        /// Type: Boolean.
        /// </summary>
        public static readonly string WizardResult = "wizardResult";

        /// <summary>
        /// The user account selected by the user (if control present) to own the
        /// uploaded function. This can also be used to select an account on entry
        /// to the wizard.
        /// Type: AccountViewModel.
        /// </summary>
        public static readonly string UserAccount = "userAccount";

        /// <summary>
        /// The region to host the function (if control present). This can also be
        /// used to select a region on entry to the wizard.
        /// Type: RegionEndpointsManager.RegionEndpoints.
        /// </summary>
        public static readonly string Region = "region";

    }
}
