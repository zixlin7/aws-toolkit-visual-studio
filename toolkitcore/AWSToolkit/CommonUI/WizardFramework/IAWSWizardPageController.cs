using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    /// <summary>
    /// Implemented by plug-in pages to an AWSWizard instance
    /// </summary>
    public interface IAWSWizardPageController
    {
        /// <summary>
        /// Returns a unique ID for the page controller; this is used for diagnostic purposes
        /// </summary>
        string PageID { get; }

        /// <summary>
        /// Called by the hosting wizard on page registration to give the page access to the
        /// host environment, should it require it.
        /// </summary>
        IAWSWizard HostingWizard { get;  set; }

        /// <summary>
        /// Returns the logical grouping of the page, if the host wizard supports (and
        /// displays) groups. Return an empty string (AWSWizardConstants.DefaultPageGroup
        /// to not use a group). The host wizard is free to ignore grouping.
        /// </summary>
        string PageGroup { get; }

        /// <summary>
        /// Returns the displayable title for the page when it is active. This will be
        /// placed into the header area of the hosting wizard.
        /// </summary>
        string PageTitle { get; }

        /// <summary>
        /// Returns a short (one or two word max) title that will be placed into the
        /// 'table of content' list on the wizard (if visible). Return null or an
        /// empty string to suppress a TOC entry for the page.
        /// </summary>
        string ShortPageTitle { get; }

        /// <summary>
        /// Optional, returns a simple help description for the page when active to guide
        /// users as to what it's for etc. This should be limited to one or two short lines
        /// of text and is placed into the header area or above the page content, depending
        /// on the header style in use.
        /// </summary>
        string PageDescription { get; }

        /// <summary>
        /// Called prior to displaying the page during Back/Next navigation
        /// </summary>
        /// <param name="navigatingReason">Why page activation is being considered</param>
        /// <returns>True if the page wants to be displayed, false if the page should be skipped</returns>
        bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason);

        /// <summary>
        /// Called as the page is about to be rendered during navigation. The controller should return an
        /// instance of the UI for the page to be displayed. Controls on the page UI can be initialised
        /// based on the current wizard's state or you can wait for the final PageActivated call.
        /// <param name="navigationReason">The direction of user navigation that caused activation</param>
        /// </summary>
        UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason);

        /// <summary>
        /// Called immediately after the page controller/UI pair has been added to the wizard frame; this
        /// is the last chance to set control data on the page before it becomes visible.
        /// </summary>
        /// <param name="navigationReason">The direction of user navigation that caused activation</param>
        void PageActivated(AWSWizardConstants.NavigationReason navigationReason);

        /// <summary>
        /// Called when the user attempts to navigate away from the page
        /// </summary>
        /// <param name="navigatingReason">Why page activation is being considered</param>
        /// <returns>
        /// True if the user can navigate away from the page; false to remain on the page,
        /// perhaps due to a validation error or missing required value
        /// </returns>
        bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason);

        /// <summary>
        /// Called if another page (or the wizard itself) attempts to enable the Finish button
        /// </summary>
        /// <returns>
        /// True if the button can be enabled, false otherwise (ie this page must obtain
        /// data before short-circuit Finish can be performed
        /// </returns>
        bool QueryFinishButtonEnablement();

        /// <summary>
        /// Called by page UI on field change to allow controller to determine if all
        /// mandatory fields have been completed and therefore Next can be enabled
        /// </summary>
        void TestForwardTransitionEnablement();

        /// <summary>
        /// User has pressed Finish without getting to end of the pages; validate and
        /// return false if short circuit should be halted at this page.
        /// </summary>
        /// <returns></returns>
        bool AllowShortCircuit();
    }
}
