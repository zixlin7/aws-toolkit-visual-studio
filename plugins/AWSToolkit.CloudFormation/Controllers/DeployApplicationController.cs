using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CloudFormation.Model;
using Amazon.AWSToolkit.CloudFormation.View.Components;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.Notifications;

using log4net;

namespace Amazon.AWSToolkit.CloudFormation.Controllers
{
    class DeployApplicationController : DeploymentControllerBase
    {
        public DeployApplicationController(string deploymentPackage, IDictionary<string, object> deploymentProperties)
            : base(deploymentPackage, deploymentProperties)
        {
            LOGGER = LogManager.GetLogger(typeof(DeployApplicationController));
            Observer = new DeploymentControllerBaseObserver(LOGGER);
            Deployment.Observer = Observer;
            Deployment.AWSAccessKey = _account.AccessKey;
            Deployment.AWSSecretKey = _account.SecretKey;   
        }

        public override void Execute()
        {
            string stackID = null;

            try
            {
                if (_account == null)
                    return;

                ToolkitFactory.Instance.Navigator.UpdateAccountSelection(new Guid(_account.SettingsUniqueKey), false);

                Deployment.StackName = getValue<string>(DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);
                Deployment.UploadBucket = DefaultBucketName(_account, Deployment.Region);
//                Deployment.ConfigFileKey = DefaultConfigFileKey(Deployment.StackName);

                var wrapper = getValue<CloudFormationTemplateWrapper>(DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate);
                Deployment.Template = wrapper.TemplateContent;
                Deployment.TemplateFilename = wrapper.TemplateFilename;

                Deployment.Settings.SNSTopic = getValue<string>(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_SNSTopic);
                int timeout = getValue<int>(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_CreationTimeout);
                if (timeout > 0)
                    Deployment.Settings.CreationTimeout = timeout;

                Deployment.Settings.RollbackOnFailure = getValue<bool>(CloudFormationDeploymentWizardProperties.AWSOptionsProperties.propkey_RollbackOnFailure);

                CopyContainerProperties();
                CopyApplicationOptionProperties();

                // add the parameters that the user could edit via the Template Parameters page; if there were no user-visible properties
                // this data may not exist
                var setParameterValues 
                    = getValue<Dictionary<string, CloudFormationTemplateWrapper.TemplateParameter>>(CloudFormationDeploymentWizardProperties.TemplateParametersProperties.propkey_TemplateParameterValues);
                if (setParameterValues != null)
                {
                    foreach (var kvp in setParameterValues)
                    {
                        if (!kvp.Value.Hidden)
                        {
                            Deployment.TemplateParameters.Add(kvp.Key, kvp.Value.OverrideValue);
                        }
                    }
                }

                // finally add in the 'hidden' parameters that were exposed as fields on various wizard pages
                string ami = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_CustomAMIID);
                if (!string.IsNullOrEmpty(ami))
                    Deployment.TemplateParameters["AmazonMachineImage"] = ami;

                Deployment.TemplateParameters["InstanceType"] = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID);

                Deployment.KeyPairName = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_KeyPairName);
                if (getValue<bool>(DeploymentWizardProperties.AWSOptions.propkey_CreateKeyPair))
                    CreateKeyPair(_account);

                string groupName = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_SecurityGroupName);
                if (!string.IsNullOrEmpty(groupName))
                {
                    Deployment.TemplateParameters["SecurityGroup"] = groupName;
                    if (getValue<bool>(DeploymentWizardProperties.AWSOptions.propkey_AutoOpenPort80))
                    {
                        // don't have access to Toolkit's EC2 NetworkProtocol at this stage
                        OpenIngressForGroup(groupName, "0.0.0.0/0", "tcp", 80);
                    }
                }

                string configFileDestination = getValue<string>(DeploymentWizardProperties.ReviewProperties.propkey_ConfigFileDestination);
                Deployment.ConfigFileDestination = configFileDestination;

                Deployment.Deploy();
                stackID = Deployment.StackId;
            }
            catch (Exception e)
            {
                string errMsg = string.Format("Error publishing application: {0}", e.Message);
                Observer.Error(errMsg);
                ToolkitFactory.Instance.ShellProvider.ShowError("Publish Error", errMsg);
            }
            finally
            {
                if (!string.IsNullOrEmpty(stackID))
                {
                    Observer.Status("Publish to AWS CloudFormation stack '{0}' completed successfully", Deployment.StackName);
                    DeploymentTaskNotifier notifier = new DeploymentTaskNotifier();
                    notifier.CloudFormationClient = this.CloudFormationClient;
                    notifier.StackName = Deployment.StackName;
                    TaskWatcher.WatchAndNotify(TaskWatcher.DefaultPollInterval, notifier, notifier);
                    // nicer to not switch navigator context if deployment failed!
                    try
                    {
                        ToolkitFactory.Instance.Navigator.UpdateRegionSelection(RegionEndPoints);
                        SelectNewTreeItems(_account);
                    }
                    catch (Exception e)
                    {
                        LOGGER.Error("Error selecting new tree items", e);
                    }
                }
                else
                    Observer.Status("Publish to AWS CloudFormation stack '{0}' did not complete successfully", Deployment.StackName);
            }
        }
    }

    /// <summary>
    /// Internal class wraps checking for deployment completion and eventual notification if
    /// it succeeds
    /// </summary>
    internal class DeploymentTaskNotifier : TaskWatcher.IQueryTaskCompletionProxy, TaskWatcher.INotifyTaskCompletionProxy
    {
        public IAmazonCloudFormation CloudFormationClient { get; set; }
        public string StackName { get; set; }

        #region IQueryTaskCompletionProxy Members

        public TaskWatcher.TaskCompletionState QueryTaskCompletion(TaskWatcher callingNotifier)
        {
            TaskWatcher.TaskCompletionState completionState = TaskWatcher.TaskCompletionState.pending;

            try
            {
                var response = CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = StackName });
                if (response.Stacks[0].StackStatus == StackStatus.CREATE_COMPLETE)
                    completionState = TaskWatcher.TaskCompletionState.completed;
                else
                    if (response.Stacks[0].StackStatus.Value.StartsWith("ERROR_", StringComparison.InvariantCultureIgnoreCase))
                        completionState = TaskWatcher.TaskCompletionState.error;
            }
            catch (Exception)
            {
            }

            return completionState;
        }

        #endregion

        #region INotifyTaskCompletionProxy Members

        public void NotifyTaskCompletion(TaskWatcher callingNotifier)
        {
            bool success = callingNotifier.WatchingState == TaskWatcher.WatcherState.completedOK;
            string url = string.Empty;
            if (success)
            {
                try
                {
                    var response = CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = StackName });
                    foreach (Output output in response.Stacks[0].Outputs)
                    {
                        if (output.OutputKey == "URL")
                        {
                            url = output.OutputValue;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                }
            }

            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    AWSNotificationToaster toaster = new AWSNotificationToaster();
                    DeploymentNotificationPanel panel = new DeploymentNotificationPanel();
                    panel.SetPanelContent(StackName, url, success);
                    toaster.ShowNotification(panel, "AWS CloudFormation");
                }));
        }

        #endregion
    }
}
