using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.RDS.Nodes;
using Amazon.AWSToolkit.RDS.Model;
using Amazon.AWSToolkit.RDS.WizardPages;
using Amazon.AWSToolkit.RDS.WizardPages.PageControllers;
using Amazon.AWSToolkit.RDS.WizardPages.PageUI;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.Util;
using log4net;
using Amazon.AWSToolkit.Util;
using System.Threading;
using Filter = Amazon.EC2.Model.Filter;
using Amazon.AWSToolkit.EC2;

namespace Amazon.AWSToolkit.RDS.Controller
{
    public class LaunchDBInstanceController : BaseContextCommand
    {
        public const string CreatedInstanceParameter = "CREATED_INSTANCE_PARAMETER";

        static readonly ILog Logger = LogManager.GetLogger(typeof(LaunchDBInstanceController));
        RDSInstanceRootViewModel _rootModel;

        const int MaxRefreshRetries = 3;
        const int SleepTimeBetweenRefreshes = 500;

        public override ActionResults Execute(IViewModel model)
        {
            try
            {
                if (model is RDSRootViewModel)
                {
                    var root = model as RDSRootViewModel;
                    this._rootModel = model.FindSingleChild<RDSInstanceRootViewModel>(false);
                }
                else
                {
                    this._rootModel = model as RDSInstanceRootViewModel;
                }
            
                if (_rootModel == null)
                    return new ActionResults().WithSuccess(false);

                var seedProperties = new Dictionary<string, object>();
                var account = _rootModel.AccountViewModel;
                seedProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] = account;
                seedProperties[CommonWizardProperties.propkey_NavigatorRootViewModel] = _rootModel;
                seedProperties[RDSWizardProperties.SeedData.propkey_RDSClient] = _rootModel.RDSClient;

                var endPoints = RegionEndPointsManager.Instance.GetRegion(_rootModel.CurrentEndPoint.RegionSystemName);
                var endPoint = endPoints.GetEndpoint(RegionEndPointsManager.EC2_SERVICE_NAME);

                var config = new AmazonEC2Config() { ServiceURL = endPoint.Url };
                var ec2Client = new AmazonEC2Client(account.AccessKey, account.SecretKey, config);
                seedProperties[RDSWizardProperties.SeedData.propkey_EC2Client] = ec2Client;

                seedProperties[RDSWizardProperties.SeedData.propkey_DBInstanceWrapper] = null;

                seedProperties[RDSWizardProperties.SeedData.propkey_VPCOnly] = EC2Utilities.CheckForVpcOnlyMode(ec2Client);

                var wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.RDS.View.LaunchDBInstance", seedProperties);
                wizard.Title = "Launch DB Instance";

                var defaultPages = new IAWSWizardPageController[]
                {
                    new LaunchDBInstanceEnginePageController(),
                    new LaunchDBInstanceDetailsPageController(),
                    new LaunchDBInstanceAdvancedSettingsPageController(),
                    new DBInstanceBackupsPageController(),
                    new LaunchDBInstanceReviewPageController()
                };

                wizard.RegisterPageControllers(defaultPages, 0);
                if (wizard.Run())
                {
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.CreateDBInstanceAsync), wizard.CollectedProperties);
                    return new ActionResults().WithSuccess(true);
                }
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Exception during RDS launch wizard run or instance launch process - {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Launch RDS Instance", "An error occurred in the launch wizard or instance creation process.\r\n" + e.Message);
            }

            return new ActionResults().WithSuccess(false);
        }

        public static bool IsLaunchingIntoVPC(string vpcId)
        {
            if (string.IsNullOrEmpty(vpcId))
                return false;

            if (vpcId.Equals(VPCWrapper.NotInVpcPseudoId, StringComparison.Ordinal))
                return false;

            return true;
        }

        public static bool CreateNewVpcOnLaunch(string vpcId)
        {
            return IsLaunchingIntoVPC(vpcId) && vpcId.Equals(VPCWrapper.CreateNewVpcPseudoId, StringComparison.Ordinal);
        }

        void CreateDBInstanceAsync(object state)
        {
            try
            {
                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("Configuring for DB instance launch", true);

                var launchProperties = state as Dictionary<string, object>;

                var ec2Client = launchProperties[RDSWizardProperties.SeedData.propkey_EC2Client] as IAmazonEC2;
                var rdsClient = launchProperties[RDSWizardProperties.SeedData.propkey_RDSClient] as IAmazonRDS;

                var request = new CreateDBInstanceRequest();

                // mandatory parameters
                request.Engine = getValue<string>(launchProperties, RDSWizardProperties.EngineProperties.propkey_DBEngine);
                var ver = getValue<DBEngineVersion>(launchProperties, RDSWizardProperties.EngineProperties.propkey_EngineVersion);
                request.EngineVersion = ver.EngineVersion;

                request.DBInstanceIdentifier = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DBInstanceIdentifier);
                request.DBInstanceClass = getValue<DBInstanceClass>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_InstanceClass).Id;
                request.AllocatedStorage = getValue<int>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_Storage);

                request.MasterUsername = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_MasterUserName);
                request.MasterUserPassword = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_MasterUserPassword);

                // optional parameters
                if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_LicenseModel))
                    request.LicenseModel = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_LicenseModel);

                if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade))
                    request.AutoMinorVersionUpgrade = getValue<bool>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_AutoMinorVersionUpgrade);

                var isMultiAz = getValue<bool>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_MultiAZ);
                var optionGroups = new List<OptionGroup>();
                
                // SQL Server does Multi AZ by using an option group for mirroring instead of the MultiAZ Property.
                if (request.Engine.StartsWith("sqlserver-") && isMultiAz)
                {
                    request.MultiAZ = false;
                    var optionGroup = GetMirroredOptionGroup(request);
                    request.OptionGroupName = optionGroup.OptionGroupName;
                    optionGroups.Add(optionGroup);
                }
                else
                    request.MultiAZ = isMultiAz;

                if (!isMultiAz && launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_AvailabilityZone))
                    request.AvailabilityZone = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_AvailabilityZone);

                if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_DatabaseName))
                    request.DBName = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DatabaseName);

                if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_DatabasePort))
                    request.Port = getValue<int>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DatabasePort);

                var vpcId = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_VpcId);
                var isVpcLaunch = !string.IsNullOrEmpty(vpcId);

                if (isVpcLaunch)
                {
                    if (vpcId.Equals(VPCWrapper.CreateNewVpcPseudoId))
                        vpcId = CreateVPCForDBInstance(ec2Client);

                    // if launching into a default VPC, we may need to create the subnet group. Note that the console shows
                    // (and uses) the name 'Default' - we can't use it, as it is reserved
                    request.DBSubnetGroupName = getValue<bool>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_CreateDBSubnetGroup) 
                        ? CreateDBSubnetGroupForVPC(ec2Client, rdsClient, vpcId, null) 
                        : getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DBSubnetGroup);
                }

                var securityGroups = new List<string>();
                var createNewSecurityGroup = getValue<bool>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_CreateNewDBSecurityGroup);
                if (createNewSecurityGroup)
                {
                    var newSecurityGroup = CreateNewSecurityGroupForLaunch(rdsClient, ec2Client, vpcId);
                    if (newSecurityGroup != null)
                    {
                        securityGroups.Add(newSecurityGroup);
                        if (isVpcLaunch)
                            request.VpcSecurityGroupIds = securityGroups;
                        else
                            request.DBSecurityGroups = securityGroups;
                    }
                }
                else if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_SecurityGroups))
                {
                    var groups = getValue<List<SecurityGroupInfo>>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_SecurityGroups);
                    if (groups.Count > 0)
                    {
                        if (isVpcLaunch)
                        {
                            securityGroups.AddRange(groups.Select(@group => @group.Id));
                            request.VpcSecurityGroupIds = securityGroups;
                        }
                        else
                        {
                            securityGroups.AddRange(groups.Select(@group => @group.Name));
                            request.DBSecurityGroups = securityGroups;
                        }
                    }
                }

                if (securityGroups.Any() 
                        && (getValue<bool>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_AddCIDRToGroups)
                            || createNewSecurityGroup))
                {
                    var port = getValue<int>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DatabasePort);
                    AddCidrToSecurityGroups(IPAddressUtil.DetermineIPFromExternalSource(),
                                            port,
                                            securityGroups,
                                            isVpcLaunch,
                                            rdsClient,
                                            ec2Client);
                }

                if (isVpcLaunch && launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_PubliclyAccessible))
                    request.PubliclyAccessible = (bool) launchProperties[RDSWizardProperties.InstanceProperties.propkey_PubliclyAccessible];

                if (launchProperties.ContainsKey(RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup))
                    request.DBParameterGroupName = getValue<string>(launchProperties, RDSWizardProperties.InstanceProperties.propkey_DBParameterGroup);

                if (launchProperties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod))
                    request.BackupRetentionPeriod = getValue<int>(launchProperties, RDSWizardProperties.MaintenanceProperties.propkey_RetentionPeriod);

                if (launchProperties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow))
                    request.PreferredBackupWindow = getValue<string>(launchProperties, RDSWizardProperties.MaintenanceProperties.propkey_BackupWindow);

                if (launchProperties.ContainsKey(RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow))
                    request.PreferredMaintenanceWindow = getValue<string>(launchProperties, RDSWizardProperties.MaintenanceProperties.propkey_MaintenanceWindow);

                OutputProgress("requesting creation of new DB instance");

                var response = _rootModel.RDSClient.CreateDBInstance(request);
                var instance = new DBInstanceWrapper(response.DBInstance, optionGroups);

                var launchViewOnClose = getValue<bool>(launchProperties, RDSWizardProperties.ReviewProperties.propkey_LaunchInstancesViewOnClose);
                ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
                {
                    if (this._rootModel != null)
                    {
                        this._rootModel.AddDBInstance(instance);
                        _rootModel.Refresh(false);

                        if (launchViewOnClose)
                        {
                            RDSInstanceViewModel newInstance = null;
                            for (var i = 0; newInstance == null && i < MaxRefreshRetries; i++)
                            {
                                newInstance = _rootModel.FindSingleChild<RDSInstanceViewModel>(false, x => x.Name == instance.DisplayName);

                                if (newInstance == null)
                                    Thread.Sleep(SleepTimeBetweenRefreshes);
                            }

                            if (newInstance != null)
                            {
                                var command = new ViewDBInstancesController();
                                command.Execute(newInstance);
                                ToolkitFactory.Instance.Navigator.SelectedNode = newInstance;
                            }
                            else
                                _rootModel.ExecuteDefaultAction();
                        }
                    }
                }));

                ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("DB instance configuration and launch request completed", true);
            }
            catch (AmazonRDSException e)
            {
                Logger.ErrorFormat("Caught exception creating DB instance - {0}", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Data Error", "Error launching DB instance: " + e.Message);
            }
        }

        /// <summary>
        /// Creates a VPC for the instance, emulating the settings that the RDS console launch wizard
        /// uses.
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <returns></returns>
        string CreateVPCForDBInstance(IAmazonEC2 ec2Client)
        {
            try
            {
                OutputProgress("creating new VPC for DB instance");

                const string cidrBlockFormat = "172.30.{0}.0/{1}"; // follow console

                var vpc = ec2Client.CreateVpc(new CreateVpcRequest
                { 
                    CidrBlock = string.Format(cidrBlockFormat, 0, 16) 
                }).Vpc;
                WaitTillTrue(((Func<bool>)(() => ec2Client.DescribeVpcs(new DescribeVpcsRequest { VpcIds = new List<string> { vpc.VpcId } }).Vpcs.Count == 1)));

                OutputProgress(string.Format("...created VPC with ID {0}", vpc.VpcId));

                // create a subnet for each AZ
                var zones = ec2Client.DescribeAvailabilityZones().AvailabilityZones;
                int block = 0;
                foreach (var zone in zones)
                {
                    var subnet = ec2Client.CreateSubnet(new CreateSubnetRequest
                    {
                        AvailabilityZone = zone.ZoneName,
                        CidrBlock = string.Format(cidrBlockFormat, block, 24),
                        VpcId = vpc.VpcId
                    }).Subnet;

                    OutputProgress(string.Format("...created subnet {0} for availability zone {1} with CIDR block {2}", subnet.SubnetId, subnet.AvailabilityZone, subnet.CidrBlock));
                    block++;
                }

                var internetGateway = ec2Client.CreateInternetGateway().InternetGateway;

                ec2Client.AttachInternetGateway(new AttachInternetGatewayRequest
                {
                    InternetGatewayId = internetGateway.InternetGatewayId,
                    VpcId = vpc.VpcId
                });

                OutputProgress(string.Format("...created internet gateway with ID {0} and attached to VPC", internetGateway.InternetGatewayId));

                var defaultRouteTable = ec2Client.DescribeRouteTables(new DescribeRouteTablesRequest
                {
                    Filters = new List<Filter>
                    { 
                        new Filter { Name = "vpc-id", Values = new List<string> { vpc.VpcId } },
                        new Filter { Name = "association.main", Values = new List<string> { "true" } } 
                    }
                }).RouteTables[0];

                ec2Client.CreateRoute(new CreateRouteRequest()
                {
                    RouteTableId = defaultRouteTable.RouteTableId,
                    DestinationCidrBlock = "0.0.0.0/0",
                    GatewayId = internetGateway.InternetGatewayId
                });

                // can set only one attribute at a time
                ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest
                {
                    VpcId = vpc.VpcId,
                    EnableDnsHostnames = true
                });

                ec2Client.ModifyVpcAttribute(new ModifyVpcAttributeRequest
                {
                    VpcId = vpc.VpcId,
                    EnableDnsSupport = true
                });

                OutputProgress("VPC creation for new DB instance completed");
                return vpc.VpcId;
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Exception constructing new VPC: {0}", e.Message);
            }

            return null;
        }

        /// <summary>
        /// Creates a DB subnet group spanning all subnets in the specified VPC. If
        /// a suggested name is not specified, we'll follow console convention and
        /// use something indicating default for the VPC.
        /// </summary>
        /// <param name="ec2Client"></param>
        /// <param name="rdsClient"></param>
        /// <param name="vpcId"></param>
        /// <param name="suggestedSubnetGroupName"></param>
        /// <returns>The name of the new subnet group</returns>
        string CreateDBSubnetGroupForVPC(IAmazonEC2 ec2Client, IAmazonRDS rdsClient, string vpcId, string suggestedSubnetGroupName)
        {
            var subnetGroupName = suggestedSubnetGroupName;
            if (string.IsNullOrEmpty(subnetGroupName))
                subnetGroupName = string.Format("default-{0}", vpcId);

            try
            {
                var subnetsQueryResponse = ec2Client.DescribeSubnets(new DescribeSubnetsRequest
                {
                    Filters = new List<Filter>
                {
                    new Filter
                    {
                        Name = "vpc-id",
                        Values = new List<string> { vpcId }
                    }
                }
                });

                rdsClient.CreateDBSubnetGroup(new CreateDBSubnetGroupRequest
                {
                    DBSubnetGroupDescription = "Auto-created by the AWS Toolkit for Visual Studio",
                    DBSubnetGroupName = subnetGroupName,
                    SubnetIds = subnetsQueryResponse.Subnets.Select(s => s.SubnetId).ToList()
                });

                OutputProgress(string.Format("created DB subnet group with name {0} for VPC subnets", subnetGroupName));
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Exception querying for subnets/creating db subnet group {0}: {1}", subnetGroupName, e.Message);
            }

            return subnetGroupName;
        }

        private static void WaitTillTrue(Func<bool> func)
        {
            for (int i = 0; i < 40; i++)
            {
                try
                {
                    if (func())
                        return;
                }
                catch { }
                AWSSDKUtils.Sleep(1000);
            }
        }

        OptionGroup GetMirroredOptionGroup(CreateDBInstanceRequest request)
        {
            var versionComponents = request.EngineVersion.Split('.');
            // Working with an unknown engine version string
            if (versionComponents.Length < 2)
                return null;

            var optionName = string.Format("{0}-{1}-{2}-mirrored", request.Engine, versionComponents[0], versionComponents[1]);

            var describeResponse = this._rootModel.RDSClient.DescribeOptionGroups();
            var optionGroup = describeResponse.OptionGroupsList.FirstOrDefault(x => x.OptionGroupName == optionName);
            if (optionGroup != null)
                return optionGroup;


            var createOptionsRequest = new CreateOptionGroupRequest
            {
                EngineName = request.Engine,
                MajorEngineVersion = string.Format("{0}.{1}", versionComponents[0], versionComponents[1]),
                OptionGroupName = optionName,
                OptionGroupDescription = "Default Mirroring-enabled option group for sqlserver-se"
            };

            this._rootModel.RDSClient.CreateOptionGroup(createOptionsRequest);

            var modifyRequest = new ModifyOptionGroupRequest
            {
                OptionGroupName = createOptionsRequest.OptionGroupName,
                ApplyImmediately = true
            };
            modifyRequest.OptionsToInclude.Add(new OptionConfiguration
            {
                OptionName = RDSConstants.MIRRORING_OPTION_GROUP
            });
            this._rootModel.RDSClient.ModifyOptionGroup(modifyRequest);

            describeResponse = this._rootModel.RDSClient.DescribeOptionGroups();
            optionGroup = describeResponse.OptionGroupsList.FirstOrDefault(x => x.OptionGroupName == optionName);
            return optionGroup;
        }

        /// <summary>
        /// Edits the RDS or EC2 security groups associated with the DB instance launch
        /// so that the user's current IP is permitted access.
        /// </summary>
        /// <param name="currentIp"></param>
        /// <param name="port"></param>
        /// <param name="securityGroups"></param>
        /// <param name="vpcGroups"></param>
        /// <param name="rdsClient"></param>
        /// <param name="ec2Client"></param>
        void AddCidrToSecurityGroups(string currentIp, 
                                     int port,
                                     IEnumerable<string> securityGroups, 
                                     bool vpcGroups, 
                                     IAmazonRDS rdsClient, 
                                     IAmazonEC2 ec2Client)
        {
            if (string.IsNullOrEmpty(currentIp))
                return;

            var cidr = currentIp + "/32";

            if (!vpcGroups)
            {
                foreach (var groupName in securityGroups)
                {
                    try
                    {
                        var request = new AuthorizeDBSecurityGroupIngressRequest()
                        {
                            DBSecurityGroupName = groupName,
                            CIDRIP = cidr
                        };
                        rdsClient.AuthorizeDBSecurityGroupIngress(request);
                        OutputProgress(string.Format("added CIDR {0} to security group {1}", cidr, groupName));
                    }
                    catch (AmazonRDSException e)
                    {
                        if (e is AuthorizationAlreadyExistsException)
                            Logger.InfoFormat("CIDR {0} already exists in DB Security Group {1}", cidr, groupName);
                        else
                            Logger.ErrorFormat("Caught exception adding CIDR {0} to DB Security Group {1}, exception {2} message {3}",
                                              cidr, groupName, e.GetType(), e.Message);
                    }
                }

                return;
            }

            var ipPermission = new IpPermission
            {
                FromPort = port,
                ToPort = port,
                IpProtocol = "tcp",
                IpRanges = new List<string> {  cidr }
            };

            foreach (var groupId in securityGroups)
            {
                try
                {
                    var request = new AuthorizeSecurityGroupIngressRequest
                    {
                        GroupId = groupId,
                        IpPermissions = new List<IpPermission> {  ipPermission }
                    };

                    ec2Client.AuthorizeSecurityGroupIngress(request);
                    OutputProgress(string.Format("added CIDR {0} to security group {1}", cidr, groupId));
                }
                catch (AmazonEC2Exception e)
                {
                    Logger.ErrorFormat("Caught exception adding CIDR {0} to EC2 Security Group {1}, exception {2} message {3}",
                                        cidr, groupId, e.GetType(), e.Message);
                }
            }
        }

        T getValue<T>(Dictionary<string, object> properties, string key)
        {
            object value;
            if (properties.TryGetValue(key, out value))
            {
                T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }

        string CreateNewSecurityGroupForLaunch(IAmazonRDS rdsClient, IAmazonEC2 ec2Client, string vpcId)
        {
            if (!string.IsNullOrEmpty(vpcId))
            {
                var ec2SecurityGroupName = GenerateUniqueEC2SecurityGroupName(ec2Client);
                try
                {
                    var createEC2SecurityGroupResponse = ec2Client.CreateSecurityGroup(new CreateSecurityGroupRequest
                    {
                        GroupName = ec2SecurityGroupName,
                        Description = "Auto-created by the AWS Toolkit for Visual Studio",
                        VpcId = vpcId
                    });

                    OutputProgress(string.Format("created security group with ID {0}", ec2SecurityGroupName));

                    return createEC2SecurityGroupResponse.GroupId;
                }
                catch (AmazonEC2Exception e)
                {
                    Logger.ErrorFormat("Caught exception attempting to create new EC2 security group with name {0}, exception message {1}",
                                       ec2SecurityGroupName, e.Message);
                }
            }
            else
            {
                var dbSecurityGroupName = GenerateUniqueDBSecurityGroupName(rdsClient);
                try
                {

                    var createDBSecurityGroupResponse = rdsClient.CreateDBSecurityGroup(new CreateDBSecurityGroupRequest
                    {
                        DBSecurityGroupDescription = "Auto-created by the AWS Toolkit for Visual Studio",
                        DBSecurityGroupName = dbSecurityGroupName
                    });
                    return createDBSecurityGroupResponse.DBSecurityGroup.DBSecurityGroupName;
                }
                catch (AmazonRDSException e)
                {
                    Logger.ErrorFormat("Caught exception attempting to create new DB security group with name {0}, exception message {1}",
                                       dbSecurityGroupName, e.Message);
                }
            }

            return null;
        }

        string GenerateUniqueDBSecurityGroupName(IAmazonRDS rdsClient)
        {
            OutputProgress("inspecting security groups to determine unique name for new launch");
            const string vsToolkitGroupPrefix = "aws-vs-toolkit-rdsgroup";
            var index = 1;
            try
            {
                var response = rdsClient.DescribeDBSecurityGroups();
                index += response.DBSecurityGroups.Count(@group => @group.DBSecurityGroupName.StartsWith(vsToolkitGroupPrefix, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Caught exception describing RDS security groups to obtain name for new group, {0}", e.Message);
            }

            return string.Format("{0}-{1}", vsToolkitGroupPrefix, index);
        }

        string GenerateUniqueEC2SecurityGroupName(IAmazonEC2 ec2Client)
        {
            const string vsToolkitGroupPrefix = "aws-vs-toolkit-ec2group";

            OutputProgress("inspecting security groups to determine unique ID for new launch");
            var index = 1;
            try
            {
                var response = ec2Client.DescribeSecurityGroups();
                index += response.SecurityGroups.Count(@group => @group.GroupName.StartsWith(vsToolkitGroupPrefix, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception e)
            {
                Logger.ErrorFormat("Caught exception describing EC2 security groups to obtain name for new group, {0}", e.Message);
            }

            return string.Format("{0}-{1}", vsToolkitGroupPrefix, index);
        }

        void OutputProgress(string message)
        {
            // do not set force visible in case user has switched tab away whilst we're working
            ToolkitFactory.Instance.ShellProvider.OutputToHostConsole("..." + message, false);
        }
    }
}
