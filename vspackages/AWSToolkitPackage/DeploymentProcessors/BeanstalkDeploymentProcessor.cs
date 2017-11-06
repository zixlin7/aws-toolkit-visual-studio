﻿using System;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using System.IO;
using TemplateWizard.ThirdParty.Json.LitJson;
using System.Text;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.VisualStudio.DeploymentProcessors
{
    /// <summary>
    /// 'Standard' deployment processor, routing the deployment package
    /// through S3 and onwards to Beanstalk.
    /// </summary>
    internal class BeanstalkDeploymentProcessor : IDeploymentProcessor
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

        bool IDeploymentProcessor.Result
        {
            get { return _deploymentResult; }
        }

        #endregion

        private void ConfigureBeanstalkTools(DeploymentTaskInfo taskInfo)
        {
            JsonData data;
            var defaultsFilePath = Path.Combine(taskInfo.ProjectInfo.VsProjectLocation, "aws-beanstalk-tools-defaults.json");
            if(File.Exists(defaultsFilePath))
            {
                data = JsonMapper.ToObject(File.ReadAllText(defaultsFilePath));
            }
            else
            {
                data = new JsonData();
            }

            data["profile"] = (taskInfo.Options[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel).DisplayName;
            data["region"] = (taskInfo.Options[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints).SystemName;
            data["application"] = taskInfo.Options[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string;
            data["environment"] = taskInfo.Options[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string;

            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            JsonMapper.ToJson(data, writer);

            var json = sb.ToString();
            File.WriteAllText(defaultsFilePath, json);


            Utility.AddDotnetCliToolReference(taskInfo.ProjectInfo.VsProjectLocationAndName, "Amazon.ElasticBeanstalk.Tools", "0.8.0");

        }
    }
}
