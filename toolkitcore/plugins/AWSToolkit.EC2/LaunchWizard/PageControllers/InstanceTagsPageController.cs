using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    class InstanceTagsPageController : IAWSWizardPageController
    {
        InstanceTagsPage _pageUI;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Tags"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Define custom tags for the instance(s)."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
                _pageUI = new InstanceTagsPage(this);

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            // rebind in case of reference changes elsewhere and also take sensible action if prior pages/wizard
            // didn't create a default collection
            ICollection<Tag> currentTags = HostingWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] as ICollection<Tag>;
            if (currentTags == null)
            {
                UpdateNameInstanceTagValue(HostingWizard, string.Empty);
                currentTags = HostingWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] as ICollection<Tag>;
            }

            _pageUI.InstanceTags = currentTags;
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
            ICollection<Tag> tags = _pageUI.InstanceTags;
            HostingWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] = tags;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                return true;
            }
        }

        internal static void UpdateNameInstanceTagValue(IAWSWizard hostWizard, string newValue)
        {
            Tag nameTag = null;
            List<Tag> currentTags = hostWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] as List<Tag>;
            if (currentTags == null)
            {
                // there is a limit of 10 tags in the api, but don't expose this in the collection - let the UI control it
                currentTags = new List<Tag>();
                hostWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] = currentTags;
            }
            else
            {
                // is always special first tag in console but again, let UI worry about that
                nameTag = currentTags.Find((T) => string.Compare(T.Key, EC2Constants.TAG_NAME, true) == 0);
            }

            if (nameTag == null)
            {
                nameTag = new Tag() { Key = EC2Constants.TAG_NAME };
                currentTags.Add(nameTag);
            }

            nameTag.Value = newValue;
        }
    }

}
