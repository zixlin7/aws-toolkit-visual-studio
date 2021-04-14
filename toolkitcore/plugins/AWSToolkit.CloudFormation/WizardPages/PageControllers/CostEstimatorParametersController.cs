using System.Collections.Generic;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers
{
    internal class CostEstimatorParametersController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateParametersController));
        private readonly ToolkitContext _toolkitContext;

        public CostEstimatorParametersController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        CostEstimatorParameterPage _pageUI;

        public string PageID => GetType().FullName;

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageTitle => "Cost Estimator";

        public string ShortPageTitle => null;

        public string PageDescription => "These are parameters associated with your AWS CloudFormation template. You may review and proceed with the default parameters or make customizations as needed.";

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

                _pageUI = new CostEstimatorParameterPage(templateProps, _toolkitContext);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
                _pageUI.Connection.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);

                AccountViewModel account = null;
                ToolkitRegion region = null;

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
                        region = _toolkitContext.RegionProvider.GetRegion(lastRegionDeployedTo);
                }

                if (account == null)
                    account = ToolkitFactory.Instance.Navigator.SelectedAccount;
                if (region == null)
                    region = ToolkitFactory.Instance.Navigator.SelectedRegion;

                _pageUI.Connection.Account = account;
                _pageUI.Connection.Region = region;

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
            return this._pageUI.AllParametersValid &&
                   this._pageUI.Connection.ConnectionIsValid &&
                   !this._pageUI.Connection.IsValidating;
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
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] = this._pageUI.Connection.Account;
            HostingWizard[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] = this._pageUI.Connection.Region;

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
