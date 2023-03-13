using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using System;
using System.Collections.Generic;
using System.Threading;

using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    public abstract class BaseStackController : BaseContextCommand
    {
        private readonly BaseMetricSource _metricSource;

        protected BaseStackController(ToolkitContext toolkitContext, BaseMetricSource metricSource)
        {
            _toolkitContext = toolkitContext;
            _metricSource = metricSource;
        }

        protected ToolkitContext _toolkitContext { get; }

        public override ActionResults Execute(IViewModel model)
        {
            return new ActionResults().WithSuccess(true);
        }

        protected ActionResults UpdateStack(AccountViewModel account, ToolkitRegion region,
            Dictionary<string, object> collectedProperties)
        {
            var connectionSettings = new AwsConnectionSettings(account?.Identifier, region);
            ActionResults results = null;

            void Invoke() => results = Update(account, region, collectedProperties);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                RecordDeployMetric(results, connectionSettings, duration, initialDeploy: false);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
            return results;
        }

        private ActionResults Update(AccountViewModel account, ToolkitRegion region, Dictionary<string, object> collectedProperties)
        {
            try
            {
                var wrapper = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
                if (wrapper == null)
                {
                    return ActionResults.CreateFailed(new ToolkitException("Unable to find CloudFormation stack data", ToolkitException.CommonErrorCode.InternalMissingServiceState));
                }

                var cfClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var request = new UpdateStackRequest()
                {
                    StackName = collectedProperties[
                        DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                    Capabilities = new List<string>() {"CAPABILITY_IAM", "CAPABILITY_NAMED_IAM"}
                };

                if (collectedProperties.ContainsKey(DeploymentWizardProperties.DeploymentTemplate
                    .propkey_SelectedTemplateName))
                {
                    string templateName =
                        collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplateName]
                            as string;
                    request.TemplateURL = Util.CloudFormationUtil.UploadTemplateToS3(account, region,
                        wrapper.TemplateContent, templateName, request.StackName);
                }
                else
                {
                    request.TemplateBody = wrapper.TemplateContent;
                }

                if (collectedProperties.ContainsKey(CloudFormationDeploymentWizardProperties
                    .TemplateParametersProperties.propkey_TemplateParameterValues))
                {
                    var setParamterValues =
                        collectedProperties[
                                CloudFormationDeploymentWizardProperties.TemplateParametersProperties
                                    .propkey_TemplateParameterValues] as
                            Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>;
                    if (setParamterValues != null)
                    {
                        foreach (var kvp in setParamterValues)
                        {
                            var parameter = new Parameter()
                                {ParameterKey = kvp.Key, ParameterValue = kvp.Value.OverrideValue};
                            request.Parameters.Add(parameter);
                        }
                    }
                }

                if (wrapper.MustCreateWithChangeSets())
                {
                    _toolkitContext.ToolkitHost.UpdateStatus("Creating Change Set");
                    var ccRequest = new CreateChangeSetRequest
                    {
                        Capabilities = request.Capabilities,
                        ChangeSetType = ChangeSetType.UPDATE,
                        ChangeSetName = DateTime.Now.Ticks.ToString(),
                        NotificationARNs = request.NotificationARNs,
                        Parameters = request.Parameters,
                        ResourceTypes = request.ResourceTypes,
                        RoleARN = request.RoleARN,
                        StackName = request.StackName,
                        Tags = request.Tags,
                        TemplateBody = request.TemplateBody,
                        TemplateURL = request.TemplateURL
                    };
                    if (!ExecuteChangeSet(cfClient, ccRequest))
                    {
                        return ActionResults.CreateFailed(new CloudFormationToolkitException("Failed to execute change set request", CloudFormationToolkitException.CloudFormationErrorCode.ChangeSetFailed));
                    }
                }
                else
                {
                    _toolkitContext.ToolkitHost.UpdateStatus("Updating Stack");
                    cfClient.UpdateStack(request);
                }

                return new ActionResults().WithSuccess(true).WithFocalname(request.StackName)
                    .WithRunDefaultAction(true);
            }
            catch (Exception e)
            {
                _toolkitContext.ToolkitHost.ShowError("Error updating stack: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        protected ActionResults CreateStack(AccountViewModel account, ToolkitRegion region,
            Dictionary<string, object> collectedProperties)
        {
            var connectionSettings = new AwsConnectionSettings(account?.Identifier, region);
            ActionResults results = null;

            void Invoke() => results = Create(account, region, collectedProperties);

            void Record(ITelemetryLogger telemetryLogger, double duration)
            {
                RecordDeployMetric(results, connectionSettings, duration, initialDeploy: true);
            }

            _toolkitContext.TelemetryLogger.TimeAndRecord(Invoke, Record);
            return results;
        }


        private ActionResults Create(AccountViewModel account, ToolkitRegion region, Dictionary<string, object> collectedProperties)
        {
            try
            {
                var wrapper = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] as CloudFormationTemplateWrapper;
                if (wrapper == null)
                {
                    return ActionResults.CreateFailed(new ToolkitException("Unable to find CloudFormation stack data", ToolkitException.CommonErrorCode.InternalMissingServiceState));
                }

                var cfClient = account.CreateServiceClient<AmazonCloudFormationClient>(region);
                var request = new CreateStackRequest()
                {
                    StackName = collectedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                    Capabilities = new List<string>() { "CAPABILITY_IAM", "CAPABILITY_NAMED_IAM" }
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
                            var parameter = new Parameter() { ParameterKey = kvp.Key, ParameterValue = kvp.Value.OverrideValue ?? "" };
                            request.Parameters.Add(parameter);
                        }
                    }
                }

                if (wrapper.MustCreateWithChangeSets())
                {
                    _toolkitContext.ToolkitHost.UpdateStatus("Creating Change Set");
                    var ccRequest = new CreateChangeSetRequest
                    {
                        Capabilities = request.Capabilities,
                        ChangeSetType = ChangeSetType.CREATE,
                        ChangeSetName = DateTime.Now.Ticks.ToString(),
                        NotificationARNs = request.NotificationARNs,
                        Parameters = request.Parameters,
                        ResourceTypes = request.ResourceTypes,
                        RoleARN = request.RoleARN,
                        StackName = request.StackName,
                        Tags = request.Tags,
                        TemplateBody = request.TemplateBody,
                        TemplateURL = request.TemplateURL
                    };
                    if(!ExecuteChangeSet(cfClient, ccRequest))
                    {
                        return ActionResults.CreateFailed(new CloudFormationToolkitException("Failed to execute change set request", CloudFormationToolkitException.CloudFormationErrorCode.ChangeSetFailed));
                    }
                }
                else
                {
                    _toolkitContext.ToolkitHost.UpdateStatus("Creating Stack");
                    cfClient.CreateStack(request);
                }

                return new ActionResults().WithSuccess(true).WithFocalname(request.StackName).WithRunDefaultAction(true);
            }
            catch (Exception e)
            {
               _toolkitContext.ToolkitHost.ShowError("Error creating stack: " + e.Message);
                return ActionResults.CreateFailed(e);
            }
        }

        private bool ExecuteChangeSet(IAmazonCloudFormation cfClient, CreateChangeSetRequest changeSetRequest)
        {
            changeSetRequest.ChangeSetName = changeSetRequest.StackName + DateTime.Now.Ticks;

            var changeSetResponse = cfClient.CreateChangeSet(changeSetRequest);

            var request = new DescribeChangeSetRequest
            {
                ChangeSetName = changeSetResponse.Id
            };

            ToolkitFactory.Instance.ShellProvider.UpdateStatus($"... Waiting for change set to be reviewed");
            DescribeChangeSetResponse response;
            do
            {
                Thread.Sleep(1000);
                response = cfClient.DescribeChangeSet(request);
            } while (response.Status == ChangeSetStatus.CREATE_IN_PROGRESS || response.Status == ChangeSetStatus.CREATE_PENDING);

            if (response.Status == ChangeSetStatus.FAILED)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error", $"Failed to create CloudFormation change set: {response.StatusReason}");
                return false;
            }


            var executeChangeSetRequest = new ExecuteChangeSetRequest
            {
                StackName = changeSetRequest.StackName,
                ChangeSetName = changeSetResponse.Id
            };

            cfClient.ExecuteChangeSet(executeChangeSetRequest);
            if (changeSetRequest.ChangeSetType == ChangeSetType.CREATE)
                ToolkitFactory.Instance.ShellProvider.UpdateStatus($"Created CloudFormation stack {changeSetRequest.StackName}");
            else
                ToolkitFactory.Instance.ShellProvider.UpdateStatus($"Initiated CloudFormation stack update on {changeSetRequest.StackName}");

            return true;
        }

        internal static DeployedTemplateData GatherPersistableDeploymentData(AccountViewModel account, 
                                                                             ToolkitRegion region, 
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

        private void RecordDeployMetric(ActionResults result, AwsConnectionSettings connectionSettings, double duration,
            bool initialDeploy)
        {
            var data = result.CreateMetricData<CloudformationDeploy>(connectionSettings,
                _toolkitContext.ServiceClientManager);
            data.Result = result.AsTelemetryResult();
            data.InitialDeploy = initialDeploy;
            data.Duration = duration;
            data.Source = _metricSource.Location;
            data.ServiceType = _metricSource.Service;

            _toolkitContext.TelemetryLogger.RecordCloudformationDeploy(data);
        }
    }
}
