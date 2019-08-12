using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.ElasticBeanstalk;
using Amazon.IdentityManagement;
using Amazon.EC2;
using Amazon.RDS;
using Amazon.ElasticBeanstalk.Model;

using log4net;
namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages
{
    public static class DeploymentWizardHelper
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(DeploymentWizardHelper));
        public static IAmazonElasticBeanstalk GetBeanstalkClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var endpoint = region.GetEndpoint(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME);
            var beanstalkConfig = new AmazonElasticBeanstalkConfig();
            endpoint.ApplyToClientConfig(beanstalkConfig);
            return new AmazonElasticBeanstalkClient(accountViewModel.Credentials, beanstalkConfig);
        }

        public static IAmazonIdentityManagementService GetIAMClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var config = new AmazonIdentityManagementServiceConfig();
            var endpoint = region.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME);
            endpoint.ApplyToClientConfig(config);
            return new AmazonIdentityManagementServiceClient(accountViewModel.Credentials, config);
        }

        public static IAmazonEC2 GetEC2Client(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var ec2Config = new AmazonEC2Config();
            var endpoint = region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            endpoint.ApplyToClientConfig(ec2Config);
            return new AmazonEC2Client(accountViewModel.Credentials, ec2Config);
        }

        public static IAmazonRDS GetRDSClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var rdsConfig = new AmazonRDSConfig();
            var endpoint = region.GetEndpoint(RegionEndPointsManager.RDS_SERVICE_NAME);
            endpoint.ApplyToClientConfig(rdsConfig);
            return new AmazonRDSClient(accountViewModel.Credentials, rdsConfig);
        }

        public static bool IsSingleInstanceEnvironment(IAWSWizard hostWizard)
        {
            if (hostWizard.IsPropertySet(BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType))
            {
                var envType = hostWizard[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvType] as string;
                return envType.Equals(BeanstalkConstants.EnvType_SingleInstance, StringComparison.Ordinal);
            }

            return false;
        }

        /// <summary>
        /// Determines folder location to which an incremental-deployment's artifacts should be staged.
        /// </summary>
        /// <returns></returns>
        internal static string ComputeDeploymentArtifactFolder(string projectIdentifier)
        {
            string localAppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            // try and take the most significant part of the guid to keep path length short; we don't know what format
            // the inbound id is though
            string shortenedID;
            try
            {
                Guid projectGuid = new Guid(projectIdentifier);
                string g = projectGuid.ToString("D"); // collapse to 32 hex digits, groups '-' separated, no {}/() surround
                shortenedID = g.Substring(0, g.IndexOf('-'));
            }
            catch
            {
                shortenedID = projectIdentifier;
            }

            string subFolder = String.Format("AWSDeploy\\{0}\\", shortenedID);
            return Path.Combine(localAppDataFolder, subFolder);
        }

        /// <summary>
        /// If the set of build configs contains one named 'Release' then preselect it otherwise fall 
        /// back to the IDE default. Build configuration names can be anything, so this really only targets
        /// users with default project setups
        /// </summary>
        /// <param name="buildConfigurations">The set of build configurations declared by the DTE project</param>
        /// <param name="lastDeployedConfiguration">The persisted last-used configuration, if any</param>
        /// <param name="activeBuildConfiguration">The currently selected build configuration in the IDE</param>
        /// <returns>The configuration to auto select in the wizard</returns>
        internal static string SelectDeploymentBuildConfiguration(IEnumerable<string> buildConfigurations,
                                                                  string lastDeployedConfiguration,
                                                                  string activeBuildConfiguration)
        {
            if (!string.IsNullOrEmpty(lastDeployedConfiguration))
            {
                foreach (var bc in buildConfigurations.Where(bc => bc.Equals(lastDeployedConfiguration, StringComparison.Ordinal)))
                {
                    return bc;
                }
            }

            foreach (var bc in buildConfigurations.Where(bc => bc.Equals("Release", StringComparison.Ordinal)))
            {
                return bc;
            }

            return activeBuildConfiguration;
        }

        /// <summary>
        /// Returns deployment record matching the selected account and region.
        /// </summary>
        /// <param name="account"></param>
        /// <param name="regionSystemName"></param>
        /// <param name="wizardProperties"></param>
        /// <returns></returns>
        internal static BeanstalkDeploymentHistory DeploymentHistoryForAccountAndRegion(AccountViewModel account, 
                                                                                        string regionSystemName,
                                                                                        IDictionary<string, object> wizardProperties)
        {
            if (!wizardProperties.ContainsKey(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments))
                return null;

            var allPreviousDeployments = wizardProperties[DeploymentWizardProperties.SeedData.propkey_PreviousDeployments] as Dictionary<string, object>;

            if (!allPreviousDeployments.ContainsKey(DeploymentServiceIdentifiers.BeanstalkServiceName))
                return null;

            var beanstalkDeployments = allPreviousDeployments[DeploymentServiceIdentifiers.BeanstalkServiceName] as DeploymentHistories<BeanstalkDeploymentHistory>;

            // deployments within service organised by accountid: <region, T>
            var accountDeployments = beanstalkDeployments.DeploymentsForAccount(account.SettingsUniqueKey);
            return accountDeployments.ContainsKey(regionSystemName) ? accountDeployments[regionSystemName] : null;
        }

        internal class ValidBeanstalkOptions
        {
            public bool XRay { get; set; }
            public bool EnhancedHealth { get; set; }
        }

        internal static ValidBeanstalkOptions TestForValidOptionsForEnvironnment(IAmazonElasticBeanstalk beanstalkClient, string solutionStack)
        {
            return TestForValidOptionsForEnvironnment(beanstalkClient, new DescribeConfigurationOptionsRequest { SolutionStackName = solutionStack });
        }

        internal static ValidBeanstalkOptions TestForValidOptionsForEnvironnment(IAmazonElasticBeanstalk beanstalkClient, string applicationName, string environmentName)
        {
            return TestForValidOptionsForEnvironnment(beanstalkClient, new DescribeConfigurationOptionsRequest { ApplicationName = applicationName, EnvironmentName = environmentName });
        }

        private static ValidBeanstalkOptions TestForValidOptionsForEnvironnment(IAmazonElasticBeanstalk beanstalkClient, DescribeConfigurationOptionsRequest request)
        {
            var results = new ValidBeanstalkOptions();

            try
            {
                var xrayOption = new OptionSpecification
                {
                    Namespace = "aws:elasticbeanstalk:xray",
                    OptionName = "XRayEnabled"
                };
                request.Options.Add(xrayOption);

                var enhancedHealthOption = new OptionSpecification
                {
                    Namespace = "aws:elasticbeanstalk:healthreporting:system",
                    OptionName = "SystemType"
                };
                request.Options.Add(enhancedHealthOption);

                var response = beanstalkClient.DescribeConfigurationOptions(request);

                results.XRay = response.Options.FirstOrDefault(x => string.Equals(x.Namespace, xrayOption.Namespace, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, xrayOption.OptionName, StringComparison.OrdinalIgnoreCase)) != null;
                results.EnhancedHealth = response.Options.FirstOrDefault(x => string.Equals(x.Namespace, enhancedHealthOption.Namespace, StringComparison.OrdinalIgnoreCase) && string.Equals(x.Name, enhancedHealthOption.OptionName, StringComparison.OrdinalIgnoreCase)) != null;
            }
            catch (Exception e)
            {
                LOGGER.ErrorFormat("Test for valid options in environment returned error {0}", e.Message);
            }

            return results;
        }

        public const string DEFALT_SOLUTION_STACK_NAME_PREFIX = "64bit Windows Server 2016 v";
        public static string PickDefaultSolutionStack(IEnumerable<string> existingSolutionStacks)
        {
            string defaultStack = null;
            Version currentVersion = null;

            foreach(var stackName in existingSolutionStacks)
            {
                if(stackName.StartsWith(DEFALT_SOLUTION_STACK_NAME_PREFIX, StringComparison.OrdinalIgnoreCase))
                {
                    var pos = stackName.IndexOf(' ', DEFALT_SOLUTION_STACK_NAME_PREFIX.Length);
                    if (pos == -1)
                        continue;

                    var verionStr = stackName.Substring(DEFALT_SOLUTION_STACK_NAME_PREFIX.Length, pos - DEFALT_SOLUTION_STACK_NAME_PREFIX.Length);
                    Version ver;
                    if(Version.TryParse(verionStr, out ver) && (currentVersion == null || currentVersion < ver))
                    {
                        currentVersion = ver;
                        defaultStack = stackName;
                    }
                }
            }


            return defaultStack ?? existingSolutionStacks.FirstOrDefault();
        }
    }
}
