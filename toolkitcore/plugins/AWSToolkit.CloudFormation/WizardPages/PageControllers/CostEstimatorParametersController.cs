using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;
using Amazon.AWSToolkit.PluginServices.Deployment;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class CostEstimatorParametersController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateParametersController));

        CostEstimatorParameterPage _pageUI;

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
            get { return "Cost Estimator"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "These are parameters associated with your AWS CloudFormation template. You may review and proceed with the default parameters or make customizations as needed."; }
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
            bool pageSeeding = _pageUI == null;
            if (_pageUI == null)
            {
                IDictionary<string, object> templateProps = null;
                if (HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_TemplateProperties))
                    templateProps = HostingWizard[DeploymentWizardProperties.SeedData.propkey_TemplateProperties] as IDictionary<string, object>;

                _pageUI = new CostEstimatorParameterPage(templateProps);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);

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
                        region = RegionEndPointsManager.Instance.GetRegion(lastRegionDeployedTo);
                }

                if (account == null)
                    account = ToolkitFactory.Instance.Navigator.SelectedAccount;
                if (region == null)
                    region = ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints;

                this._pageUI.Initialize(account, region);

                var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;

                var setValues = HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues]
                                        as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                foreach (var parameter in wrapper.Parameters)
                {
                    if (setValues != null && setValues.ContainsKey(parameter.Key))
                    {
                        parameter.Value.OverrideValue = setValues[parameter.Key].OverrideValue;
                    }

                    parameter.Value.PropertyChanged += onTemplateParameterPropertyChanged;
                }

                this._pageUI.BuildParameters(wrapper);
            }

            return _pageUI;
        }

        void _pageUI_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        void onTemplateParameterPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            TestForwardTransitionEnablement();
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return this._pageUI.AllParametersValid && this._pageUI.SelectedAccount != null && this._pageUI.SelectedRegion != null;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, QueryFinishButtonEnablement());
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        void StorePageData()
        {
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] = this._pageUI.SelectedAccount;
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] = this._pageUI.SelectedRegion;

            var setValues = new Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>();
            var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (wrapper.Parameters != null)
            {
                foreach (var kvp in wrapper.Parameters)
                {
                    setValues[kvp.Key] = kvp.Value;
                }
            }

            HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] = setValues;
        }
    }
}
