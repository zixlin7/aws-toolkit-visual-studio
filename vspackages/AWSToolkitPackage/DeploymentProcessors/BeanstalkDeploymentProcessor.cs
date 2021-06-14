using System;
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
            var data = GetDefaultBeanstalkConfiguration(taskInfo, defaultsFilePath);
            return GetBeanstalkConfigurationFromTaskInfo(taskInfo, data);
        }

        private JsonData GetDefaultBeanstalkConfiguration(DeploymentTaskInfo taskInfo, string defaultsFilePath)
        {
            JsonData data;
            
            if (File.Exists(defaultsFilePath))
            {
                data = JsonMapper.ToObject(File.ReadAllText(defaultsFilePath));
            }
            else
            {
                data = new JsonData();
                data["comment"] = "This file is used to help set default values when using the dotnet CLI extension Amazon.ElasticBeanstalk.Tools. For more information run \"dotnet eb --help\" from the project root.";
            }

            return data;
        }

        public string GetBeanstalkConfigurationFromTaskInfo(DeploymentTaskInfo taskInfo)
        {
            return GetBeanstalkConfigurationFromTaskInfo(taskInfo, new JsonData());
        }

        private string GetBeanstalkConfigurationFromTaskInfo(DeploymentTaskInfo taskInfo, JsonData data)
        {
            data["profile"] = CommonWizardProperties.AccountSelection.GetSelectedAccount(taskInfo.Options)?.Identifier?.ProfileName;
            data["region"] = CommonWizardProperties.AccountSelection.GetSelectedRegion(taskInfo.Options).Id;

            Action<string, string> copyValue = (jsonKey, wizardOption) =>
            {
                if (taskInfo.Options.ContainsKey(wizardOption))
                    data[jsonKey] = taskInfo.Options[wizardOption] as string;
            };

            copyValue("application", DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName);
            copyValue("environment", BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName);
            copyValue("cname", BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_CName);
            copyValue("solution-stack", BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_SolutionStack);
            copyValue("environment-type", BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType);
            copyValue("instance-profile", BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_InstanceProfileName);
            copyValue("service-role", BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_ServiceRoleName);
            copyValue("health-check-url", DeploymentWizardProperties.AppOptions.propkey_HealthCheckUrl);
            copyValue("instance-type", DeploymentWizardProperties.AWSOptions.propkey_InstanceTypeID);
            copyValue("key-pair", DeploymentWizardProperties.AWSOptions.propkey_KeyPairName);

            if (taskInfo.Options.ContainsKey(DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath))
            {
                var fullIisAppPath = taskInfo.Options[DeploymentWizardProperties.AppOptions.propkey_DeployIisAppPath] as string;

                int pos = fullIisAppPath.IndexOf('/');
                if (pos == -1)
                {
                    data["app-path"] = "/" + fullIisAppPath;
                }
                else
                {
                    data["iis-website"] = fullIisAppPath.Substring(0, pos);
                    data["app-path"] = fullIisAppPath.Substring(pos);
                }
            }

            if (taskInfo.Options.ContainsKey(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon) &&
                taskInfo.Options[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon] is bool)
            {
                data["enable-xray"] = (bool) taskInfo.Options[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon];
            }

            return ConvertJSONToString(data);
        }

        private string ConvertJSONToString(JsonData data)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);
            writer.PrettyPrint = true;
            JsonMapper.ToJson(data, writer);

            return sb.ToString();
        }
    }
}
