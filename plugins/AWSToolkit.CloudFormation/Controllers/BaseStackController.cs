using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.CloudFormation.Nodes;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.WizardPages;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;


namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public abstract class BaseStackController : BaseContextCommand
    {
        public override ActionResults Execute(IViewModel model)
        {
            return new ActionResults().WithSuccess(true);
        }

        protected ActionResults UpdateStack(Account.AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, Dictionary<string, object> collectedProperties)
        {
            var wrapper = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (wrapper == null)
                return new ActionResults().WithSuccess(false);

            try
            {
                var cfClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var request = new UpdateStackRequest()
                {
                    StackName = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                    Capabilities = new List<string>() { "CAPABILITY_IAM" }
                };
                
                if (collectedProperties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName))
                {
                    string templateName = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] as string;
                    request.TemplateURL = Util.CloudFormationUtil.UploadTemplateToS3(account, region, wrapper.TemplateContent, templateName, request.StackName);
                }
                else
                {
                    request.TemplateBody = wrapper.TemplateContent;
                }

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues))
                {
                    var setParamterValues = collectedProperties[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                    if (setParamterValues != null)
                    {
                        foreach (var kvp in setParamterValues)
                        {
                            var parameter = new Parameter() { ParameterKey = kvp.Key, ParameterValue = kvp.Value.OverrideValue };
                            request.Parameters.Add(parameter);
                        }
                    }
                }

                ToolkitFactory.Instance.ShellProvider.UpdateStatus("Updating Stack");
                cfClient.UpdateStack(request);

                return new ActionResults().WithSuccess(true).WithFocalname(request.StackName).WithRunDefaultAction(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating stack: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }

        protected ActionResults CreateStack(Account.AccountViewModel account, RegionEndPointsManager.RegionEndPoints region, Dictionary<string, object> collectedProperties)
        {
            var wrapper = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
            if (wrapper == null)
                return new ActionResults().WithSuccess(false);

            try
            {
                var cfClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var request = new CreateStackRequest()
                {
                    StackName = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                    Capabilities = new List<string>() { "CAPABILITY_IAM" }
                };

                if (collectedProperties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName))
                {
                    string templateName = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] as string;
                    request.TemplateURL = Util.CloudFormationUtil.UploadTemplateToS3(account, region, wrapper.TemplateContent, templateName, request.StackName);
                }
                else
                {
                    request.TemplateBody = wrapper.TemplateContent;
                }

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic))
                    request.NotificationARNs.Add(collectedProperties[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic] as string);

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout))
                {
                    int timeout = (int)collectedProperties[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout];
                    if (timeout > 0)
                        request.TimeoutInMinutes = timeout;
                }

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure))
                {
                    request.DisableRollback = !(bool)collectedProperties[CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure];
                }

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues))
                {
                    var setParamterValues = collectedProperties[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                    if (setParamterValues != null)
                    {
                        foreach (var kvp in setParamterValues)
                        {
                            var parameter = new Parameter() { ParameterKey = kvp.Key, ParameterValue = kvp.Value.OverrideValue };
                            request.Parameters.Add(parameter);
                        }
                    }
                }

                ToolkitFactory.Instance.ShellProvider.UpdateStatus("Creating Stack");
                cfClient.CreateStack(request);

                return new ActionResults().WithSuccess(true).WithFocalname(request.StackName).WithRunDefaultAction(true);
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating stack: " + e.Message);
                return new ActionResults().WithSuccess(false);
            }
        }

        internal static DeployedTemplateData GatherPersistableDeploymentData(AccountViewModel account, 
                                                                             RegionEndPointsManager.RegionEndPoints region, 
                                                                             DeployedTemplateData.DeploymentType deploymentType, 
                                                                             IDictionary<string, object> properties)
        {
            var persistableData = new DeployedTemplateData();
            persistableData.Account = account;
            persistableData.Region = region;

            if (properties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName))
                persistableData.StackName = properties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;

            if (properties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName))
                persistableData.TemplateUri = properties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName] as string;

            persistableData.DeploymentOperation = deploymentType;
            switch (deploymentType)
            {
                case DeployedTemplateData.DeploymentType.newStack:
                case DeployedTemplateData.DeploymentType.costEstimation:
                    {
                        if (properties.ContainsKey(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues))
                        {
                            persistableData.TemplateProperties = new Dictionary<string, object>();
                            var setParameterValues
                                = properties[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] 
                                    as IDictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                            if (setParameterValues != null)
                            {
                                foreach (var k in setParameterValues.Keys)
                                {
                                    var parameter = setParameterValues[k];
                                    if (!parameter.Hidden && !parameter.NoEcho)
                                        persistableData.TemplateProperties.Add(k, parameter.OverrideValue);
                                }
                            }
                        }
                    }
                    break;

                case DeployedTemplateData.DeploymentType.updateStack:
                    break;
            }

            return persistableData;
        }
    }
}
