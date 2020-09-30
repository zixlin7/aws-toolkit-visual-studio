using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.Controller;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.Util;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Commands
{
    public abstract class BaseBeanstalkDeployCommand
    {
        protected const int MAX_REFRESH_RETRIES = 3;
        protected const int SLEEP_TIME_BETWEEN_REFRESHES = 500;

        public AccountViewModel Account { get; protected set; }
        public string DeploymentPackage { get; protected set; }
        public IDictionary<string, object> DeploymentProperties { get; protected set; }

        public DeploymentControllerObserver Observer { get; protected set; }

        protected static ILog LOGGER = LogManager.GetLogger(typeof(BaseBeanstalkDeployCommand));

        public delegate string GetDefaultVpcSubnetFunc(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints region);

        protected BaseBeanstalkDeployCommand(IDictionary<string, object> deploymentProperties)
            : this(deploymentProperties, null)
        {
        }

        protected BaseBeanstalkDeployCommand(
            IDictionary<string, object> deploymentProperties,
            DeploymentControllerObserver observer)
        {
            Observer = observer ?? new DeploymentControllerObserver(LOGGER);

            DeploymentProperties = deploymentProperties;
            this.Account = getValue<AccountViewModel>(CommonWizardProperties.AccountSelection.propkey_SelectedAccount);
        }


        public void Execute(object state)
        {
            Execute();
        }

        public abstract void Execute();


        protected string ConfigureIAMRole(AccountViewModel account,
            RegionEndPointsManager.RegionEndPoints regionEndpoints)
        {
            var roleTemplates =
                getValue<Amazon.AWSToolkit.CommonUI.Components.IAMCapabilityPicker.PolicyTemplate[]>(
                    BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_PolicyTemplates);
            if (roleTemplates != null)
            {
                var endpoint = regionEndpoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME);
                var config = new AmazonIdentityManagementServiceConfig();
                endpoint.ApplyToClientConfig(config);

                var client = new AmazonIdentityManagementServiceClient(account.Credentials, config);


                var newRoleName = "aws-elasticbeanstalk-" +
                                  getValue<string>(BeanstalkDeploymentWizardProperties.EnvironmentProperties
                                      .propkey_EnvName);
                var existingRoleNames = ExistingRoleNames(client);

                if (existingRoleNames.Contains(newRoleName))
                {
                    var baseRoleName = newRoleName;
                    for (int i = 0; true; i++)
                    {
                        var tempName = baseRoleName + "-" + i;
                        if (!existingRoleNames.Contains(tempName))
                        {
                            newRoleName = tempName;
                            break;
                        }
                    }
                }

                var role = client.CreateRole(new CreateRoleRequest
                {
                    RoleName = newRoleName,
                    AssumeRolePolicyDocument
                        = Constants.GetIAMRoleAssumeRolePolicyDocument(RegionEndPointsManager.EC2_SERVICE_NAME,
                            regionEndpoints)
                }).Role;
                this.Observer.Status("Created IAM Role {0}", newRoleName);

                var profile = client.CreateInstanceProfile(new CreateInstanceProfileRequest
                {
                    InstanceProfileName = newRoleName
                }).InstanceProfile;
                this.Observer.Status("Created IAM Instance Profile {0}", profile.InstanceProfileName);

                client.AddRoleToInstanceProfile(new AddRoleToInstanceProfileRequest
                {
                    InstanceProfileName = profile.InstanceProfileName,
                    RoleName = role.RoleName
                });
                this.Observer.Status("Adding role {0} to instance profile {1}", role.RoleName,
                    profile.InstanceProfileName);

                foreach (var template in roleTemplates)
                {
                    client.PutRolePolicy(new PutRolePolicyRequest
                    {
                        RoleName = role.RoleName,
                        PolicyName = template.IAMCompatibleName,
                        PolicyDocument = template.Body.Trim()
                    });

                    this.Observer.Status("Applied policy \"{0}\" to the role", template.Name);
                }

                return newRoleName;
            }
            else
            {
                return getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties
                    .propkey_InstanceProfileName);
            }
        }

        protected HashSet<string> ExistingRoleNames(IAmazonIdentityManagementService client)
        {
            HashSet<string> roles = new HashSet<string>();

            ListRolesResponse response = null;
            do
            {
                ListRolesRequest request = new ListRolesRequest();
                if (response != null)
                    request.Marker = response.Marker;
                response = client.ListRoles(request);
                foreach (var role in response.Roles)
                {
                    roles.Add(role.RoleName);
                }
            } while (response.IsTruncated);

            return roles;
        }

        protected T getValue<T>(string key)
        {
            object value;
            if (DeploymentProperties.TryGetValue(key, out value))
            {
                T convertedValue = (T) Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }

        protected void SelectNewTreeItems(string applicationName, string environmentName)
        {
            bool showStatus = false;
            if (DeploymentProperties.ContainsKey(
                DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose))
                showStatus = getValue<bool>(DeploymentWizardProperties.ReviewProperties.propkey_LaunchStatusOnClose);

            var serviceRoot = Account.FindSingleChild<ElasticBeanstalkRootViewModel>(false);
            ApplicationViewModel application = null;
            EnvironmentViewModel environment = null;
            for (int i = 0; (application == null || environment == null) && i < MAX_REFRESH_RETRIES; i++)
            {
                if (application == null)
                {
                    serviceRoot.Refresh(false);
                    application = serviceRoot.FindSingleChild<ApplicationViewModel>(false,
                        x => x.Application.ApplicationName == applicationName);
                }

                if (application != null)
                {
                    environment = application.FindSingleChild<EnvironmentViewModel>(false,
                        x => x.Environment.EnvironmentName == environmentName);
                    if (environment == null)
                    {
                        application.Refresh(false);
                        environment = application.FindSingleChild<EnvironmentViewModel>(false,
                            x => x.Environment.EnvironmentName == environmentName);
                    }

                    if (environment != null)
                    {
                        if (showStatus)
                        {
                            IEnvironmentViewMetaNode meta = environment.MetaNode as IEnvironmentViewMetaNode;
                            if (meta.OnEnvironmentStatus != null)
                            {
                                meta.OnEnvironmentStatus(environment);
                            }

                            ToolkitFactory.Instance.Navigator.SelectedNode = environment;
                        }

                        // Found both application and environment so break out
                        break;
                    }
                }

                LOGGER.InfoFormat(
                    "Application {0} Found {1}, Environment {2} Found {3}, retrying since one wasn't found",
                    application, application != null, environment, environment != null);

                // Didn't find the application or environment, sleeping a little to let the service catch up.
                Thread.Sleep(SLEEP_TIME_BETWEEN_REFRESHES);
            }
        }

        public void CreateKeyPair(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region,
            string keyName)
        {
            Observer.Status("Creating keypair '{0}'", keyName);

            var endpoint = region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            var config = new AmazonEC2Config();
            endpoint.ApplyToClientConfig(config);

            using (var client = new AmazonEC2Client(account.Credentials, config))
            {
                var request = new CreateKeyPairRequest() {KeyName = keyName};
                var response = client.CreateKeyPair(request);

                IEC2RootViewModel ec2Root = Account.FindSingleChild<IEC2RootViewModel>(false);
                KeyPairLocalStoreManager.Instance.SavePrivateKey(Account,
                    region.SystemName,
                    keyName,
                    response.KeyPair.KeyMaterial);
                LOGGER.Debug("key pair created with name " + keyName + " and stored in local store");
            }
        }

        protected string GetDefaultVPCSubnet(AccountViewModel account, RegionEndPointsManager.RegionEndPoints region)
        {
            Observer.Status("Determining default VPC subnets for ELB load balancer.");

            var endpoint = region.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);
            var config = new AmazonEC2Config();
            endpoint.ApplyToClientConfig(config);

            using (var client = new AmazonEC2Client(account.Credentials, config))
            {
                var defaultVpc = client.DescribeVpcs().Vpcs.Where(x => x.IsDefault).FirstOrDefault();
                if (defaultVpc == null)
                {
                    LOGGER.Debug("No default VPC found when looking for default subnets");
                    return null;
                }

                LOGGER.Debug("Default VPC found: " + defaultVpc.VpcId);


                var allSubnets = client.DescribeSubnets(new DescribeSubnetsRequest
                {
                    Filters = new List<Filter>
                        {new Filter {Name = "vpc-id", Values = new List<string> {defaultVpc.VpcId}}}
                }).Subnets;


                var defaultSubnetIds = allSubnets.Where(x => x.DefaultForAz).Select((subnet) => subnet.SubnetId);
                var formattedSubnetIds = string.Join(",", defaultSubnetIds);

                LOGGER.Debug("Default subnets found: " + formattedSubnetIds);

                return formattedSubnetIds;
            }
        }

        protected void GetVpcDetails(out bool launchIntoVpc, out string vpcId)
        {
            launchIntoVpc =
                getValue<bool>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_LaunchIntoVPC);
            vpcId = null;

            // if user did not choose to use a custom vpc and we are in a vpc-only environment, push through the default vpc id so we
            // create resources in the right place from the get-go, and don't rely on the service to 'notice'
            if (launchIntoVpc)
            {
                vpcId = getValue<string>(BeanstalkDeploymentWizardProperties.AWSOptionsProperties.propkey_VPCId);
            }
            else if (getValue<bool>(DeploymentWizardProperties.SeedData.propkey_VpcOnlyMode))
            {
                vpcId = getValue<string>(DeploymentWizardProperties.AWSOptions.propkey_DefaultVpcId);
                launchIntoVpc = !string.IsNullOrEmpty(vpcId);
            }
        }
    }
}