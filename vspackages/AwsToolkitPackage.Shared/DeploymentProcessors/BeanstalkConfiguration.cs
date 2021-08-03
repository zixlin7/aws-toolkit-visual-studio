using System;
using System.Collections.Generic;
using System.IO;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.ElasticBeanstalk;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.VisualStudio.DeploymentProcessors
{
    public class BeanstalkConfiguration
    {
        private readonly IDictionary<string, object> _configuration;

        private BeanstalkConfiguration(IDictionary<string, object> configuration)
        {
            this._configuration = configuration;
        }

        public static BeanstalkConfiguration CreateOrGetFrom(string filePath)
        {
            return File.Exists(filePath) ? LoadConfigurationFromFile(filePath) : CreateDefault();
        }

        private static BeanstalkConfiguration LoadConfigurationFromFile(string filePath)
        {
            var json = File.ReadAllText(filePath);
            var configuration = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            return new BeanstalkConfiguration(configuration);
        }

        public static BeanstalkConfiguration CreateDefault()
        {
            var configuration = new Dictionary<string, object>();
            configuration["comment"] = "This file is used to help set default values when using the dotnet CLI extension Amazon.ElasticBeanstalk.Tools. For more information run \"dotnet eb --help\" from the project root.";
            return new BeanstalkConfiguration(configuration);
        }

        public void UpdateConfigurationWith(DeploymentTaskInfo taskInfo)
        {
            _configuration["profile"] = CommonWizardProperties.AccountSelection.GetSelectedAccount(taskInfo.Options)?.Identifier?.ProfileName;
            _configuration["region"] = CommonWizardProperties.AccountSelection.GetSelectedRegion(taskInfo.Options).Id;

            Action<string, string> copyValue = (jsonKey, wizardOption) =>
            {
                if (taskInfo.Options.ContainsKey(wizardOption))
                    _configuration[jsonKey] = taskInfo.Options[wizardOption] as string;
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
                    _configuration["app-path"] = "/" + fullIisAppPath;
                }
                else
                {
                    _configuration["iis-website"] = fullIisAppPath.Substring(0, pos);
                    _configuration["app-path"] = fullIisAppPath.Substring(pos);
                }
            }

            if (taskInfo.Options.ContainsKey(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon) &&
                taskInfo.Options[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon] is bool)
            {
                _configuration["enable-xray"] = (bool) taskInfo.Options[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_EnableXRayDaemon];
            }
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(_configuration, Formatting.Indented);
        }
    }
}
