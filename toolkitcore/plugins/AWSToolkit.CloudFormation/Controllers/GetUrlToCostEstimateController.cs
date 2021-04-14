﻿using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CloudFormation.WizardPages.PageControllers;
using Amazon.AWSToolkit.Context;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public class GetUrlToCostEstimateController
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(GetUrlToCostEstimateController));
        private readonly ToolkitContext _toolkitContext;

        public GetUrlToCostEstimateController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public DeployedTemplateData Execute(string templateBody, IDictionary<string, object> seedProperties, string templateName)
        {
            try
            {
                DeployedTemplateData persistableData = null;

                var wrapper = CloudFormationTemplateWrapper.FromString(templateBody);
                wrapper.LoadAndParse();

                seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.CloudFormationServiceName;
                seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = wrapper;

                IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.CloudFormation.View.CostEstimator", seedProperties);
                wizard.Title = "Estimate Cost for Template";

                IAWSWizardPageController[] defaultPages = new IAWSWizardPageController[]
                {
                    new CostEstimatorParametersController(_toolkitContext)
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run() == true)
                {
                    var account = wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedAccount] as AccountViewModel;
                    var region = wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.SelectStackProperties.propkey_SelectedRegion] as ToolkitRegion;
                    

                    var client = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                    var request = new EstimateTemplateCostRequest();
                    request.TemplateURL = Util.CloudFormationUtil.UploadTemplateToS3(account, region, templateBody, templateName, "CostEstimator");

                    if (wizard.CollectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues))
                    {
                        var setParamterValues = wizard.CollectedProperties[CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues] as Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                        if (setParamterValues != null)
                        {
                            foreach (var kvp in setParamterValues)
                            {
                                var parameter = new Parameter() { ParameterKey = kvp.Key, ParameterValue = kvp.Value.OverrideValue };
                                request.Parameters.Add(parameter);
                            }
                        }
                    }

                    var response = client.EstimateTemplateCost(request);
                    persistableData = BaseStackController.GatherPersistableDeploymentData(account,
                                                                                          region,
                                                                                          DeployedTemplateData.DeploymentType.costEstimation,
                                                                                          wizard.CollectedProperties);
                    persistableData.CostEstimationCalculatorUrl = response.Url;
                }

                return persistableData;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Invalid Template", "Error parsing template: " + e.Message);
                LOGGER.Error("Error get url to cost estimate", e);
                return null;
            }
        }
    }
}
