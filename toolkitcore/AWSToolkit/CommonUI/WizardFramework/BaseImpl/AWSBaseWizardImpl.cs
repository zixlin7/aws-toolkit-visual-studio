using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Emit;
using System.Windows;
using System.Windows.Controls;
using log4net;
using Amazon.AWSToolkit.MobileAnalytics;

namespace Amazon.AWSToolkit.CommonUI.WizardFramework
{
    /// <summary>
    /// Common functionality across various types of wizard
    /// </summary>
    internal class AWSBaseWizardImpl
    {
        /// <summary>
        /// Property name for page title that will be used to fire notification updates as the
        /// active page changes. This can be used to drive a heading control in the wizard.
        /// </summary>
        public const string ActivePageTitlePropertyName = "ActivePageTitle";

        /// <summary>
        /// Property name for page description that will be used to fire notification updates as the
        /// active page changes. This can be used to drive a heading control in the wizard.
        /// </summary>
        public const string ActivePageDescriptionPropertyName = "ActivePageDescription";

        /// <summary>
        /// The property name that will be used to fire notification updates as the
        /// active page changes. This can be used to drive a grouping control in the wizard.
        /// </summary>
        public const string ActiveTableOfContentEntryPropertyName = "ActiveTableOfContentEntry";

        /// <summary>
        /// The property name that is used to fire a notification update when page
        /// groups are registered
        /// </summary>
        public const string HasPageGroupsPropertyName = "HasPageGroups";

        IAWSWizard _hostingWizard;
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSBaseWizardImpl));

        /// <summary>
        /// The list of groups and pages added to the wizard
        /// </summary>
        public readonly List<PageGroup> RegisteredPageGroups = new List<PageGroup>
        {
            new PageGroup{GroupName = AWSWizardConstants.DefaultPageGroup}
        };

        /// <summary>
        /// Holds the set of parameters entered by the user as they progress through the wizard; page
        /// controllers post/retrieve the data in their activating/deactivating handlers
        /// </summary>
        readonly Dictionary<string, object> _wizardProperties = new Dictionary<string, object>();

        /// <summary>
        /// Tracks the index to a page, comprised of its parent group and page
        /// index within the group
        /// </summary>
        internal class PageReference
        {
            public int GroupIndex { get; set; }
            public int PageIndex { get; set; }

            public PageReference Clone()
            {
                return new PageReference { GroupIndex = this.GroupIndex, PageIndex = this.PageIndex };
            }

            public bool PriorPagesExist
            {
                get
                {
                    if (GroupIndex == 0)
                        return PageIndex > 0;

                    return true;
                }
            }

            // helps with debugging
            public override string ToString()
            {
                return string.Format("Group {0}, Page {0}", GroupIndex, PageIndex);
            }
        }

        /// <summary>
        /// Allows for an initial temporary 'landing' page in the wizard that may get skipped,
        /// allowing a subsequent page to take over 'first page' responsibilities.
        /// </summary>
        private PageReference _firstActivatedPage;

        /// <summary>
        /// ID of the page to jump to if user presses Finish early; default is to have none
        /// and the wizard just closes
        /// </summary>
        string _shortCircuitPageID;

        /// <summary>
        /// Construct for a given wizard identifier; this identifier can be shared with
        /// the original requestor code and interested 3rd parties to supply content into
        /// the wizard at runtime.
        /// </summary>
        /// <param name="wizardID">(Optional) Unique ID assigned to the wizard by the original requestor</param>
        /// <param name="initialProperties">Initial properties to seed into wizard environment; may be null</param>
        public AWSBaseWizardImpl(string wizardID, IDictionary<string, object> initialProperties)
        {
            ActivePageReference = null;
            WizardID = wizardID;

            _wizardProperties.Add(CommonWizardProperties.propkey_InteractiveMode, true);

            if (initialProperties == null || initialProperties.Count <= 0) return;

            foreach (var key in initialProperties.Keys.Where(key => initialProperties[key] != null))
            {
                _wizardProperties.Add(key, initialProperties[key]);
            }
        }

        public string WizardID { get; protected set; }

        public IAWSWizardPageController ActivePageController
        {
            get
            {
                return ActivePageReference == null ? null : PageFromIndexes(ActivePageReference);
            }
        }

        /// <summary>
        /// The page reference to the page the wizard is currently displaying
        /// </summary>
        public PageReference ActivePageReference { get; protected set; }

        public PageGroup ActivePageGroup
        {
            get
            {
                return ActivePageReference == null ? null : RegisteredPageGroups[ActivePageReference.GroupIndex];
            }
        }

        public int PageGroupCount
        {
            get { return RegisteredPageGroups.Count; }
        }

        public bool HasPageGroups
        {
            get { return PageGroupCount > 1; }    
        }

        public int TotalPageCount
        {
            get { return RegisteredPageGroups.Sum(@group => @group.Pages.Count); }
        }

        /// <summary>
        /// Gets or sets the collection of page group names. If groups are going to be
        /// used, this must be called before any page controllers are registered.
        /// </summary>
        public IEnumerable<string> PageGroupNames
        {
            get
            {
                return RegisteredPageGroups.Select(@group => @group.GroupName).ToList();
            }
            set
            {
                // groups must be specified before pages can be added
                if (PageGroupCount > 1 || RegisteredPageGroups[0].Pages.Count > 0)
                    throw new InvalidOperationException("Groups must be specified prior to pages being registered");

                RegisteredPageGroups.Clear();
                foreach (var group in value)
                {
                    RegisteredPageGroups.Add(new PageGroup { GroupName = group });
                }

                _hostingWizard.NotifyPropertyChanged(HasPageGroupsPropertyName);
            }
        }

        private List<TableOfContentEntry> _tableOfContents; 
        public IEnumerable<TableOfContentEntry> TableOfContents
        {
            get
            {
                if (_tableOfContents == null)
                {
                    _tableOfContents = new List<TableOfContentEntry>();
                    foreach (var group in RegisteredPageGroups)
                    {
                        _tableOfContents.Add(new TableOfContentEntry { GroupName = group.GroupName });
                        foreach (var page in group.Pages)
                        {
                            if (!string.IsNullOrEmpty(page.ShortPageTitle))
                                _tableOfContents.Add(new TableOfContentEntry { GroupName = group.GroupName, PageName = page.ShortPageTitle });
                        }
                    }
                }

                return _tableOfContents;
            }
        }

        public TableOfContentEntry ActiveTableOfContentEntry { get; set; }

        internal void SetHostWizard(IAWSWizard hostWizard)
        {
            if (this._hostingWizard != null)
                throw new InvalidOperationException("Hosting wizard can be set once only");

            this._hostingWizard = hostWizard;
        }

        /// <summary>
        /// The wizard is about to run; do final preparations based on the configured pages
        /// </summary>
        internal void FinalizeForRun()
        {
            if (TotalPageCount == 0)
                throw new InvalidOperationException("No pages have been registered with the wizard");
        }

        internal bool PreviousPagesExist
        {
            get
            {
                return ActivePageReference != null && ActivePageReference.PriorPagesExist;
            }
        }

        internal bool FurtherPagesExist
        {
            get
            {
                if (ActivePageReference == null)
                    return false;

                // clearer to account for the 0/1 indexing/count differences outside the return statement
                var atGroup = ActivePageReference.GroupIndex + 1;
                var atGroupPage = RegisteredPageGroups[ActivePageReference.GroupIndex].Pages.Count + 1;

                return atGroup < PageGroupCount
                    && atGroupPage < RegisteredPageGroups[ActivePageReference.GroupIndex].Pages.Count;
            }
        }

        internal bool ActivePageIsFinal
        {
            get
            {
                return ActivePageReference.GroupIndex == PageGroupCount - 1 
                    && ActivePageReference.PageIndex == RegisteredPageGroups[ActivePageReference.GroupIndex].Pages.Count - 1;
            }
        }

        /// <summary>
        /// Inserts/appends a batch of one or more page controllers based on priority. If group names
        /// have been registered, each page must return a group name corresponding to one of the
        /// pre-registered groups.
        /// </summary>
        /// <param name="pageControllers"></param>
        /// <param name="priority"></param>
        /// <param name="hostingWizard"></param>
        internal void RegisterPageControllers(IEnumerable<IAWSWizardPageController> pageControllers, 
                                              int priority, 
                                              IAWSWizard hostingWizard)
        {
            if (HasCustomPageGroups)
            {
                var awsWizardPageControllers = pageControllers as IAWSWizardPageController[] ?? pageControllers.ToArray();
                foreach (var page in awsWizardPageControllers)
                {
                    var group = FindGroup(page.PageGroup);
                    if (group == null)
                        throw new ArgumentException(string.Format("Unrecognized group name {0} for page with id {1}", page.PageGroup, page.PageID));

                    group.RegisterPageControllers(hostingWizard, new[] { page }, priority);
                }
            }
            else
            {
                var group = FindGroup(AWSWizardConstants.DefaultPageGroup);
                group.RegisterPageControllers(hostingWizard, pageControllers, priority);
            }
        }

        internal bool HasCustomPageGroups
        {
            get
            {
                if (PageGroupCount == 0)
                    return false;

                if (PageGroupCount == 1 && RegisteredPageGroups[0].GroupName == AWSWizardConstants.DefaultPageGroup)
                    return false;

                return true;
            }
        }

        internal PageGroup FindGroup(string groupName)
        {
            var groupToFind = string.IsNullOrEmpty(groupName) ? AWSWizardConstants.DefaultPageGroup : groupName;
            return (from @group in RegisteredPageGroups 
                    where string.Equals(@group.GroupName, groupToFind, StringComparison.OrdinalIgnoreCase) 
                    select @group).FirstOrDefault();
        }

        internal ILog Logger { get { return LOGGER; } }

        internal string PageErrorText { get; set; }

        internal Func<bool> CommitAction
        {
            get;
            set;
        }

        /// <summary>
        /// Polls registered page controllers in the specified direction to determine the next page
        /// that should be presented to the user. The contents of the supplied collection container
        /// will be replaced with the UI control for the page to be shown.
        /// </summary>
        /// <param name="navigationReason"></param>
        /// <param name="pageContainer"></param>
        internal void TransitionPage(AWSWizardConstants.NavigationReason navigationReason, UIElementCollection pageContainer)
        {
            if (TotalPageCount == 0)
                throw new InvalidOperationException("No pages have been registered with the wizard");

            if (navigationReason == AWSWizardConstants.NavigationReason.finishPressed)
                throw new InvalidOperationException("TransitionPage should be called for back/forward navigation only");

            // can't assume next page in registered set wants to show itself, so must walk controllers
            PageReference pageToShow = null;
            var outgoingPage = ActivePageController;

            // we have to call this now even though we don't know where we'll end up, in case the next
            // page's visibility decision depends on properties the outgoing page might be about to post
            if (outgoingPage != null)
            {
                if (!outgoingPage.PageDeactivating(navigationReason))
                    return;
            }

            PageReference pageReference;
            IAWSWizardPageController page;

            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                // active ref will be null on wizard startup, so position us onto
                // the first registered page rather than peeking ahead
                if (ActivePageReference != null) 
                {
                    pageReference = ActivePageReference.Clone();
                    page = PeekNextPage(pageReference);
                }
                else
                {
                    pageReference = new PageReference {GroupIndex = 0, PageIndex = 0};
                    page = PageFromIndexes(pageReference);
                }

                while (page != null && pageToShow == null)
                {
                    if (page.QueryPageActivation(navigationReason))
                    {
                        if (_firstActivatedPage == null)
                            _firstActivatedPage = pageReference;
                        pageToShow = pageReference;
                    }
                    else
                        page = PeekNextPage(pageReference);
                }
            }
            else
            {
                pageReference = ActivePageReference.Clone();
                page = PeekPreviousPage(pageReference);
                while (page != null && pageToShow == null)
                {
                    if (page.QueryPageActivation(navigationReason))
                        pageToShow = pageReference;
                    else
                        page = PeekPreviousPage(pageReference);
                }
            }

            if (pageToShow != null)
                ActivatePage(navigationReason, pageToShow, pageContainer);
        }

        /// <summary>
        /// Returns the page at an index in a group or null if we've run out of pages.
        /// </summary>
        /// <param name="pageReference"></param>
        /// <returns></returns>
        IAWSWizardPageController PageFromIndexes(PageReference pageReference)
        {
            if (pageReference == null)
                throw new ArgumentNullException();

            return PageFromIndexes(pageReference.GroupIndex, pageReference.PageIndex);
        }

        /// <summary>
        /// Returns the page at an index in a group or null if we've run out of pages.
        /// </summary>
        /// <param name="groupIndex"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        IAWSWizardPageController PageFromIndexes(int groupIndex, int pageIndex)
        {
            if (groupIndex < 0 || pageIndex < 0)
                throw new ArgumentOutOfRangeException();

            if (groupIndex >= PageGroupCount || pageIndex >= RegisteredPageGroups[groupIndex].Pages.Count)
                return null;

            return RegisteredPageGroups[groupIndex].Pages[pageIndex];
        }

        /// <summary>
        /// Maps the reference to a page to the closest TOC entry
        /// </summary>
        /// <param name="pageRef"></param>
        /// <returns></returns>
        TableOfContentEntry GetTableOfContentEntryForReference(PageReference pageRef)
        {
            var toc = TableOfContents;
            // try and map to page by preference, then go by group if no relevant page
            var tocCount = toc.Count();
            if (tocCount == 0)
                return null;

            var groupName = RegisteredPageGroups[pageRef.GroupIndex].GroupName;
            var pageName = RegisteredPageGroups[pageRef.GroupIndex].Pages[pageRef.PageIndex].ShortPageTitle;

            var tocIndex = 0;
            var tocEntry = toc.ElementAt(tocIndex);
            while (tocEntry != null)
            {
                if (tocEntry.GroupName.Equals(groupName, StringComparison.Ordinal))
                {
                    var tocGroup = tocEntry;

                    tocIndex++;
                    tocEntry = tocIndex < tocCount ? toc.ElementAt(tocIndex) : null;
                    while (tocEntry != null)
                    {
                        if (!string.IsNullOrEmpty(pageName) && pageName.Equals(tocEntry.PageName))
                            return tocEntry;

                        tocIndex++;
                        tocEntry = tocIndex < tocCount ? toc.ElementAt(tocIndex) : null;
                    }

                    // subordinate page not in toc, so go up and activate group parent
                    return tocGroup;
                }

                tocIndex++;
                tocEntry = tocIndex < tocCount ? toc.ElementAt(tocIndex) : null;
            }

            return null;
        }

        /// <summary>
        /// Peeks for the next available page beyond the supplied indexes, returning the
        /// page and updating the indexes if successful.
        /// If no page is available, the indexes in the supplied reference are returned 
        /// unchanged.
        /// </summary>
        /// <param name="pageRef"></param>
        /// <returns></returns>
        IAWSWizardPageController PeekNextPage(PageReference pageRef)
        {
            if (pageRef == null)
                throw new ArgumentNullException();

            var groupIndex = pageRef.GroupIndex;
            var pageIndex = pageRef.PageIndex;

            if (groupIndex < 0 || pageIndex < 0 || groupIndex >= PageGroupCount)
                throw new ArgumentOutOfRangeException();

            pageIndex++;
            if (pageIndex >= RegisteredPageGroups[groupIndex].Pages.Count)
            {
                groupIndex++;
                if (groupIndex == PageGroupCount)
                    return null;

                pageIndex = 0;
            }

            var page = PageFromIndexes(groupIndex, pageIndex);
            if (page != null)
            {
                pageRef.GroupIndex = groupIndex;
                pageRef.PageIndex = pageIndex;
            }

            return page;
        }

        /// <summary>
        /// Peeks for the immediately prior page, returning the page and updating the
        /// indexes if successful.
        /// If no page is available, the page reference is returned unchanged.
        /// </summary>
        /// <param name="pageRef"></param>
        /// <returns></returns>
        IAWSWizardPageController PeekPreviousPage(PageReference pageRef)
        {
            if (pageRef == null)
                throw new ArgumentNullException();

            var groupIndex = pageRef.GroupIndex;
            var pageIndex = pageRef.PageIndex;

            if (groupIndex < 0 || pageIndex < 0 || groupIndex >= PageGroupCount)
                throw new ArgumentOutOfRangeException();

            pageIndex--;
            if (pageIndex < 0)
            {
                groupIndex--;
                if (groupIndex < 0)
                    return null;

                pageIndex = RegisteredPageGroups[groupIndex].Pages.Count - 1;
            }

            var page = PageFromIndexes(groupIndex, pageIndex);
            if (page != null)
            {
                pageRef.GroupIndex = groupIndex;
                pageRef.PageIndex = pageIndex;
            }

            return page;
        }

        /// <summary>
        /// Activates the specified page within the wizard
        /// </summary>
        /// <param name="navigationReason"></param>
        /// <param name="pageToShow"></param>
        /// <param name="pageContainer"></param>
        void ActivatePage(AWSWizardConstants.NavigationReason navigationReason, PageReference pageToShow, UIElementCollection pageContainer)
        {
            ActivePageReference = pageToShow;

            var pageController = PageFromIndexes(pageToShow);
            var pageUIControl = pageController.PageActivating(navigationReason);
            if (ActiveTableOfContentEntry != null)
                ActiveTableOfContentEntry.IsActive = false;

            ActiveTableOfContentEntry = GetTableOfContentEntryForReference(pageToShow);
            ActiveTableOfContentEntry.IsActive = true;

            pageContainer.Clear();
            pageUIControl.HorizontalAlignment = HorizontalAlignment.Stretch;
            pageContainer.Add(pageUIControl);

            // auto-enable back/next depending on whether we're at the end of the run; the
            // page may choose to override if it determines not all its mandatory data has
            // been supplied
            _hostingWizard.SetNavigationEnablement(pageController, AWSWizardConstants.NavigationButtons.Back, PreviousPagesExist);
            _hostingWizard.SetNavigationEnablement(pageController, AWSWizardConstants.NavigationButtons.Forward, FurtherPagesExist);

            foreach (var activePageProperty in new[]
            {
                ActivePageTitlePropertyName, 
                ActivePageDescriptionPropertyName, 
                ActiveTableOfContentEntryPropertyName, 
            })
            {
                _hostingWizard.NotifyPropertyChanged(activePageProperty);
            }

            pageController.PageActivated(navigationReason);

            ToolkitEvent toolkitEvent = new ToolkitEvent(AMAConstants.EventTypes.WizardTrackingEvent);
            toolkitEvent.AddProperty(MetricKeys.PageIndex, pageToShow.PageIndex);
            toolkitEvent.AddProperty(MetricKeys.GroupIndex, pageToShow.GroupIndex);
            toolkitEvent.AddProperty(AttributeKeys.NavigationReason, navigationReason.ToString());
            toolkitEvent.AddProperty(AttributeKeys.OpenViewFullIdentifier, _hostingWizard.WizardID);
            SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(toolkitEvent);
        }

        /// <summary>
        /// Finish was pressed whilst on current page; notify the page of the transition in
        /// case property storage is required and all downstream pages. 
        /// If a short-circuit page has been designated, jump to that page otherwise we
        /// can close the wizard.
        /// </summary>
        /// <returns>True if the wizard should be closed (no short circuit page set)</returns>
        /// <remarks>
        /// We may want to consider notifying downstream pages of the short-circuit, in case
        /// they want to push default properties into the environment. If done via existing
        /// OnPageDeactivation call however this would mean pages need to be aware they may
        /// not have launched any UI.
        /// </remarks>
        internal bool FinishPressed(UIElementCollection pageContainer)
        {
            ActivePageController.PageDeactivating(AWSWizardConstants.NavigationReason.finishPressed);

            if (ActivePageIsFinal || string.IsNullOrEmpty(this._shortCircuitPageID))
                return CheckFinalForCommitAction();

            PageReference stopShortCircuitAtPage = null;
            var pageReference = ActivePageReference.Clone();
            IAWSWizardPageController page;

            do
            {
                page = PeekNextPage(pageReference);
                if (page == null)
                    break;

                if (!page.AllowShortCircuit())
                    stopShortCircuitAtPage = pageReference;

            } while (stopShortCircuitAtPage == null);
                
            // someone's not happy, go direct to them...
            if (stopShortCircuitAtPage != null)
            {
                ActivatePage(AWSWizardConstants.NavigationReason.finishPressed, stopShortCircuitAtPage, pageContainer);
                return false;
            }

            // find the designated short circuit page (most likely the last, so walk backwards for efficiency)
            // and jump to it
            pageReference.GroupIndex = PageGroupCount - 1;
            pageReference.PageIndex = RegisteredPageGroups[pageReference.GroupIndex].Pages.Count - 1;

            page = PageFromIndexes(pageReference);
            while (page != null 
                    && pageReference.GroupIndex >= ActivePageReference.GroupIndex 
                    && pageReference.PageIndex > ActivePageReference.PageIndex)
            {
                if (string.Compare(_shortCircuitPageID, page.PageID, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    ActivatePage(AWSWizardConstants.NavigationReason.finishPressed, pageReference, pageContainer);
                    return false;
                }

                page = PeekPreviousPage(pageReference);
            }

            // FAIL-SAFE -- make sure the wizard closes if short circuit failed
            return CheckFinalForCommitAction();
        }

        bool CheckFinalForCommitAction()
        {
            if (this.CommitAction == null) return true;

            try
            {
                return this.CommitAction();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Unknown Error", "Unknown Error: " + e.Message);
            }

            return true;
        }

        /// <summary>
        /// Used to force Finish enablement if we are on the last possible page
        /// </summary>
        /// <param name="pageController"></param>
        /// <returns></returns>
        internal bool IsFinalPage(IAWSWizardPageController pageController)
        {
            if (TotalPageCount == 0)
                return false; // no pages ever registered!

            var lastGroupIndex = PageGroupCount - 1;
            var lastRegisteredPage = PageFromIndexes(lastGroupIndex, RegisteredPageGroups[lastGroupIndex].Pages.Count - 1);
            return string.CompareOrdinal(pageController.PageID, lastRegisteredPage.PageID) == 0;
        }

        /// <summary>
        /// Sets a property value in the runtime environment of the wizard; if 
        /// a value already exists for the specified key it is overwritten.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        internal void SetProperty(string key, object value)
        {
            bool isSet = _wizardProperties.ContainsKey(key);
            if (value != null)
            {
                if (!isSet)
                    _wizardProperties.Add(key, value);
                else
                    _wizardProperties[key] = value;
            }
            else
                if (isSet)
                    _wizardProperties.Remove(key);
        }

        /// <summary>
        /// Called on transition from temporary initial landing page, causes
        /// the next page to respond to take over 'first page' duties
        /// </summary>
        internal void ResetFirstActivePage()
        {
            _firstActivatedPage = null;
        }

        /// <summary>
        /// Push one or more properties into the wizard environment as a batch;
        /// properties that already have values will be rewritten.
        /// </summary>
        /// <param name="properties"></param>
        internal void SetProperties(Dictionary<string, object> properties)
        {
            foreach (var key in properties.Keys)
                SetProperty(key, properties[key]);
        }

        /// <summary>
        /// Returns the value, if set, from the wizards runtime environment.
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal object GetProperty(string key)
        {
            object value;
            _wizardProperties.TryGetValue(key, out value);
            return value;
        }

        internal bool GetBoolProperty(string key)
        {
            var value = GetProperty(key);
            return (value != null) && (bool)value;
        }

        internal T GetPropertyValue<T>(string key)
        {
            object value;
            if (!_wizardProperties.TryGetValue(key, out value)) return default(T);

            var convertedValue = (T)Convert.ChangeType(value, typeof(T));
            return convertedValue;
        }

        /// <summary>
        /// Return indication of whether a given property has been set (as
        /// distinct from set-but-has-null-value)
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        internal bool IsPropertySet(string key)
        {
            return _wizardProperties.ContainsKey(key);
        }

        /// <summary>
        /// Polls downstream pages from the requestor to determine if short-circuit
        /// enablement of the Finish button is possible. 
        /// </summary>
        /// <param name="requestorPage"></param>
        /// <returns></returns>
        internal bool RequestFinishEnablement(IAWSWizardPageController requestorPage)
        {
            // if the last page is asking, then it's a no brainer
            if (IsFinalPage(requestorPage))
                return true;

            var pageReference = ActivePageReference.Clone();

            var page = PeekNextPage(pageReference);
            while (page != null)
            {
                if (page != requestorPage)
                    if (!page.QueryFinishButtonEnablement())
                        return false;

                page = PeekNextPage(pageReference);
            }

            return true;
        }

        /// <summary>
        /// Returns a copy of the current wizard property set
        /// </summary>
        /// <returns></returns>
        internal Dictionary<string, object> CopyProperties()
        {
            return new Dictionary<string, object>(_wizardProperties);
        }

        /// <summary>
        /// Records the id of the page to skip to if Finish is pressed early by the user;
        /// the default is to have no short-circuit page and simply close the wizard after
        /// notifying downstream pages
        /// </summary>
        /// <param name="shortCircuitPageID">ID of the page to short to</param>
        internal void SetShortCircuitPage(string shortCircuitPageID)
        {
            if (string.IsNullOrEmpty(shortCircuitPageID))
            {
                _shortCircuitPageID = string.Empty;
                return;
            }

            if (string.CompareOrdinal(shortCircuitPageID, AWSWizardConstants.WizardPageReferences.LastPageID) == 0)
            {
                var lastGroupIndex = PageGroupCount - 1;
                _shortCircuitPageID = PageFromIndexes(lastGroupIndex, RegisteredPageGroups[lastGroupIndex].Pages.Count - 1).PageID;
                return;
            }

            if (string.CompareOrdinal(shortCircuitPageID, AWSWizardConstants.WizardPageReferences.FirstPageID) == 0)
            {
                // bit odd to set short circuit as first page but...
                _shortCircuitPageID = PageFromIndexes(0, 0).PageID;
                return;
            }

            // check 0 again in case caller did not use firstpageid constant
            var pageReference = new PageReference {GroupIndex = 0, PageIndex = 0};
            var page = PageFromIndexes(pageReference);
            while (page != null)
            {
                if (string.CompareOrdinal(shortCircuitPageID, page.PageID) == 0)
                {
                    _shortCircuitPageID = shortCircuitPageID;
                    return;
                }

                page = PeekNextPage(pageReference);
            }
            
            throw new ArgumentException("Unknown page ID to use as short circuit page - " + shortCircuitPageID);
        }

        private AWSBaseWizardImpl() { }

        internal void NotifyForwardPagesReset(IAWSWizardPageController requestorPage)
        {
            if (IsFinalPage(requestorPage))
                return;

            var pageReference = ActivePageReference.Clone();


            var page = PeekNextPage(pageReference);
            while (page != null)
            {
                if (page != requestorPage)
                {
                    page.ResetPage();
                }

                page = PeekNextPage(pageReference);
            }
        }
    }

    /// <summary>
    /// Groups together the pages belonging to a group in a wizard
    /// </summary>
    public class PageGroup
    {
        /// <summary>
        /// The name of the group; if more than one group is configured this
        /// will be displayed in a navigation helper panel on the left side
        /// of the wizard
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// The set of pages registered as belonging to the group
        /// </summary>
        public List<IAWSWizardPageController> Pages = new List<IAWSWizardPageController>();

        public void RegisterPageControllers(IAWSWizard hostingWizard, IEnumerable<IAWSWizardPageController> pageControllers, int priority)
        {
            foreach (var controller in pageControllers)
            {
                // todo: come up with some form of priority insertion, 'after this page' or 'before this
                // page' scheme
                controller.HostingWizard = hostingWizard;
                Pages.Add(controller);
            }
        }
    }

    public class TableOfContentEntry : INotifyPropertyChanged
    {
        internal string GroupName { get; set; }
        internal string PageName { get; set; }

        public string DisplayName
        {
            get
            {
                return string.IsNullOrEmpty(PageName) ? GroupName : PageName;
            }
        }

        public bool IsIndented
        {
            get { return !string.IsNullOrEmpty(PageName); }
        }

        private bool _isActive = false;

        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                _isActive = value;
                NotifyPropertyChanged("IsActive");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
