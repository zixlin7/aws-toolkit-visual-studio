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
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;

using log4net;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    public class SelectStackPageController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(SelectTemplateController));


        SelectStackPage _pageUI;

        #region IAWSWizardPageController Members

        public SelectStackPageController()
        {
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
            get { return "Select Template"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "To create a stack, fill in the name for your stack and select a template. You may choose one of the sample templates to get started quickly or on your local hard drive."; }
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
                _pageUI = new SelectStackPage(this);
                this._pageUI.PropertyChanged += new PropertyChangedEventHandler(_pageUI_PropertyChanged);
                this._pageUI.DataContext = this;

                AccountViewModel account = null;
                RegionEndPointsManager.RegionEndPoints region = null;

                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid))
                {
                    string accountGuidKey = HostingWizard[DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid] as string;
                    account = ToolkitFactory.Instance.RootViewModel.AccountFromIdentityKey(accountGuidKey);
                }

                string lastRegionDeployedTo = string.Empty;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo))
                {
                    lastRegionDeployedTo = HostingWizard[DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo] as string;
                    if (!string.IsNullOrEmpty(lastRegionDeployedTo))
                        region = RegionEndPointsManager.GetInstance().GetRegion(lastRegionDeployedTo);
                }

                if (account == null)
                    account = ToolkitFactory.Instance.Navigator.SelectedAccount;
                if (region == null)
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
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
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
                if (HostingWizard.GetProperty(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount) == null)
                    return false;
                if (HostingWizard.GetProperty(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion) == null)
                    return false;
                if (HostingWizard.GetProperty(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode) == null)
                    return false;
                if (!validateStackName())
                    return false;
                return true;
            }
        }

        bool validateStackName()
        {
            var name = HostingWizard.GetProperty<string>(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);
            return SelectTemplateModel.IsValidStackName(name);
        }

        bool StorePageData()
        {
            try
            {

//                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] = this._model.SNSTopic;

                HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] = this._pageUI.SelectedAccount;
                HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] = this._pageUI.SelectedRegion;
                HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] = this._pageUI.StackName;
                HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode] = this._pageUI.CreateStackMode;

                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout] = this._pageUI.CreationTimeout;
                HostingWizard[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure] = this._pageUI.RollbackOnFailure;

                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error loading template: " + e.Message);
                return false;
            }
        }
    }
}
