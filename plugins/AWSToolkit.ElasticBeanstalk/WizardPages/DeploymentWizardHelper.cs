using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Persistence;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.ElasticBeanstalk;
using Amazon.IdentityManagement;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;

using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.EC2;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.RDS;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages
{
    internal static class DeploymentWizardHelper
    {
        /// <summary>
        /// Return the access and secret keys to use in a query 
        /// </summary>
        public static void GetAWSAccountKeys(IAWSWizard hostWizard, out string awsAccessKey, out string awsSecretKey)
        {
            var account = hostWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
            awsAccessKey = account.AccessKey;
            awsSecretKey = account.SecretKey;
        }

        public static IAmazonElasticBeanstalk GetBeanstalkClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var beanstalkConfig = new AmazonElasticBeanstalkConfig
            {
                ServiceURL = region.GetEndpoint(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME).Url
            };
            return new AmazonElasticBeanstalkClient(accountViewModel.AccessKey, accountViewModel.SecretKey, beanstalkConfig);
        }

        public static IAmazonIdentityManagementService GetIAMClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var config = new AmazonIdentityManagementServiceConfig
            {
                ServiceURL = region.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).Url
            };
            return new AmazonIdentityManagementServiceClient(accountViewModel.AccessKey, accountViewModel.SecretKey, config);
        }

        public static IAmazonEC2 GetEC2Client(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var ec2Config = new AmazonEC2Config
            {
                ServiceURL = region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME).Url
            };
            return new AmazonEC2Client(accountViewModel.AccessKey, accountViewModel.SecretKey, ec2Config);
        }

        public static IAmazonRDS GetRDSClient(AccountViewModel accountViewModel, RegionEndPointsManager.RegionEndPoints region)
        {
            var rdsConfig = new AmazonRDSConfig
            {
                ServiceURL = region.GetEndpoint(RegionEndPointsManager.RDS_SERVICE_NAME).Url
            };
            return new AmazonRDSClient(accountViewModel.AccessKey, accountViewModel.SecretKey, rdsConfig);
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
            return accountDeployments[regionSystemName];
        }
    }
}
