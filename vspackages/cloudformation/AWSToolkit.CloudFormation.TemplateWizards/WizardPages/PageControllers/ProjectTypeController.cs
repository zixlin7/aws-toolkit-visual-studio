using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageUI;

using log4net;


namespace Amazon.AWSToolkit.CloudFormation.TemplateWizards.WizardPages.PageControllers
{
    public class ProjectTypeController : IAWSWizardPageController
    {
        public enum CreationMode { Empty, ExistingStack, FromSample };

        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ProjectTypePage));

        ProjectTypePage _pageUI;

        string _pageTitle;
        string _pageDescription;

        #region IAWSWizardPageController Members

        public ProjectTypeController(string pageTitle, string pageDesciption)
        {
            this._pageTitle = pageTitle;
            this._pageDescription = pageDesciption;
        }

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
            get { return this._pageTitle; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return this._pageDescription; }
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
            {
                _pageUI = new ProjectTypePage(this);
                this._pageUI.PropertyChanged += new PropertyChangedEventHandler(_pageUI_PropertyChanged);
                this._pageUI.DataContext = this;

                AccountViewModel account;
                RegionEndPointsManager.RegionEndPoints region;

                account = ToolkitFactory.Instance.Navigator.SelectedAccount;
                region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;

                this._pageUI.Initialize(account, region);
            }

            return _pageUI;
        }

        void _pageUI_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.StorePageData();
            TestForwardTransitionEnablement();
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard.RequestFinishEnablement(this);
            TestForwardTransitionEnablement();
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

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }

        #endregion

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                if(this._pageUI.CreationMode == CreationMode.Empty)
                    return true;
                if (this._pageUI.CreationMode == CreationMode.ExistingStack)
                {
                    if (this._pageUI.SelectedAccount != null && this._pageUI.SelectedRegion != null && !string.IsNullOrEmpty(this._pageUI.SelectedExistingStackName))
                        return true;
                }
                if (this._pageUI.CreationMode == CreationMode.FromSample)
                {
                    if (this._pageUI.SelectedSampleTemplate != null)
                        return true;
                }

                return false;
            }
        }

        bool StorePageData()
        {
            try
            {
                HostingWizard[WizardPropertyNameConstants.propKey_CreationMode] = this._pageUI.CreationMode;

                HostingWizard[WizardPropertyNameConstants.propKey_SelectedAccount] = this._pageUI.SelectedAccount;
                HostingWizard[WizardPropertyNameConstants.propKey_SelectedRegion] = this._pageUI.SelectedRegion;
                HostingWizard[WizardPropertyNameConstants.propKey_ExistingStackName] = this._pageUI.SelectedExistingStackName;

                if (this._pageUI.SelectedSampleTemplate != null)
                {
                    HostingWizard[WizardPropertyNameConstants.propKey_SampleTemplateURL] = this._pageUI.SelectedSampleTemplate.URL;
                }

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating AWS CloudFormation Project: " + e.Message);
                return false;
            }
        }
    }
}
