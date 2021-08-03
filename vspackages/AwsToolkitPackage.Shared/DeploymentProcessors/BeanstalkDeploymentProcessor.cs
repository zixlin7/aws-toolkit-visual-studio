using System;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using System.IO;

namespace Amazon.AWSToolkit.VisualStudio.DeploymentProcessors
{
    /// <summary>
    /// 'Standard' deployment processor, routing the deployment package
    /// through S3 and onwards to Beanstalk.
    /// </summary>
    public class BeanstalkDeploymentProcessor : IDeploymentProcessor
    {
        bool _deploymentResult;

        #region IDeploymentProcessor

        void IDeploymentProcessor.DeployPackage(DeploymentTaskInfo taskInfo)
        {
            try
            {
                var beanstalkPlugin = taskInfo.ServicePlugin as IAWSElasticBeanstalk;
                // the deployment package will be a zip file or folder reference depending on incremental mode -
                // the location of the repository to push to is contained in the Options dictionary and doesn't
                // concern us at this level
                _deploymentResult = beanstalkPlugin.DeploymentService.Deploy(taskInfo.DeploymentPackage, taskInfo.Options);

                if (taskInfo.Options.ContainsKey(DeploymentWizardProperties.SeedData.propkey_ProjectType) &&
                    taskInfo.Options.ContainsKey(DeploymentWizardProperties.ReviewProperties.propkey_SaveBeanstalkTools))
                {
                    var projectType = taskInfo.Options[DeploymentWizardProperties.SeedData.propkey_ProjectType] as string;
                    var saveBeanstalkTools = (bool)taskInfo.Options[DeploymentWizardProperties.ReviewProperties.propkey_SaveBeanstalkTools];
                    if (string.Equals(projectType, DeploymentWizardProperties.NetCoreWebProject, StringComparison.OrdinalIgnoreCase) && saveBeanstalkTools)
                    {
                        ConfigureBeanstalkTools(taskInfo);
                    }
                }
            }
            catch (Exception exc)
            {
                taskInfo.Logger.OutputMessage(string.Format("Caught exception during handoff process to Elastic Beanstalk, deployment failed - {0}", exc.Message));
            }
            finally
            {
                taskInfo.CompletionSignalEvent.Set();
            }
        }

        bool IDeploymentProcessor.Result => _deploymentResult;

        #endregion

        private void ConfigureBeanstalkTools(DeploymentTaskInfo taskInfo)
        {
            string defaultsFilePath = Path.Combine(taskInfo.ProjectInfo.VsProjectLocation, "aws-beanstalk-tools-defaults.json");
            var json = GetBeanstalkConfiguration(taskInfo, defaultsFilePath);
            File.WriteAllText(defaultsFilePath, json);
            Utility.AddDotnetCliToolReference(taskInfo.ProjectInfo.VsProjectLocationAndName, "Amazon.ElasticBeanstalk.Tools");
        }

        private string GetBeanstalkConfiguration(DeploymentTaskInfo taskInfo, string defaultsFilePath)
        {
            var configuration = BeanstalkConfiguration.CreateOrGetFrom(defaultsFilePath);
            configuration.UpdateConfigurationWith(taskInfo);
            return configuration.ToJson();
        }
    }
}
