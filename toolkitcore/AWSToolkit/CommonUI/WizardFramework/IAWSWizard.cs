using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Interop;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    /// <summary>
    /// Interface implemented on AWS wizard instances
    /// </summary>
    public interface IAWSWizard
    {
        /// <summary>
        /// Returns the unique ID assigned to the wizard by the original requestor, 
        /// to be used to identify the wizard by 3rd parties wishing to contribute
        /// </summary>
        string WizardID { get; }

        /// <summary>
        /// Sets the display title for the wizard, as appropriate for the wizard type
        /// </summary>
        string Title { set; }

        /// <summary>
        /// Queries the active page to return the page title, suitable for display in
        /// the wizard header panel.
        /// </summary>
        string ActivePageTitle { get; }

        /// <summary>
        /// Queries the active page to return one or two line descriptive text about
        /// the page.
        /// </summary>
        string ActivePageDescription { get; }

        /// <summary>
        /// Toggle display of the banner at the head of the wizard, which can be used
        /// to show a custom logo and/or additional page description text as the user
        /// progresses through the wizard
        /// </summary>
        bool DisplayHeader { get; set; }

        /// <summary>
        /// Toggle automatic polling for Finish enablement during page transition. 
        /// Defaults to True; if set False, pages must manually request Finish
        /// enablement.
        /// </summary>
        bool AutoPollFinishEnablement { get; set; }

        /// <summary>
        /// Called by a page to enable/disable a navigation button.
        /// </summary>
        /// <param name="requestorPage">The calling page</param>
        /// <param name="button"></param>
        /// <param name="enable"></param>
        /// <remarks>
        /// If called to enable the Next button, the wizard automatically
        /// calls RequestFinishEnablement for you. If 'enable' is false,
        /// Finish will be automatically disabled (as if you can't move
        /// forwards, you can't very well skip to the end!)
        /// </remarks>
        void SetNavigationEnablement(IAWSWizardPageController requestorPage, AWSWizardConstants.NavigationButtons button, bool enable);

        /// <summary>
        /// Called to request that the Finish button be enabled; all pages downstream of the
        /// requestor will be polled to determine if this is OK
        /// </summary>
        /// <param name="requestorPage">The page that wants to have Finish enabled</param>
        /// <returns>True if downstream pages consented and Finish is now enabled, false otherwise</returns>
        bool RequestFinishEnablement(IAWSWizardPageController requestorPage);

        /// <summary>
        /// Optional call to enable 'group' mode for a wizard. In this mode, pages are arranged
        /// into groups and additional navigation hints are shown so the user knows what group
        /// they are in and hence progress through the wizard.
        /// If grouping is to be used, the group names must be registered before any page controllers
        /// are added.
        /// </summary>
        /// <param name="groupNames"></param>
        void RegisterPageGroups(IEnumerable<string> groupNames);

        /// <summary>
        /// Adds one or more page controllers to the wizard environment at a suggested location
        /// in the wizard flow. If page groups have been registered, the pages will be slotted into
        /// groups according to the pre-registered names in the wizard, with the same relative
        /// priority in each group. If no groups have been registered, any group names specified
        /// by the controllers will be ignored.
        /// </summary>
        /// <param name="pageControllers"></param>
        /// <param name="priority"></param>
        void RegisterPageControllers(IEnumerable<IAWSWizardPageController> pageControllers, int priority);

        /// <summary>
        /// Returns the value associated with the specified key, if set, from the wizard's 
        /// runtime environment.
        /// </summary>
        /// <param name="key">Unique key assigned to the property</param>
        /// <returns>Null if no page has set the property</returns>
        object GetProperty(string key);

        /// <summary>
        /// Returns the value associated with the specified key, if set, from the wizard's 
        /// runtime environment. If the key is not set, the default value for the specified
        /// type is returned.
        /// </summary>
        /// <typeparam name="T">Type of the value to return</typeparam>
        /// <param name="key">Unique key assigned to the property</param>
        /// <returns>Value (or type default) for the property</returns>
        T GetProperty<T>(string key);

        /// <summary>
        /// Sets a property value in the runtime environment of the wizard; if 
        /// a value already exists for the specified key it is overwritten.
        /// </summary>
        /// <param name="key">Unique key assigned to the property</param>
        /// <param name="value">The value to be assigned; pass null to delete the property</param>
        void SetProperty(string key, object value);

        /// <summary>
        /// Adds the specified properties into the wizard environment as a batch. If any
        /// properties already have values they will be overwritten.
        /// </summary>
        /// <param name="properties">Properties collection to be added.</param>
        void SetProperties(Dictionary<string, object> properties);

        /// <summary>
        /// Returns an indication of whether the given property has been set (this is
        /// distinct from set-but-has-null-value)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        bool IsPropertySet(string key);

        /// <summary>
        /// Assembles the registered page controllers into order and executes the wizard
        /// </summary>
        /// <returns>False if the user cancels the wizard, True if it runs to completion</returns>
        bool Run();

        /// <summary>
        /// Indexer for properties in the wizard's environment
        /// </summary>
        /// <param name="propertyKey">The name of the property</param>
        /// <returns>Assigned value or null</returns>
        object this[string propertyKey] { get; set; }

        /// <summary>
        /// Returns a copy of the properties set during the wizard's run
        /// </summary>
        Dictionary<string, object> CollectedProperties { get; }

        /// <summary>
        /// Change the default text of a given navigation button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonText"></param>
        void SetNavigationButtonText(WizardFramework.AWSWizardConstants.NavigationButtons button, string buttonText);

        /// <summary>
        /// Change the text and image glyph for the specified navigation button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="buttonText"></param>
        /// <param name="buttonGlyph"></param>
        void SetNavigationButtonUI(WizardFramework.AWSWizardConstants.NavigationButtons button, string buttonText, Image buttonGlyph);

        /// <summary>
        /// Set visibility of the specified navigation button
        /// </summary>
        /// <param name="button"></param>
        /// <param name="isVisible"></param>
        void SetNavigationButtonVisibility(WizardFramework.AWSWizardConstants.NavigationButtons button, bool isVisible);

        /// <summary>
        /// Optional theme support to allow the wizard to take on some colorization from its host environment.
        /// If not set, the background panel for the navigation panel is white.
        /// </summary>
        /// <param name="navPanelBrush"></param>
        void SetNavigationPanelBackground(Brush navPanelBrush);

        /// <summary>
        /// Sets the final page to jump to if Finish is pressed before the user gets to the last page
        /// (typically the short circuit page is the last page). If not set then when the user presses
        /// Finish early we close the wizard (after notifying all downstream pages).
        /// </summary>
        /// <param name="shortCircuitPageID">
        /// The ID of the page to short to; pass empty string to clear short circuit or special page
        /// references from AWSWizardConstants.WizardPageReferences.
        /// </param>
        void SetShortCircuitPage(string shortCircuitPageID);

        /// <summary>
        /// Called on transition from temporary initial landing page, causes the next page to respond to 
        /// activation to take over 'first page' duties. The initial landing page will not be accessible
        /// again.
        /// </summary>
        void ResetFirstActivePage();

        /// <summary>
        /// Exposes logging service through the host wizard
        /// </summary>
        ILog Logger { get; }

        /// <summary>
        /// If set this function is called before the wizard is closed. This can be used for wizards that 
        /// are quick to perform the final action and so if there is an error, allow the user to go back 
        /// and fix possible issues in the wizard. 
        /// <remarks>
        /// This should not be used be used for wizards who's OK/Finish action is time consuming since the
        /// UI will be frozen until completion.
        /// </remarks>
        /// </summary>
        /// <returns>Returns true if the action was successful and the wizard can be closed.</returns>
        Func<bool> CommitAction
        {
            get;
            set;
        }

        /// <summary>
        /// Used to fire property change notifications through the host wizard UI.
        /// </summary>
        /// <param name="propertyName"></param>
        void NotifyPropertyChanged(string propertyName);

        /// <summary>
        /// Cancels the wizard
        /// </summary>
        void CancelRun();

        /// <summary>
        /// Cancels the wizard, setting the specified property and value prior to exiting
        /// to the caller.
        /// </summary>
        /// <param name="propertyKey"></param>
        /// <param name="propertyValue"></param>
        void CancelRun(string propertyKey, object propertyValue);

        /// <summary>
        /// Toggles the display of the page error panel depending on the contents
        /// of errorText.
        /// </summary>
        /// <param name="errorText">Null or empty to hide the panel, non-empty to show the specified error.</param>
        void SetPageError(string errorText);
    }
}
