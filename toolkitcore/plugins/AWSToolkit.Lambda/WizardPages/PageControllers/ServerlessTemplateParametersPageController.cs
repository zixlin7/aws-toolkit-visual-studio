using Amazon.AWSToolkit.CommonUI.WizardFramework;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;

using Amazon.AWSToolkit.CloudFormation.WizardPages.PageUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Account;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.Lambda.WizardPages.PageControllers
{
    public class ServerlessTemplateParametersPageController : IAWSWizardPageController
    {
        ILog LOGGER = LogManager.GetLogger(typeof(ServerlessTemplateParametersPageController));

        private TemplateParametersPage _pageUI;

        public IAWSWizard HostingWizard { get; set; }

        public string PageDescription => "These are parameters associated with your AWS CloudFormation template. You may review and proceed with the default parameters or make customizations as needed.";

        public string PageGroup => AWSWizardConstants.DefaultPageGroup;

        public string PageID => GetType().FullName;

        public string PageTitle => "Edit Template Parameters";

        public string ShortPageTitle => null;

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {

            var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;
            if (wrapper == null || !wrapper.ContainsUserVisibleParameters)
            {
                return false;
            }

            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            bool pageSeeding = _pageUI == null;
            if (_pageUI == null)
            {
                IDictionary<string, object> templateProps = null;
                if (HostingWizard.IsPropertySet(UploadFunctionWizardProperties.CloudFormationParameters))
                {
                    if (HostingWizard[UploadFunctionWizardProperties.CloudFormationParameters] is IDictionary<string, string>)
                    {
                        templateProps = new Dictionary<string, object>();
                        var originalValues = HostingWizard[UploadFunctionWizardProperties.CloudFormationParameters] as IDictionary<string, string>;
                        foreach(var kvp in originalValues)
                        {
                            templateProps[kvp.Key] = kvp.Value;
                        }
                    }
                    else
                    {
                        templateProps = HostingWizard[UploadFunctionWizardProperties.CloudFormationParameters] as IDictionary<string, object>;
                    }
                }

                bool disableLoadPrevious = true;
                //if (HostingWizard.IsPropertySet(CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propKey_DisableLoadPreviousValues))
                //    disableLoadPrevious = HostingWizard.GetProperty<bool>(CloudFormationDeploymentWizardProperties.SelectTemplateProperties.propKey_DisableLoadPreviousValues);

                _pageUI = new TemplateParametersPage(templateProps, disableLoadPrevious);
                _pageUI.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(_pageUI_PropertyChanged);

            }

            LoadExistingStackParameters();

            var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;

            var setValues = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters]
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
            // set Next to false to try and indicate this is the last page, and the Upload page
            // will start processing
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, IsForwardsNavigationAllowed);
        }

        public bool AllowShortCircuit()
        {
            return IsForwardsNavigationAllowed;
        }


        void LoadExistingStackParameters()
        {
            try
            {
                bool createStack = HostingWizard.GetProperty<bool>(UploadFunctionWizardProperties.IsNewStack);
                if (createStack)
                    return;

                var accountModel = HostingWizard.GetSelectedAccount(UploadFunctionWizardProperties.UserAccount);
                var region = HostingWizard.GetSelectedRegion(UploadFunctionWizardProperties.Region);

                var stackName = HostingWizard.GetProperty<string>(UploadFunctionWizardProperties.StackName);

                var client = accountModel.CreateServiceClient<AmazonCloudFormationClient>(region);
                var response = client.DescribeStacks(new DescribeStacksRequest() { StackName = stackName });

                if (response.Stacks.Count != 1)
                    return;

                var stack = response.Stacks[0];
                var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;

                bool newParameters = false;
                var setValues = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters]
                                    as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                if (setValues == null)
                {
                    newParameters = true;
                    setValues = new Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>();
                    HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters] = setValues;
                }


                foreach (var parameter in wrapper.Parameters)
                {
                    if (!string.IsNullOrEmpty(parameter.Value.OverrideValue) && !newParameters)
                        continue;

                    var existingParameter = stack.Parameters.FirstOrDefault(x => x.ParameterKey == parameter.Key);

                    if (existingParameter != null && !string.Equals(existingParameter.ParameterValue, Amazon.AWSToolkit.CloudFormation.CloudFormationConstants.NO_ECHO_VALUE_RETURN_FROM_CONSOLE))
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

        void StorePageData()
        {
            var setValues = new Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>();
            var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;
            if (wrapper.Parameters != null)
            {
                foreach (var kvp in wrapper.Parameters)
                {
                    setValues[kvp.Key] = kvp.Value;
                }
            }

            HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateParameters] = setValues;
        }

        public bool IsForwardsNavigationAllowed
        {
            get
            {
                var wrapper = HostingWizard[UploadFunctionWizardProperties.CloudFormationTemplateWrapper] as CloudFormationTemplateWrapper;
                if (wrapper == null || wrapper.Parameters == null)
                    return true;

                if (wrapper.Parameters.Count == 0)
                    return true;

                if (this._pageUI != null && !this._pageUI.AllParametersValid)
                    return false;

                return true;
            }
        }

    }
}
