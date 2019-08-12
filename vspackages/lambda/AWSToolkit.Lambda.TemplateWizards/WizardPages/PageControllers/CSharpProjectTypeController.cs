using System;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageUI;

using log4net;
using System.ComponentModel;
using Amazon.AWSToolkit.Lambda.TemplateWizards.Model;

namespace Amazon.AWSToolkit.Lambda.TemplateWizards.WizardPages.PageControllers
{
    public class CSharpProjectTypeController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CSharpProjectTypePage));

        string _blueprintTypes;
        BlueprintsModel _blueprintsModel;
        CSharpProjectTypePage _pageUI;

        string _pageTitle;
        string _pageDescription;
        string[] _requiredTags;

        public CSharpProjectTypeController(string pageTitle, string pageDescription, string[] requiredTags, string blueprintTypes)
        {
            this._pageTitle = pageTitle;
            this._pageDescription = pageDescription;
            this._requiredTags = requiredTags;
            this._blueprintTypes = blueprintTypes;
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageID => GetType().FullName;

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => this._pageTitle;

        public string ShortPageTitle => null;

        public string PageDescription => this._pageDescription;

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        public void ResetPage()
        {

        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard.RequestFinishEnablement(this);
            TestForwardTransitionEnablement();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                try
                {
                    _blueprintsModel = BlueprintsManifest.Deserialize(this._blueprintTypes);
                }
                catch(Exception e)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Error retrieving Blueprints: " + e.Message);
                    LOGGER.Error("Error retrieving Blueprints", e);
                }
                _pageUI = new CSharpProjectTypePage(this, _blueprintsModel, this._requiredTags);
                this._pageUI.PropertyChanged += new PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }

            return _pageUI;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                return StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI == null)
                    return false;

                return _pageUI.SelectedBlueprint != null;
            }
        }

        void _pageUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.StorePageData();
            TestForwardTransitionEnablement();
        }

        bool StorePageData()
        {
            try
            {
                HostingWizard[CSharpWizardPropertyNameConstants.propKey_SelectedBlueprint] = _pageUI.SelectedBlueprint;
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating AWS Lambda Project: " + e.Message);
                return false;
            }
        }
    }
}
