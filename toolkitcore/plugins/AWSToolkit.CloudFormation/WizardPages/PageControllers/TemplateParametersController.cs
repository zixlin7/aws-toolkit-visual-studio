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
    internal class TemplateParametersController : IAWSWizardPageController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(TemplateParametersController));

        TemplateParametersPage _pageUI;

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
            get { return "Edit Template Parameters"; }
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
            // used in several CloudFormation deployment wizards, so only reject if Beanstalk is explicitly set
            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner))
            {
                if (!IsWizardInCloudFormationMode)
                    return false;
            }

            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
            {
                if ((bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy])
                    return false;
            }

            var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (!wrapper.ContainsUserVisibleParameters)
            {
                // if the user has rolled back and forth through the wizard selecting different templates but
                // never deploying, guard against a build up of non-relevant data. Unfortunately this will 
                // mean users have to re-enter data on repeated template switching...
                HostingWizard.SetProperty(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues, null); 
                return false;
            }

            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
                return true;

            if (HostingWizard.IsPropertySet(DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy))
                return !((bool)HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy]);

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            bool pageSeeding = _pageUI == null;
            if (_pageUI == null)
            {
                IDictionary<string, object> templateProps = null;
                if(HostingWizard.IsPropertySet(DeploymentWizardProperties.SeedData.propkey_TemplateProperties))
                    templateProps = HostingWizard[DeploymentWizardProperties.SeedData.propkey_TemplateProperties] as IDictionary<string, object>;

                bool disableLoadPrevious = false;
                if (HostingWizard.IsPropertySet(CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propKey_DisableLoadPreviousValues))
                    disableLoadPrevious = HostingWizard.GetProperty<bool>(CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propKey_DisableLoadPreviousValues);

                _pageUI = new TemplateParametersPage(templateProps, disableLoadPrevious);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);
            }

            LoadExistingStackParameters();

            bool createStack = HostingWizard.GetProperty<bool>(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode);
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

        void LoadExistingStackParameters()
        {
            try
            {
                bool createStack = HostingWizard.GetProperty<bool>(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_CreateStackMode);
                if (createStack)
                    return;

                var accountModel = HostingWizard.GetProperty<AccountViewModel>(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount);
                var region = HostingWizard.GetProperty<RegionEndPointsManager.RegionEndPoints>(CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion);

                var stackName = HostingWizard.GetProperty<string>(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);

                var client = accountModel.CreateServiceClient<AmazonCloudFormationClient>(region);
                var response = client.DescribeStacks(new DescribeStacksRequest() { StackName = stackName });

                if (response.Stacks.Count != 1)
                    return;

                var stack = response.Stacks[0];

                var setValues = HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues]
                                    as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                if (setValues == null)
                {
                    setValues = new Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>();
                    HostingWizard[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] = setValues;
                }

                var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;

                foreach (var parameter in wrapper.Parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.Value.OverrideValue))
                        continue;

                    var existingParameter = stack.Parameters.FirstOrDefault(x => x.ParameterKey == parameter.Key);

                    if (existingParameter != null && !string.Equals(existingParameter.ParameterValue, CloudFormationConstants.NO_ECHO_VALUE_RETURN_FROM_CONSOLE))
                    {
                        parameter.Value.OverrideValue = existingParameter.ParameterValue;
                        setValues[parameter.Key] = parameter.Value;
                    }
                }
            }
            catch (Exception e)
            {
                LOGGER.Info("Load parameters from existing", e);
            }
        }


        bool IsWizardInCloudFormationMode
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                return service == DeploymentServiceIdentifiers.CloudFormationServiceName;
            }
        }

        void StorePageData()
        {
            if (!IsWizardInCloudFormationMode)
                return;

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

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                string service = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                if (service != DeploymentServiceIdentifiers.CloudFormationServiceName)
                    return true;

                var wrapper = HostingWizard[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
                if (wrapper == null || wrapper.Parameters == null)
                    return false;

                if (wrapper.Parameters.Count == 0)
                    return true;

                if (this._pageUI != null && !this._pageUI.AllParametersValid)
                    return false;

                return true;
            }
        }

    }
}
