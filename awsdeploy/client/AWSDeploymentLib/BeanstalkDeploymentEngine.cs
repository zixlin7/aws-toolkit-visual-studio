using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Amazon;
using Amazon.AWSToolkit;
using Amazon.DevTools;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk;
using Amazon.ElasticBeanstalk.Model;
using Amazon.RDS;
using Amazon.RDS.Model;
using Amazon.S3.Transfer;

using ICSharpCode.SharpZipLib.Zip;
using NGit;
using NGit.Api;
using NGit.Revwalk;
using NGit.Storage.File;
using NGit.Treewalk;
using Sharpen;
using Amazon.EC2;

using Amazon.IdentityManagement;
using Amazon.IdentityManagement.Model;
using Amazon.Auth.AccessControlPolicy;
using Amazon.S3.Model;
using Amazon.Auth.AccessControlPolicy.ActionIdentifiers;

namespace AWSDeployment
{
    public class BeanstalkDeploymentEngine : DeploymentEngineBase
    {
        // stack name selected if we can't use toolkit's ami manifest to determine default
        private const string FALLBACK_DEFAULT_STACK = "64bit Windows Server 2012 running IIS 8";

        #region Beanstalk-specific Deployment Properties

        const int NUMBER_OF_FILES_CHANGES_TO_SWITCH_TO_ABBREVIATED_MODE = 20;
        const string GIT_PUSH_SERVICE_NAME = "GitPush";

        public const string APPLICATION_NAME = "ApplicationName";
        public const string ENVIRONMENT_NAME = "EnvironmentName";

        /// <summary>
        /// The name of the Beanstalk application
        /// </summary>
        public string ApplicationName { get; set; }

        /// <summary>
        /// Optional description for the application
        /// </summary>
        public string ApplicationDescription { get; set; }

        /// <summary>
        /// The name of the environment for the Beanstalk application
        /// </summary>
        public string EnvironmentName { get; set; }

        /// <summary>
        /// Optional description for the application environment
        /// </summary>
        public string EnvironmentDescription { get; set; }

        /// <summary>
        /// CNAME for a non-single instance deployment environment
        /// </summary>
        public string EnvironmentCNAME { get; set; }

        /// <summary>
        /// Type of the environment to launch. Currently 'SingleInstance'
        /// or 'LoadBalanced'. If not set, 'LoadBalanced' is assumed.
        /// </summary>
        public string EnvironmentType { get; set; }

        bool IsSingleInstanceEnvironmentType
        {
            get
            {
                return EnvironmentType != null 
                    && EnvironmentType.Equals("SingleInstance", StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// If true, deployment will be to a new environment of the specified name
        /// otherwise we are re-using an existing environment
        /// </summary>
        public bool CreateNewEnvironment { get; set; }

        /// <summary>
        /// Whether the deployment created a new Beanstalk application instance
        /// </summary>
        public bool DeploymentCreatedApplication { get; protected set; }

        /// <summary>
        /// Whether the deployment created a new Beanstalk environment
        /// </summary>
        public bool DeploymentCreatedEnvironment { get; protected set; }

        /// <summary>
        /// If true, deployment will be performed via intermediate Git repository
        /// </summary>
        public bool UseIncrementalDeployment { get; set; }

        /// <summary>
        /// If using incremental deployment, the folder containing the Git repository
        /// to which the deployment package content, referenced from DeploymentPackage, 
        /// will be committed prior to push.
        /// </summary>
        public string IncrementalPushRepositoryLocation { get; set; }

        /// <summary>
        /// The Beanstalk solution stack to be used to host the deployment
        /// </summary>
        public string SolutionStack { get; set; }

        /// <summary>
        /// Optional, id of a custom ami to override that defined by the solution stack
        /// </summary>
        public string CustomAmiID { get; set; }

        /// <summary>
        /// t1.micro etc; the size of the EC2 instance(s) that will be created
        /// </summary>
        public string InstanceTypeID { get; set; }

        /// <summary>
        /// IAM role name for instance launch; we assume there is a corresponding
        /// instance profile with the same name. If this is the beanstalk
        /// 'default' role name, we create it if necessary.
        /// </summary>
        public string RoleName { get; set; }

        /// <summary>
        /// IAM role that Elastic Beanstalk assumes when calling other services on your behalf.
        /// Elastic Beanstalk uses the service role that you specify when creating an Elastic 
        /// Beanstalk environment when it calls Amazon Elastic Compute Cloud (Amazon EC2), Elastic 
        /// Load Balancing, and Auto Scaling APIs to gather information about the health of its AWS 
        /// resources.
        /// </summary>
        public string ServiceRoleName { get; set; }

        public bool LaunchIntoVPC { get; set; }
        public string VPCId { get; set; }
        public string VPCSecurityGroupId { get; set; }
        public string InstanceSubnetId { get; set; }
        public string ELBSubnetId { get; set; }
        public string ELBScheme { get; set; }

        public bool EnableConfigRollingDeployment;
        public int ConfigRollingDeploymentMaximumBatchSize;
        public int ConfigRollingDeploymentMinimumInstancesInServices;

        public string AppRollingDeploymentBatchType;
        public int AppRollingDeploymentBatchSize;

        /// <summary>
        /// Test if the container is a legacy container which runs in reduced features.
        /// </summary>
        /// <param name="containerName">The name of the container to test</param>
        /// <returns>True if the container is a legacy container</returns>
        public static bool IsLegacyContainer(string containerName)
        {
            if (containerName.ToLower().Contains("(legacy)"))
                return true;

            return false;
        }

        /// <summary>
        /// Optional. Set of RDS Security Groups to which the EC2 security
        /// group of the Beanstalk instance should be added.
        /// </summary>
        public IEnumerable<string> RDSSecurityGroups { get; set; }

        /// <summary>
        /// Optional. Set of EC2-VPC Security Groups to which the EC2 security
        /// group of the Beanstalk instance should be added.
        /// </summary>
        public IEnumerable<string> VPCSecurityGroups { get; set; }

        /// <summary>
        /// Map of vpc security group to a collection of ports used by one or more RDS db instance(s)
        /// using that group. Used to enable us to edit vpc groups so that the deployed Beanstalk app 
        /// can communicate through the specific port to the db (for vpc sg edit, we need the port info).
        /// </summary>
        public Dictionary<string, List<int>> VPCGroupsAndReferencingDBInstances { get; set; }

        public void SetConfigurationOption(string section, string key, string value)
        {
            if (section.Contains(':'))
                ProcessConfigurationOptionSettings(section, key, value);
            else
                throw new ArgumentException("Section is not in recognised format; missing ':' namespace separator(s)");
        }

        #endregion

        #region AWS Clients

        IAmazonElasticBeanstalk _beanstalkClient = null;
        [Browsable(false)]
        public IAmazonElasticBeanstalk BeanstalkClient
        {
            get
            {
                if (this._beanstalkClient == null)
                {
                    var endpoint = RegionEndPoints.GetEndpoint("ElasticBeanstalk");
                    var beanstalkConfig = new AmazonElasticBeanstalkConfig {ServiceURL = endpoint.Url, AuthenticationRegion = endpoint.AuthRegion};
                    this._beanstalkClient = new AmazonElasticBeanstalkClient(Credentials, beanstalkConfig);
                }

                return this._beanstalkClient;
            }
        }

        IAmazonRDS _rdsClient = null;
        [Browsable(false)]
        public IAmazonRDS RDSClient
        {
            get
            {
                if (this._rdsClient == null)
                {
                    var rdsConfig = new AmazonRDSConfig {ServiceURL = RegionEndPoints.GetEndpoint("RDS").Url};
                    this._rdsClient = new AmazonRDSClient(Credentials, rdsConfig);
                }

                return this._rdsClient;
            }
        }

        IAmazonIdentityManagementService _iamClient = null;
        [Browsable(false)]
        public IAmazonIdentityManagementService IAMClient
        {
            get
            {
                if (this._iamClient == null)
                {
                    var iamConfig = new AmazonIdentityManagementServiceConfig {ServiceURL = RegionEndPoints.GetEndpoint(RegionEndPointsManager.IAM_SERVICE_NAME).Url};
                    this._iamClient = new AmazonIdentityManagementServiceClient(Credentials, iamConfig);
                }

                return this._iamClient;
            }
        }

        #endregion

        readonly List<ConfigurationOptionSetting> configOptionSettings = new List<ConfigurationOptionSetting>();

        internal BeanstalkDeploymentEngine()
            : this(new DeploymentObserver())
        {
        }

        internal BeanstalkDeploymentEngine(DeploymentObserver observer)
        {
            VersionLabel = "v" + DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
            Region = "us-east-1";
            Observer = observer;
            CreateNewEnvironment = true;
            SolutionStack = ToolkitAMIManifest.Instance.QueryDefaultWebDeploymentContainer(ToolkitAMIManifest.HostService.ElasticBeanstalk);
            if (string.IsNullOrEmpty(SolutionStack))
                SolutionStack = FALLBACK_DEFAULT_STACK;
        }

        #region DeploymentEngineBase Overrides

        public override void ProcessConfigurationLine(string section, string key, string val, int lineNo)
        {
            if (string.IsNullOrEmpty(section))
                ProcessGeneralKey(key, val, lineNo);
            else if (string.Compare(section, "application", true) == 0)
                ProcessApplicationKey(key, val, lineNo);
            else if (string.Compare(section, "environment", true) == 0)
                ProcessEnvironmentKey(key, val, lineNo);
            else if (string.Compare(section, "container", true) == 0)
                ProcessContainerKey(key, val, lineNo);
            else if (section.Contains(':'))
                ProcessConfigurationOptionSettings(section, key, val);
            else
                Observer.Warn("Unrecognised settings section '{0}', contents ignored.", section);
        }

        // overridden to check specific redeploy mode
        public override int PostProcessConfigurationSettings(bool isRedeploy)
        {
            if (!isRedeploy)
                return PostProcessConfigurationSettingsForNewDeploy();
            else
                return PostProcessConfigurationSettingsForRedeploy();
        }

        int PostProcessConfigurationSettingsForNewDeploy()
        {
            DeploymentMode = DeploymentModes.DeployNewApplication;

            EnsureCredentialsReadyForDeployment();

            return VerifyOptionsForNewUploadAreSet();
        }

        // verifies that environment exists and probes for whether to redeploy
        // new version or prior version
        int PostProcessConfigurationSettingsForRedeploy()
        {
            int ret = DeploymentEngineBase.CONFIGURATION_ERROR;
            DeploymentMode = DeploymentModes.RedeployNewVersion; // assume!
            try
            {
                Observer.Info("...inspecting application '{0}' for environment '{1}' and version '{2}'",
                              ApplicationName,
                              EnvironmentName,
                              VersionLabel);

                EnsureCredentialsReadyForDeployment();

                var response
                    = BeanstalkClient.DescribeEnvironments(new DescribeEnvironmentsRequest()
                    {
                        ApplicationName = this.ApplicationName,
                        EnvironmentNames = new List<string>() { this.EnvironmentName },
                        IncludeDeleted = false
                    });

                Amazon.ElasticBeanstalk.Model.EnvironmentDescription envDescription = null;
                foreach (var env in response.Environments)
                {
                    // Beanstalk throws an exception if we try and update an environment that is not 'available'
                    // but only filter out terminating/terminated here, so we can offer better error to user
                    if (env.Status != EnvironmentStatus.Terminated)
                    {
                        envDescription = env;
                        break;
                    }
                }

                if (envDescription != null)
                {
                    if (string.Compare(envDescription.Status, "ready", true) == 0)
                    {
                        Observer.Status("...environment '{0}' found and available for redeployment (configuration parameters not required for redeployment will be ignored)",
                                        EnvironmentName);
                        this.CreateNewEnvironment = false;

                        // determine specific redeployment mode; do we need to create a new app version or not?
                        var vrnResponse
                            = BeanstalkClient.DescribeApplicationVersions(new DescribeApplicationVersionsRequest() { ApplicationName = this.ApplicationName });
                        foreach (var vrn in vrnResponse.ApplicationVersions)
                        {
                            if (string.Compare(vrn.VersionLabel, VersionLabel, true) == 0)
                            {
                                Observer.Warn("requested version '{0}' already exists for application, assuming request to redeploy that version (upload package will be ignored)", vrn.VersionLabel);
                                DeploymentMode = DeploymentModes.RedeployPriorVersion;
                                break;
                            }
                        }

                        if (DeploymentMode == DeploymentModes.RedeployNewVersion)
                            ret = VerifyOptionsForNewUploadAreSet();
                        else
                            ret = DeploymentEngineBase.SUCCESS;
                    }
                    else
                    {
                        string errMsg = string.Format("...found environment '{0}' but it is not at 'Ready' state (status is '{1}').\r\nRedeployment cannot proceed at this time.",
                                        EnvironmentName, envDescription.Status);
                        Observer.Error(errMsg);
                    }
                }
                else
                {
                    string errMsg = string.Format("...did not find existing environment '{0}' for redeployment of application '{1}', or environment not at 'Ready' state.\r\nRedeployment cannot proceed.",
                                    EnvironmentName, ApplicationName);
                    Observer.Error(errMsg);
                }
            }
            catch (AmazonElasticBeanstalkException e)
            {
                string msg = string.Format("Caught AmazonElasticBeanstalkException probing for existing app {0} with environment {1}, message {2}",
                              ApplicationName, EnvironmentName, e.Message);
                Observer.Error(msg);
            }
            catch (NullReferenceException e)
            {
                Observer.Error(string.Format("Caught exception probing for redeployment mode, {0}", e.Message));
            }

            return ret;
        }

        /// <summary>
        /// For deployments/redeployments that involve uploading new content, verifies that
        /// the appropriate options have been set
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// In the case of Beanstalk S3-based deployments, if a bucket name has not been set
        /// we will request that Beanstalk create a location for us
        /// </remarks>
        int VerifyOptionsForNewUploadAreSet()
        {
            // presence of deployment package will be verified later as we may need to know
            // specified redeployment mode
            if (!UseIncrementalDeployment)
            {
                if (string.IsNullOrEmpty(UploadBucket))
                {
                    if (string.IsNullOrEmpty(RequestDefaultUploadLocation()))
                    {
                        Observer.Error("Argument error: unable to obtain upload bucket location; required for S3-based deployment");
                        return DeploymentEngineBase.CONFIGURATION_ERROR;
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(IncrementalPushRepositoryLocation))
                {
                    Observer.Error("Argument error: missing IncrementalPushRepositoryLocation parameter; required for incremental deployment");
                    return DeploymentEngineBase.CONFIGURATION_ERROR;
                }
            }

            return DeploymentEngineBase.SUCCESS;
        }

        protected override void PreDeploymentValidation() 
        {
            base.PreDeploymentValidation(DeploymentMode != DeploymentModes.RedeployPriorVersion);
        }

        protected override object ExecuteDeployment()
        {
            try
            {
                Observer.Status("..starting deployment to AWS Elastic Beanstalk environment '{0}'", EnvironmentName);

                string versionLabel = null;
                if (!UseIncrementalDeployment)
                {
                    S3Location s3Location = null;
                    if (DeploymentMode != DeploymentModes.DeployPriorVersion)
                    {
                        s3Location = UploadDeploymentPackage(UploadBucket);
                        versionLabel = CreateApplicationVersion(s3Location);
                        this.DeploymentCreatedApplication = true;
                    }
                    else
                        versionLabel = this.VersionLabel;
                }
                else
                {
                    DeployViaDevTools(false);
                    versionLabel = GetLatestApplicationVersionLabel();
                }

                if ((RDSSecurityGroups != null && RDSSecurityGroups.Any()) || (VPCSecurityGroups != null && VPCSecurityGroups.Any()))
                {
                    this.SetupEC2GroupForRDS();
                }

                CreateEnvironment(versionLabel);

                return EnvironmentInstance();
            }
            catch (Exception e)
            {
                var sb = new StringBuilder("Publish to AWS Elastic Beanstalk failed with exception: ");
                sb.Append(e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                    sb.AppendFormat(",\r\nInner Exception Message: {0}", e.InnerException.Message);
                Observer.Error(sb.ToString());

                throw new Exception(String.Format("Publish Failed: {0}", sb.ToString()), e);
            }
        }

        private void SetupEC2GroupForRDS()
        {
            var ec2SecurityGroupName = this.EnvironmentName + Amazon.AWSToolkit.Constants.BEANSTALK_RDS_SECURITY_GROUP_POSTFIX;

            SecurityGroup ec2SecurityGroup = null;
            try
            {
                var filters = new List<Amazon.EC2.Model.Filter>
                {
                    new Amazon.EC2.Model.Filter { Name = "group-name", Values = new List<string>{ec2SecurityGroupName}}
                };
                if (!string.IsNullOrEmpty(VPCId))
                    filters.Add(new Amazon.EC2.Model.Filter { Name = "vpc-id", Values = new List<string>{ VPCId}});
                
                var describeSecurityGroupsRequest = new DescribeSecurityGroupsRequest
                {
                    Filters = filters
                };

                var describeSecurityGroupResult = this.EC2Client.DescribeSecurityGroups(describeSecurityGroupsRequest);
                ec2SecurityGroup = describeSecurityGroupResult.SecurityGroups.FirstOrDefault();
            }
            catch { } // Bury exceptions since the describe call will throw exceptions if the group does not exist.

            string ec2SecurityGroupId;
            if (ec2SecurityGroup == null)
            {
                var createSecurityGroupRequest = new CreateSecurityGroupRequest
                {
                    GroupName = ec2SecurityGroupName,
                    Description = "Security Group created for Beanstalk Environment to give access to RDS instances"
                };
                if (!string.IsNullOrEmpty(VPCId))
                    createSecurityGroupRequest.VpcId = VPCId;

                ec2SecurityGroupId = this.EC2Client.CreateSecurityGroup(createSecurityGroupRequest).GroupId;
                Observer.Status("...created EC2 security group for allowing access to RDS instances '{0}'", ec2SecurityGroupName);

                // allow for possible eventual consistency
                const int maxRetryCount = 10;
                var retry = 0;
                while (ec2SecurityGroup == null && retry < maxRetryCount)
                {
                    retry++;
                    try
                    {
                        ec2SecurityGroup = EC2Client.DescribeSecurityGroups(new DescribeSecurityGroupsRequest {GroupIds = new List<string> {ec2SecurityGroupId}}).SecurityGroups.FirstOrDefault();
                    }
                    catch
                    {
                        Observer.Status("...waiting for creation of Amazon EC2 security group {0}", ec2SecurityGroupName);
                        Thread.Sleep(500);
                    }
                }

                if (ec2SecurityGroup == null)
                {
                    Observer.Status("...failed to confirm EC2 security group creation after {0} attempts; continuing with deployment", maxRetryCount);
                }
            }
            else
            {
                ec2SecurityGroupId = ec2SecurityGroup.GroupId;
            }

            try
            {
                this.EC2Client.AuthorizeSecurityGroupIngress(new AuthorizeSecurityGroupIngressRequest
                {
                    GroupId = ec2SecurityGroupId,
                    IpPermissions = new List<IpPermission>
                {
                    new IpPermission
                    {
                        FromPort = 80,
                        ToPort = 80,
                        IpProtocol = "tcp",
                        IpRanges = new List<string>{"0.0.0.0/0"}
                    }
                }
                });
            }
            catch (Exception e)
            {
                Observer.Status("...error opening port 80 in security group '{0}': {1}", ec2SecurityGroupName, e.Message);
            }

            if (RDSSecurityGroups.Any())
            {
                foreach (var rdsSecurityGroup in RDSSecurityGroups)
                {
                    try
                    {
                        var authIngressRequest = new AuthorizeDBSecurityGroupIngressRequest
                        {
                            DBSecurityGroupName = rdsSecurityGroup, 
                            EC2SecurityGroupId = ec2SecurityGroupId, 
                            EC2SecurityGroupOwnerId = ec2SecurityGroup.OwnerId
                        };

                        // can't output to console as message potentially out-of-order now we're in a thread
                        Observer.LogOnly("...adding EC2 security group '{0}' to RDS security group '{1}'", ec2SecurityGroupName, rdsSecurityGroup);
                        RDSClient.AuthorizeDBSecurityGroupIngress(authIngressRequest);
                    }
                    catch (AmazonRDSException e)
                    {
                        // can't output to console as message potentially out-of-order now we're in a thread
                        if (e is AuthorizationAlreadyExistsException)
                            Observer.Status("......EC2 security group '{0}' already has ingress permissions in RDS security group '{1}'",
                                            ec2SecurityGroupName, rdsSecurityGroup);
                        else
                            Observer.Status("......caught AmazonRDSException whilst adding EC2 security group '{0}' to RDS security group '{1}', skipped - {2}",
                                        ec2SecurityGroupName, rdsSecurityGroup, e.Message);
                    }
                }
            }

            if (VPCSecurityGroups.Any())
            {
                foreach (var vpcSecurityGroup in VPCSecurityGroups)
                {
                    try
                    {
                        var ipPermissions = new List<IpPermission>();
                        if (VPCGroupsAndReferencingDBInstances.ContainsKey(vpcSecurityGroup))
                        {
                            // unlikely but not impossible that multiple instances might be using
                            // different ports in the same group, but we'd better handle it
                            var ports = VPCGroupsAndReferencingDBInstances[vpcSecurityGroup];
                            foreach (var p in ports)
                            {
                                var ipPermission = new IpPermission
                                {
                                    IpProtocol = "tcp",
                                    FromPort = p,
                                    ToPort = p,
                                    UserIdGroupPairs = new List<UserIdGroupPair>
                                    {
                                        new UserIdGroupPair { GroupId = ec2SecurityGroupId }
                                    }
                                };

                                ipPermissions.Add(ipPermission);
                            }
                        }
                        else
                        {
                            // don't think we'll ever get to here without the db port; rather than 
                            // open up all ports, log the fact we have skipped this. The user can always
                            // manually edit the group - better safe than insecure
                            Observer.Info("...skipped updating of EC2-VPC security group '{0}' to allow deployed application access; not able to determine database port.", vpcSecurityGroup);
                        }

                        if (ipPermissions.Any())
                        {
                            var authIngressRequest = new AuthorizeSecurityGroupIngressRequest
                            {
                                GroupId = vpcSecurityGroup,
                                IpPermissions = ipPermissions
                            };

                            // can't output to console as message potentially out-of-order now we're in a thread
                            Observer.LogOnly("...adding EC2 security group '{0}' to EC2-VPC security group for DB instance '{1}'", ec2SecurityGroupId, vpcSecurityGroup);
                            EC2Client.AuthorizeSecurityGroupIngress(authIngressRequest);
                        }
                    }
                    catch (AmazonEC2Exception e)
                    {
                        // can't output to console as message potentially out-of-order now we're in a thread
                        Observer.Status("......caught AmazonEC2Exception whilst adding EC2 security group '{0}' to EC2-VPC security group '{1}', skipped - {2}",
                                    ec2SecurityGroupId, vpcSecurityGroup, e.Message);
                    }
                }
            }

            var existingOption = configOptionSettings.FirstOrDefault(x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "SecurityGroups"));
            if (existingOption == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "SecurityGroups",
                    Value = this.LaunchIntoVPC ? ec2SecurityGroupId : ec2SecurityGroupName
                });
            }
            else
            {
                existingOption.Value += "," + (this.LaunchIntoVPC ? ec2SecurityGroupId : ec2SecurityGroupName);
            }
        }

        protected override void ExecuteUpdateStack()
        {
            Observer.Warn("Update stack is not valid for AWS Elastic Beanstalk environments.");
        }

        protected override object ExecuteRedeployment()
        {
            try
            {
                Observer.Status("...starting redeployment to AWS Elastic Beanstalk environment '{0}'", EnvironmentName);

                this.DeploymentCreatedApplication = false;
                if (!UseIncrementalDeployment)
                {
                    S3Location s3Location = null;
                    if (DeploymentMode != DeploymentModes.RedeployPriorVersion)
                        s3Location = UploadDeploymentPackage(UploadBucket);

                    CreateApplicationVersion(s3Location);
                }

                if ((RDSSecurityGroups != null && RDSSecurityGroups.Any()) || (VPCSecurityGroups != null && VPCSecurityGroups.Any()))
                {
                    this.SetupEC2GroupForRDS();
                }

                if (!this.CreateNewEnvironment)
                {
                    if (UseIncrementalDeployment)
                        DeployViaDevTools(true);
                    else
                        UpdateEnvironment();
                }
                else
                {
                    if (UseIncrementalDeployment)
                    {
                        DeployViaDevTools(false);
                        VersionLabel = GetLatestApplicationVersionLabel();
                    }

                    CreateEnvironment(VersionLabel);
                }

                return EnvironmentInstance();
            }
            catch (Exception e)
            {
                StringBuilder sb = new StringBuilder("Publish to AWS Elastic Beanstalk failed with exception: ");
                sb.Append(e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                    sb.AppendFormat(",\r\nInner Exception Message: {0}", e.InnerException.Message);
                Observer.Error(sb.ToString());

                throw new Exception(String.Format("Publish Failed: {0}", sb.ToString()), e);
            }
        }

        public override int WaitForCompletion()
        {
            TimeSpan THIRTY_SECONDS = new TimeSpan(0, 0, 30);

            for (/* LOOP */ ; /* FOR */ ; /* EVAR */ )
            {
                Thread.Sleep(THIRTY_SECONDS);
                var info = GetEnvironmentInfo();
                if (info != null)
                {
                    if (string.Compare(info.Status, "READY", true) == 0)
                    {
                        Observer.Status("Application deployment completed; environment health is {0}", info.Health);
                        Observer.Info("URL is {0}", info.CNAME);
                        return SUCCESS;
                    }
                    else if (string.Compare(info.Status, "TERMINATING", true) == 0 
                                || string.Compare(info.Status, "TERMINATED", true) == 0)
                    {
                        Observer.Error("Application deployment failed; environment is currently at status: {0}", info.Status);
                        return DEPLOYMENT_FAILED;
                    }
                }
            }
        }

        #endregion

        #region via-S3 deployment support

        /// <summary>
        /// Called if user has not specified an upload bucket; request a default one from Beanstalk
        /// </summary>
        /// <returns>The name of the bucket on success; also sets UploadBucket property</returns>
        string RequestDefaultUploadLocation()
        {
            try
            {
                Observer.Status("...upload bucket not specified; requesting default location from service");
                var response = BeanstalkClient.CreateStorageLocation();
                UploadBucket = response.S3Bucket;
                Observer.Status("......service responded with bucket name '{0}'", UploadBucket);

                return UploadBucket;
            }
            catch (AmazonElasticBeanstalkException e)
            {
                Observer.Error("Caught AmazonElasticBeanstalkException exception requesting default upload location, '{0}'", e.Message);
            }
            catch (Exception e)
            {
                Observer.Error("Caught exception requesting default upload location, '{0}'", e.Message);
            }

            return null;
        }

        // opportunity to make this common
        S3Location UploadDeploymentPackage(string bucketName)
        {
            // can't use deploymentPackage directly as vs2008/vs2010 pass different names (vs08 already has version in it),
            // so synthesize one
            string key = string.Format("{0}/AWSDeploymentArchive_{0}_{1}{2}",
                                        ApplicationName.Replace(' ', '-'),
                                        VersionLabel.Replace(' ', '-'),
                                        Path.GetExtension(DeploymentPackage));

            var fileInfo = new FileInfo(DeploymentPackage);

            if (!PrepareUploadBucket(UploadBucket))
                throw new Exception("Detected error in deployment bucket preparation; abandoning deployment");

            Observer.Status("....uploading application deployment package to Amazon S3");
            Observer.Info("......uploading from file path {0}, size {1} bytes", DeploymentPackage, fileInfo.Length);

            var config = new TransferUtilityConfig() { DefaultTimeout = Amazon.AWSToolkit.Constants.DEFAULT_S3_TIMEOUT };
            TransferUtility transfer = new TransferUtility(S3Client, config);

            var request = new TransferUtilityUploadRequest()
            {
                BucketName = bucketName,
                Key = key,
                FilePath = DeploymentPackage,
            };
            request.UploadProgressEvent += this.UploadProgress;
            transfer.Upload(request);

            return new S3Location() { S3Bucket = bucketName, S3Key = key };
        }

        string CreateApplicationVersion(S3Location s3Location)
        {
            switch (DeploymentMode)
            {
                case DeploymentModes.DeployNewApplication:
                    Observer.Status("....creating application '{0}'", ApplicationName);
                    break;
                case DeploymentModes.RedeployNewVersion:
                    Observer.Status("....creating new version '{0}' for application '{1}", VersionLabel, ApplicationName);
                    break;
                case DeploymentModes.RedeployPriorVersion:
                case DeploymentModes.DeployPriorVersion:
                    return this.VersionLabel;
            }

            var request = new CreateApplicationVersionRequest()
            {
                ApplicationName = this.ApplicationName,
                Description = this.ApplicationDescription,
                AutoCreateApplication = !IsRedeployment,
                VersionLabel = this.VersionLabel,
                SourceBundle = s3Location
            };
            BeanstalkClient.CreateApplicationVersion(request);

            return this.VersionLabel;
        }

        /// <summary>
        /// Deploys the specified application version to an existing environment
        /// </summary>
        void UpdateEnvironment()
        {
            Observer.Status("...requesting update of environment '{0}' with application version '{1}'", EnvironmentName, VersionLabel);
            var optionsToRemove = new List<OptionSpecification>();

            if (Enable32BitApplications != null && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:container:dotnet:apppool" && x.OptionName == "Enable 32-bit Applications")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:container:dotnet:apppool",
                    OptionName = "Enable 32-bit Applications",
                    Value = (Enable32BitApplications.GetValueOrDefault()).ToString()
                });
            }
            if (!IsSingleInstanceEnvironmentType && !string.IsNullOrEmpty(ApplicationHealthcheckPath) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:application" && x.OptionName == "Application Healthcheck URL")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:application",
                    OptionName = "Application Healthcheck URL",
                    Value = ApplicationHealthcheckPath
                });

                optionsToRemove.Add(new OptionSpecification()
                {
                    Namespace = "aws:elb:healthcheck",
                    OptionName = "Target"
                });
            }

            var request = new UpdateEnvironmentRequest()
            {
                EnvironmentName = this.EnvironmentName,
                VersionLabel = this.VersionLabel,
				OptionSettings = this.configOptionSettings,
                OptionsToRemove = optionsToRemove
            };

            BeanstalkClient.UpdateEnvironment(request);
        }

        void UpdateChangedEnvironmentSettings()
        {
            // temp bodge to get around fact we can't update config and version at the same time (yet)
            if (configOptionSettings.Count > 0)
            {
                Observer.Status("...requesting update of environment '{0}' for new configuration settings", EnvironmentName);
                var updateResponse = BeanstalkClient.UpdateEnvironment(new UpdateEnvironmentRequest() { EnvironmentName = this.EnvironmentName, OptionSettings = configOptionSettings });

                var request = new DescribeEnvironmentsRequest() { ApplicationName = this.ApplicationName, EnvironmentNames = new List<string>() { this.EnvironmentName } };

                Observer.Info("......waiting for environment to resume 'Ready' state with new configuration settings");
                DescribeEnvironmentsResponse response = null;
                do
                {
                    Thread.Sleep(500);
                    response = BeanstalkClient.DescribeEnvironments(request);
                }
                while (string.Compare(response.Environments[0].Status, "ready", true) != 0);
            }
        }

        #endregion

        #region via-DevTools deployment support

        /// <summary>
        /// Make sure the MSDeploy.ObjectResolver.filePath attribute is in the archive.xml file.
        /// This doesn't appear to be added until the archive is zipped.  Without this web.config transformations
        /// don't workk.
        /// </summary>
        /// <param name="deploymentLocation"></param>
        void FixArchive(string deploymentLocation)
        {
            var archiveFilePath = Path.Combine(deploymentLocation, "archive.xml");
            if (!File.Exists(archiveFilePath))
                return;

            var content = File.ReadAllText(archiveFilePath);
            if (!content.Contains("MSDeploy.ObjectResolver.dirPath=\"Microsoft.Web.Deployment.DirPathObjectResolver\">"))
                return;

            content = content.Replace("MSDeploy.ObjectResolver.dirPath=\"Microsoft.Web.Deployment.DirPathObjectResolver\">",
                "MSDeploy.ObjectResolver.dirPath=\"Microsoft.Web.Deployment.DirPathObjectResolver\" MSDeploy.ObjectResolver.filePath=\"Microsoft.Web.Deployment.FilePathObjectResolver\">");

            File.WriteAllText(archiveFilePath, content);
        }

        /// <summary>
        /// Deploys incrementally to either a new application or an existing environment.
        /// </summary>
        /// <param name="includeEnvironment"></param>
        protected void DeployViaDevTools(bool includeEnvironment)
        {
            if (DeploymentMode == DeploymentModes.RedeployNewVersion)
            {
                // work around Beanstalk api restriction where new config settings have to be
                // done separately from version update; noop if nothing altered
                UpdateChangedEnvironmentSettings();
            }

            long start = DateTime.Now.Ticks;
            Observer.Status("...starting incremental deployment to environment '{0}'", EnvironmentName);

            FixArchive(DeploymentPackage);
            FileRepositoryBuilder builder = new FileRepositoryBuilder();

            Observer.Info("...staging deployment from work folder {0}", DeploymentPackage);
            builder.SetWorkTree(DeploymentPackage);

            Observer.Info("...commit repository root set at {0}", IncrementalPushRepositoryLocation);
            builder.SetGitDir(Path.Combine(IncrementalPushRepositoryLocation, ".git"));

            var rep = builder.Build();
            bool initialUpload = false;
            if (!((FileBasedConfig)rep.GetConfig()).GetFile().Exists())
            {
                rep.Create();
                initialUpload = true;
            }

            var git = new Git(rep);
            git.Add().AddFilepattern(".").Call();
            string commitMsg;
            if (!string.IsNullOrEmpty(this.ApplicationDescription))
                commitMsg = this.ApplicationDescription;
            else
                commitMsg = "Incremental Deployment: " + DateTime.Now.ToString();
            var commitResults = git.Commit().SetAll(true).SetMessage(commitMsg).Call();

            string url = getRemoteURL(includeEnvironment);
            var pushCommand = git.Push().SetRemote(url).SetForce(true).Add("master");
            pushCommand.SetTimeout(20 * 60); // Set timeout to 20 minutes
            var results = pushCommand.Call();

            CheckForErrorsPushing(results);
            var oldTree = GetPrePushTree(git, results);

            if (initialUpload || oldTree == null)
            {
                Observer.Info("...a full deployment of the application content is required");
            }
            else
            {
                var uniqueFiles = new Dictionary<string, string>();
                addFiles(git, oldTree, uniqueFiles);

                if (uniqueFiles.Count != 0)
                {
                    Observer.Status(
                        "   Incremental Changes Deployed\r\n" +
                        "   ----------------------------");

                    if (uniqueFiles.Count <= NUMBER_OF_FILES_CHANGES_TO_SWITCH_TO_ABBREVIATED_MODE)
                        writeUpdatedFiles(uniqueFiles);
                    else
                        writeAbbreviatedFilesMessage(uniqueFiles);

                    Observer.Status("\r\n");
                }
                else
                    Observer.Status("...no changes detected during commit phase");
            }

            Observer.Status("...finished incremental deployment in {0} ms", new TimeSpan(DateTime.Now.Ticks - start).TotalMilliseconds);
        }

        public string GetLatestApplicationVersionLabel()
        {
            var request = new DescribeApplicationVersionsRequest()
            {
                ApplicationName = this.ApplicationName
            };

            var response = this.BeanstalkClient.DescribeApplicationVersions(request);

            var version = response.ApplicationVersions.OrderByDescending(x => x.DateCreated).FirstOrDefault();
            if (version == null)
                return null;

            return version.VersionLabel;
        }

        void CheckForErrorsPushing(Iterable<NGit.Transport.PushResult> results)
        {
            foreach (var result in results)
            {
                string message = result.GetMessages();
                if (string.IsNullOrEmpty(message) || message == "\n")
                    continue;

                throw new ApplicationException(message);
            }
        }

        AbstractTreeIterator GetPrePushTree(Git git, Iterable<NGit.Transport.PushResult> results)
        {
            foreach (var result in results)
            {
                foreach (var rf in result.GetAdvertisedRefs())
                {
                    var tree = GetTreeIterator(git, rf.GetObjectId().Name);
                    if (tree != null)
                        return tree;
                }
            }

            return null;
        }

        void writeAbbreviatedFilesMessage(IDictionary<string, string> files)
        {
            Observer.Status("   Files Added: {0}",
                files.Count(x => NGit.Diff.DiffEntry.ChangeType.ADD.ToString().Equals(x.Value)));

            Observer.Status("   Files Modified: {0}",
                files.Count(x => NGit.Diff.DiffEntry.ChangeType.MODIFY.ToString().Equals(x.Value)));

            Observer.Status("   Files Deleted: {0}",
                files.Count(x => NGit.Diff.DiffEntry.ChangeType.DELETE.ToString().Equals(x.Value)));
        }

        void writeUpdatedFiles(IDictionary<string, string> files)
        {
            foreach (var kvp in files.OrderBy(x => x.Value + "---" + x.Key))
            {
                Observer.Status("   {0} - {1}", kvp.Value, kvp.Key);
            }
        }

        void addFiles(Git git, AbstractTreeIterator oldTree, IDictionary<string, string> files)
        {
            MemoryStream output = new System.IO.MemoryStream();
            var diffCommand = git.Diff();

            diffCommand.SetOldTree(oldTree);
            diffCommand.SetNewTree(GetTreeIterator(git, "master"));
            diffCommand.SetOutputStream(output);
            diffCommand.SetShowNameAndStatusOnly(true);

            var entries = diffCommand.Call();
            foreach (var entry in entries)
            {
                if (entry.GetChangeType() == NGit.Diff.DiffEntry.ChangeType.DELETE)
                    files[entry.GetOldPath()] = entry.GetChangeType().ToString();
                else if (!files.ContainsKey(entry.GetNewPath()))
                    files[entry.GetNewPath()] = entry.GetChangeType().ToString();
            }
        }

        AbstractTreeIterator GetTreeIterator(Git git, string name)
        {
            var db = git.GetRepository();
            ObjectId id = db.Resolve(name);
            if (id == null)
            {
                throw new ArgumentException(name);
            }
            CanonicalTreeParser p = new CanonicalTreeParser();
            ObjectReader or = db.NewObjectReader();
            try
            {
                p.Reset(or, new RevWalk(db).ParseTree(id));
                return p;
            }
            catch
            {
                return null;
            }
            finally
            {
                or.Release();
            }
        }


        string getRemoteURL(bool includeEnvironment)
        {
            var user = new AWSUser();
            var credentialKeys = Credentials.GetCredentials();
            user.AccessKey = credentialKeys.AccessKey;
            user.SecretKey = credentialKeys.SecretKey;
            var request = new AWSElasticBeanstalkRequest();
            request.Host = GetGitPushHost();
            request.Region = RegionEndPoints.SystemName;
            request.Application = ApplicationName;

            if (includeEnvironment)
                request.Environment = EnvironmentName;

            var auth = new AWSDevToolsAuth(user, request);
            var url = auth.DeriveRemote();
            return url.ToString();
        }

        void detectChanges(string repository, string zip, out List<string> adds, out List<string> deletes)
        {
            adds = new List<string>();
            deletes = new List<string>();
            HashSet<string> allFilesInArchive = new HashSet<string>();
            using (ZipFile zipFile = new ZipFile(zip))
            {
                foreach (ZipEntry e in zipFile)
                {
                    if (!e.IsFile)
                        continue;

                    string reproLocation = Path.Combine(repository, e.Name);
                    allFilesInArchive.Add(reproLocation.Replace('/', '\\').ToLower());

                    if (!File.Exists(reproLocation))
                    {
                        adds.Add(reproLocation);
                    }

                    using (var inputStream = zipFile.GetInputStream(e))
                    {
                        writeFile(inputStream, reproLocation);
                    }
                }
            }

            foreach (var file in Directory.GetFiles(repository, "*", SearchOption.AllDirectories))
            {
                if (file.ToLower().Contains(".git"))
                    continue;

                if (!allFilesInArchive.Contains(file.ToLower()))
                {
                    File.Delete(file);
                    deletes.Add(file);
                }
            }
        }

        void writeFile(Stream stream, string reproLocation)
        {
            FileInfo fi = new FileInfo(reproLocation);
            if (!fi.Directory.Exists)
                fi.Directory.Create();

            byte[] buffer = new byte[64 * 1024];
            using (var outputStream = new FileStream(reproLocation, System.IO.FileMode.Create, FileAccess.Write))
            {
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    outputStream.Write(buffer, 0, bytesRead);
                }
            }
        }

        string GetGitPushHost()
        {
            var host = RegionEndPoints.GetEndpoint(GIT_PUSH_SERVICE_NAME).Url;
            int pos = host.IndexOf("://");
            if (pos > 0)
                host = host.Substring(pos + 3);

            if (host.EndsWith("/"))
                host = host.Substring(0, host.Length - 1);

            return host;
        }

        #endregion

        /// <summary>
        /// Helper to return an environment instance
        /// </summary>
        /// <returns></returns>
        Amazon.ElasticBeanstalk.Model.EnvironmentDescription EnvironmentInstance()
        {
            const int maxRetries = 5;
            for (var i = 1; i <= maxRetries; i++)
            {
                try
                {
                    var envDescribeReq = new DescribeEnvironmentsRequest() { ApplicationName = this.ApplicationName, EnvironmentNames = new List<string>() { this.EnvironmentName } };
                    return BeanstalkClient.DescribeEnvironments(envDescribeReq)
                                            .Environments[0];
                }
                catch
                {
                }
                Thread.Sleep(200 * i);
            }

            Observer.Warn("Unable to retrieve the EnvironmentDescription instance corresponding to the deployment after 5 attempts.");
            return null;
        }

        string CreateEnvironment(string versionLabel)
        {
            var isSingleInstanceEnvLaunch = IsSingleInstanceEnvironmentType;

            if (!string.IsNullOrEmpty(EnvironmentType) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:environment" && x.OptionName == "EnvironmentType")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
				{
                    Namespace = "aws:elasticbeanstalk:environment",
                    OptionName = "EnvironmentType",
                    Value = EnvironmentType
				});
            }

            if (!string.IsNullOrEmpty(CustomAmiID) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "ImageId"))  == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "ImageId",
                    Value = CustomAmiID
                });
            }

            if (!string.IsNullOrEmpty(InstanceTypeID) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "InstanceType")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "InstanceType",
                    Value = InstanceTypeID
                });
            }
            if (!string.IsNullOrEmpty(KeyPairName) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "EC2KeyName")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:autoscaling:launchconfiguration",
                    OptionName = "EC2KeyName",
                    Value = KeyPairName
                });
            }
			if (!IsLegacyContainer(SolutionStack))
            {
            	if (!string.IsNullOrEmpty(RoleName) && configOptionSettings.FirstOrDefault(
                	x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "IamInstanceProfile")) == null)
            	{
                	string instanceProfileName = ConfigureRoleAndProfile(RoleName); // same name, but allow for one day may not be
                	// if error, chosen to not use but proceed with launch - user can fix up via environment view later
                	if (!string.IsNullOrEmpty(instanceProfileName))
                	{
                    	configOptionSettings.Add(new ConfigurationOptionSetting()
                    	{
                        	Namespace = "aws:autoscaling:launchconfiguration",
                        	OptionName = "IamInstanceProfile",
                        	Value = instanceProfileName
                    	});
                	}
				}

                if (!string.IsNullOrEmpty(ServiceRoleName) && configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:elasticbeanstalk:environment" && x.OptionName == "ServiceRole")) == null)
                {
                    ConfigureServiceRole(ServiceRoleName);
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:elasticbeanstalk:environment",
                        OptionName = "ServiceRole",
                        Value = ServiceRoleName
                    });
                }
            }

            if (!isSingleInstanceEnvLaunch && !string.IsNullOrEmpty(ApplicationHealthcheckPath) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:application" && x.OptionName == "Application Healthcheck URL")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:application",
                    OptionName = "Application Healthcheck URL",
                    Value = ApplicationHealthcheckPath
                });
            }
            if(this.EnableConfigRollingDeployment)
            {
                if (configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:autoscaling:updatepolicy:rollingupdate" && x.OptionName == "RollingUpdateEnabled")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "RollingUpdateEnabled",
                        Value = "true"
                    });
                }
                if (configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:autoscaling:updatepolicy:rollingupdate" && x.OptionName == "MaxBatchSize")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "MaxBatchSize",
                        Value = this.ConfigRollingDeploymentMaximumBatchSize.ToString()
                    });
                }
                if (configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:autoscaling:updatepolicy:rollingupdate" && x.OptionName == "MinInstancesInService")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:autoscaling:updatepolicy:rollingupdate",
                        OptionName = "MinInstancesInService",
                        Value = this.ConfigRollingDeploymentMinimumInstancesInServices.ToString()
                    });
                }
            }
            if(this.AppRollingDeploymentBatchType != null && this.AppRollingDeploymentBatchSize > 0)
            {
                if (configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:elasticbeanstalk:command" && x.OptionName == "BatchSize")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:elasticbeanstalk:command",
                        OptionName = "BatchSize",
                        Value = this.AppRollingDeploymentBatchSize.ToString()
                    });
                } 
                if (configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:elasticbeanstalk:command" && x.OptionName == "BatchType")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:elasticbeanstalk:command",
                        OptionName = "BatchType",
                        Value = this.AppRollingDeploymentBatchType
                    });
                }
            }
            if (LaunchIntoVPC)
            {
                if (!string.IsNullOrEmpty(VPCId) && configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:ec2:vpc" && x.OptionName == "VPCId")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:ec2:vpc",
                        OptionName = "VPCId",
                        Value = VPCId
                    });
                }
                if (!string.IsNullOrEmpty(InstanceSubnetId) && configOptionSettings.FirstOrDefault(
                    x => (x.Namespace == "aws:ec2:vpc" && x.OptionName == "Subnets")) == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:ec2:vpc",
                        OptionName = "Subnets",
                        Value = InstanceSubnetId
                    });
                }
				if (!isSingleInstanceEnvLaunch)
				{
                	if (!string.IsNullOrEmpty(ELBSubnetId) && configOptionSettings.FirstOrDefault(
                    	x => (x.Namespace == "aws:ec2:vpc" && x.OptionName == "ELBSubnets")) == null)
                	{
                    	configOptionSettings.Add(new ConfigurationOptionSetting()
                    	{
                        	Namespace = "aws:ec2:vpc",
                        	OptionName = "ELBSubnets",
                        	Value = ELBSubnetId
                    	});
                	}
                	if (!string.IsNullOrEmpty(ELBScheme) && configOptionSettings.FirstOrDefault(
                    	x => (x.Namespace == "aws:ec2:vpc" && x.OptionName == "ELBScheme")) == null)
                	{
                    	configOptionSettings.Add(new ConfigurationOptionSetting()
                   	 	{
                        	Namespace = "aws:ec2:vpc",
                        	OptionName = "ELBScheme",
                        	Value = ELBScheme
                    	});
                	}
				}
                var existingSecurityGroupsOption = configOptionSettings.FirstOrDefault(x => (x.Namespace == "aws:autoscaling:launchconfiguration" && x.OptionName == "SecurityGroups"));
                if (!string.IsNullOrEmpty(VPCSecurityGroupId) && existingSecurityGroupsOption == null)
                {
                    configOptionSettings.Add(new ConfigurationOptionSetting()
                    {
                        Namespace = "aws:autoscaling:launchconfiguration",
                        OptionName = "SecurityGroups",
                        Value = VPCSecurityGroupId
                    });
                }
                else
                {
                    existingSecurityGroupsOption.Value += "," + VPCSecurityGroupId;
                }
            }
            if (Enable32BitApplications != null && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:container:dotnet:apppool" && x.OptionName == "Enable 32-bit Applications")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:container:dotnet:apppool",
                    OptionName = "Enable 32-bit Applications",
                    Value = (Enable32BitApplications.GetValueOrDefault()).ToString()
                });
            }
            if (!string.IsNullOrEmpty(TargetRuntime) && configOptionSettings.FirstOrDefault(
                x => (x.Namespace == "aws:elasticbeanstalk:container:dotnet:apppool" && x.OptionName == "Target Runtime")) == null)
            {
                configOptionSettings.Add(new ConfigurationOptionSetting()
                {
                    Namespace = "aws:elasticbeanstalk:container:dotnet:apppool",
                    OptionName = "Target Runtime",
                    Value = TargetRuntime // should be in x.y format
                });
            }

            var request = new CreateEnvironmentRequest()
            {
                ApplicationName = this.ApplicationName,
                EnvironmentName = this.EnvironmentName,
                Description = this.EnvironmentDescription,
                SolutionStackName = this.SolutionStack,
                OptionSettings = configOptionSettings
            };

            if (!string.IsNullOrEmpty(EnvironmentCNAME))
            {
                request.CNAMEPrefix = EnvironmentCNAME;
            }

            string statusMsg;
            if (!string.IsNullOrEmpty(versionLabel))
            {
                request.VersionLabel = versionLabel;
                statusMsg = string.Format("....creating environment '{0}' with application version '{1}'", 
                                          EnvironmentName, versionLabel);
            }
            else
                statusMsg = string.Format("....creating environment '{0}'", EnvironmentName);

            Observer.Status(statusMsg);
            this.DeploymentCreatedEnvironment = true;
            return BeanstalkClient.CreateEnvironment(request).EnvironmentId;
        }

        public EnvironmentDescription GetEnvironmentInfo()
        {
            var request = new DescribeEnvironmentsRequest() { ApplicationName = this.ApplicationName, EnvironmentNames = new List<string>() { this.EnvironmentName } };
            var result = BeanstalkClient.DescribeEnvironments(request);
            if (result.Environments.Count > 0)
                return result.Environments[0];

            return null;
        }

        /// <summary>
        /// Tests the role name for the 'magic default' recognised by Beanstalk and creates/
        /// configures it appropriately. If the user specified a custom role of their own,
        /// we do nothing other than ensure an instance profile of the same name exists.
        /// </summary>
        /// <param name="roleName"></param>
        /// <returns></returns>
        string ConfigureRoleAndProfile(string roleName)
        {
            string instanceProfileName = string.Empty;
            try
            {
                // ensure the default or user-defined role exists
                bool isDefaultRole = roleName == BeanstalkParameters.DefaultRoleName;
                Role role = getRole(roleName);
                if (role == null)
                {
                    if (isDefaultRole)
                    {
                        var ASSUME_ROLE_POLICY 
                            = Amazon.AWSToolkit.Constants.GetIAMRoleAssumeRolePolicyDocument(RegionEndPointsManager.EC2_SERVICE_NAME,
                                                                                             this.RegionEndPoints);
                        var request = new CreateRoleRequest()
                        {
                            RoleName = roleName,
                            Path = "/",
                            AssumeRolePolicyDocument = ASSUME_ROLE_POLICY
                        };
                        role = IAMClient.CreateRole(request).Role;
                    }
                    else
                    {
                        Observer.Warn("Unable to find role with name '{0}'; launch configuration settings for role will be skipped.", roleName);
                        return string.Empty;
                    }
                }

                /* Beanstalk doesn't need this for default role, and we've elected to not modify
                 * permissions on custom roles
                bool logPolicyExists = false;
                try
                {
                    // IAM throws if policy doesn't exist
                    IAMClient.GetRolePolicy(new GetRolePolicyRequest().WithRoleName(roleName).WithPolicyName(BeanstalkParameters.LogPublishingPolicyName));
                    logPolicyExists = true;
                }
                catch (AmazonIdentityManagementServiceException) { }

                if (!logPolicyExists)
                {
                    if (string.IsNullOrEmpty(UploadBucket))
                        RequestDefaultUploadLocation();

                    Policy logPublishingPolicy = new Policy().WithStatements
                    (
                        new Statement(Statement.StatementEffect.Allow)
                            .WithActionIdentifiers(S3ActionIdentifiers.PutObject)
                            .WithResources(new Resource(string.Format("arn:aws:s3:::{0}/*", UploadBucket))) // could further restrain to specific resource path...
                    );
                    string LogPublishingPolicy = logPublishingPolicy.ToJson();
                    IAMClient.PutRolePolicy(new PutRolePolicyRequest()
                                                    .WithRoleName(roleName)
                                                    .WithPolicyName(BeanstalkParameters.LogPublishingPolicyName)
                                                    .WithPolicyDocument(LogPublishingPolicy));
                }
                */

                InstanceProfile ip = getInstanceProfile(roleName);
                if (ip == null)
                {
                    ip = IAMClient.CreateInstanceProfile(new CreateInstanceProfileRequest()
                        {
                            InstanceProfileName = roleName,
                            Path = "/"
                        }).InstanceProfile;
                    IAMClient.AddRoleToInstanceProfile(new AddRoleToInstanceProfileRequest(){InstanceProfileName = roleName, RoleName = roleName});
                }

                instanceProfileName = ip.InstanceProfileName;
            }
            catch (AmazonIdentityManagementServiceException e)
            {
                Observer.Error("Caught AmazonIdentityManagementServiceException whilst setting up role: {0}", e.Message);
            }
            catch (Exception e)
            {
                Observer.Error("Caught Exception whilst setting up role: {0}", e.Message);
            }

            return instanceProfileName;
        }

        void ConfigureServiceRole(string serviceRoleName)
        {
            try
            {
                // ensure the default or user-defined role exists
                bool isDefaultRole = serviceRoleName == BeanstalkParameters.DefaultServiceRoleName;
                Role role = getRole(serviceRoleName);
                if (role == null)
                {
                    if (isDefaultRole)
                    {
                        var ASSUME_ROLE_POLICY 
                            = Amazon.AWSToolkit.Constants.GetIAMRoleAssumeRolePolicyDocument(RegionEndPointsManager.ELASTICBEANSTALK_SERVICE_NAME,
                                                                                             this.RegionEndPoints);
                        var request = new CreateRoleRequest()
                        {
                            RoleName = serviceRoleName,
                            Path = "/",
                            AssumeRolePolicyDocument = ASSUME_ROLE_POLICY
                        };
                        role = IAMClient.CreateRole(request).Role;

                        IAMClient.PutRolePolicy(new PutRolePolicyRequest
                        {
                            PolicyDocument = RolePolicies.DefaultBeanstalkServiceRolePolicy,
                            PolicyName = serviceRoleName + "_VSToolkit-autocreated",
                            RoleName = serviceRoleName
                        });
                    }
                    else
                    {
                        Observer.Warn("Unable to find role with name '{0}'; environment settings for service role will be skipped.", serviceRoleName);
                    }
                }
            }
            catch (Exception e)
            {
                Observer.Error("Caught Exception whilst setting up service role: {0}", e.Message);
            }
        }

        Role getRole(string roleName)
        {
            try
            {
                return IAMClient.GetRole(new GetRoleRequest(){RoleName = roleName}).Role;
            }
            catch (Amazon.IdentityManagement.Model.NoSuchEntityException) { }

            return null;
        }

        InstanceProfile getInstanceProfile(string profileName)
        {
            try
            {
                return IAMClient.GetInstanceProfile(new GetInstanceProfileRequest(){InstanceProfileName = profileName}).InstanceProfile;
            }
            catch (Amazon.IdentityManagement.Model.NoSuchEntityException) { }

            return null;
        }

        #region Configuration File Processing

        private void ProcessGeneralKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case BeanstalkParameters.GeneralSection_IncrementalPushRepository:
                    this.UseIncrementalDeployment = true;
                    this.IncrementalPushRepositoryLocation = val;
                    break;
                case CommonParameters.GeneralSection_Template: // can ignore this, used to select beanstalk as well as cloudformation
                    break;
                case BeanstalkParameters.GeneralSection_SolutionStack: // alias for Container.Type, which is shared across Beanstalk/CloudFormation
                    ProcessContainerKey(CommonParameters.ContainerSection_Type, val, lineNo);
                    break;
                default:
                    Observer.Warn("Unknown general configuration key '{0}', ignored.", key);
                    break;
            }
        }

        private void ProcessApplicationKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case BeanstalkParameters.ApplicationSection_Name:
                    this.ApplicationName = val;
                    break;
                case BeanstalkParameters.ApplicationSection_Description:
                    this.ApplicationDescription = val;
                    break;
                case BeanstalkParameters.ApplicationSection_Version:
                    this.VersionLabel = val;
                    break;
                default:
                    Observer.Warn("Unknown application configuration key '{0}', ignored.", key);
                    break;
            }
        }

        private void ProcessEnvironmentKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case BeanstalkParameters.EnvironmentSection_Name:
                    this.EnvironmentName = val;
                    break;
                case BeanstalkParameters.EnvironmentSection_Description:
                    this.EnvironmentDescription = val;
                    break;
                case BeanstalkParameters.EnvironmentSection_CNAME:
                    this.EnvironmentCNAME = val;
                    break;
                default:
                    Observer.Warn("Unknown environment configuration key '{0}', ignored.", key);
                    break;
            }
        }

        private void ProcessConfigurationOptionSettings(string ns, string key, string value)
        {
            configOptionSettings.Add(new ConfigurationOptionSetting()
            {
                Namespace = ns,
                OptionName = key,
                Value = value
            });
        }

        private void ProcessContainerKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case CommonParameters.ContainerSection_Type:
                    this.SolutionStack = val.Trim(new char[] { '"', ' '});
                    break;
                case BeanstalkParameters.ContainerSection_NotificationEmail:
                    ProcessConfigurationOptionSettings("aws:elasticbeanstalk:sns:topics", "Notification Endpoint", val);
                    break;
                case CommonParameters.ContainerSection_InstanceType:
                    this.InstanceTypeID = val;
                    break;
                case CommonParameters.ContainerSection_AmiID:
                    this.CustomAmiID = val;
                    break;
                default:
                    Observer.Warn("Unknown container configuration key '{0}', ignored.", key);
                    break;
            }
        }

        #endregion

        #region Configuration creation

        /// <summary>
        /// Populates ConfigurationParameterSets with current settings
        /// </summary>
        /// <param name="config"></param>
        protected override void PopulateConfiguration(ConfigurationParameterSets config)
        {
            // common parameters
            if (this.UseIncrementalDeployment && !string.IsNullOrEmpty(this.IncrementalPushRepositoryLocation))
            {
                config.PutParameter(BeanstalkParameters.GeneralSection_IncrementalPushRepository, this.IncrementalPushRepositoryLocation);
            }
            config.PutParameter(CommonParameters.GeneralSection_Template, "ElasticBeanstalk");
            var fi = new FileInfo(this.DeploymentPackage);
            var di = new DirectoryInfo(this.DeploymentPackage);
            if (fi.Exists)
                config.PutParameter(CommonParameters.GeneralSection_DeploymentPackage, fi.Name);
            else if (di.Exists)
                config.PutParameter(CommonParameters.GeneralSection_DeploymentPackage, di.Name);

            // container parameters
            config.PutParameter(BeanstalkParameters.GeneralSection_SolutionStack, this.SolutionStack); // aka Container.Type, but stay with Beanstalk terminology
            config.PutParameter(CommonSectionNames.ContainerSection, CommonParameters.ContainerSection_InstanceType, this.InstanceTypeID);
            config.PutParameter(CommonSectionNames.ContainerSection, CommonParameters.ContainerSection_AmiID, this.CustomAmiID);

            // application parameters
            config.PutParameter(BeanstalkParameters.ApplicationSection, BeanstalkParameters.ApplicationSection_Name, this.ApplicationName);
            config.PutParameter(BeanstalkParameters.ApplicationSection, BeanstalkParameters.ApplicationSection_Description, this.ApplicationDescription);

            // environment parameters
            config.PutParameter(CommonSectionNames.EnvironmentSection, BeanstalkParameters.EnvironmentSection_Name, this.EnvironmentName);
            config.PutParameter(CommonSectionNames.EnvironmentSection, BeanstalkParameters.EnvironmentSection_Description, this.EnvironmentDescription);
            config.PutParameter(CommonSectionNames.EnvironmentSection, BeanstalkParameters.EnvironmentSection_CNAME, this.EnvironmentCNAME);

            // config parameters
            foreach (var configOptionSetting in configOptionSettings)
            {
                if (string.Equals(configOptionSetting.Namespace, "aws:elasticbeanstalk:sns:topics", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(configOptionSetting.OptionName, "Notification Endpoint", StringComparison.OrdinalIgnoreCase))
                {
                    config.PutParameter(CommonSectionNames.ContainerSection, BeanstalkParameters.ContainerSection_NotificationEmail, configOptionSetting.Value);
                }
                else
                {
                    config.PutParameter(configOptionSetting.Namespace, configOptionSetting.OptionName, configOptionSetting.Value);
                }
            }
        }

        /// <summary>
        /// Populates engine with info described by settings
        /// </summary>
        /// <param name="settings"></param>
        protected override void PopulateEngine(Dictionary<string, object> settings)
        {
            this.ApplicationName = settings[APPLICATION_NAME] as string;
            this.EnvironmentName = settings[ENVIRONMENT_NAME] as string;
            this.DeploymentPackage = this.EnvironmentName + ".zip";

            var applications = BeanstalkClient.DescribeApplications(new DescribeApplicationsRequest
            {
                ApplicationNames = new List<string> { this.ApplicationName }
            }).Applications;
            var application = applications.First();
            this.ApplicationDescription = application.Description;

            var environments = BeanstalkClient.DescribeEnvironments(new DescribeEnvironmentsRequest
            {
                ApplicationName = this.ApplicationName,
                EnvironmentNames = new List<string> { this.EnvironmentName },
                IncludeDeleted = false
            }).Environments;
            var environment = environments.First();
            //this.VersionLabel = environment.VersionLabel;
            this.EnvironmentDescription = environment.Description;
            this.EnvironmentCNAME = TrimCNAME(environment.CNAME);

            var resources = BeanstalkClient.DescribeEnvironmentResources(new DescribeEnvironmentResourcesRequest
            {
                EnvironmentName = this.EnvironmentName
            }).EnvironmentResources;

            this.SolutionStack = !string.IsNullOrEmpty(environment.SolutionStackName) 
                ? environment.SolutionStackName : environment.TemplateName;

            var configSettings = BeanstalkClient.DescribeConfigurationSettings(new DescribeConfigurationSettingsRequest
            {
                ApplicationName = this.ApplicationName,
                EnvironmentName = this.EnvironmentName
            }).ConfigurationSettings;
            var configSetting = configSettings.First();

            var options = configSetting.OptionSettings;
            this.InstanceTypeID = GetValue(options, "aws:autoscaling:launchconfiguration", "InstanceType");
            this.KeyPairName = GetValue(options, "aws:autoscaling:launchconfiguration", "EC2KeyName");
            this.ApplicationHealthcheckPath = GetValue(options, "aws:elasticbeanstalk:application", "Application Healthcheck URL");

            this.TargetRuntime = GetValue(options, "aws:elasticbeanstalk:container:dotnet:apppool", "Target Runtime");
            bool enable32BitApps;
            if (bool.TryParse(GetValue(options, "aws:elasticbeanstalk:container:dotnet:apppool", "Enable 32-bit Applications"), out enable32BitApps))
                this.Enable32BitApplications = enable32BitApps;

            var notificationEmailOption = FindOption(options, "aws:elasticbeanstalk:sns:topics", "Notification Endpoint");
            if (notificationEmailOption != null)
            {
                configOptionSettings.Add(notificationEmailOption);
            }

            foreach (var option in FindOptions(options, "aws:elasticbeanstalk:application:environment"))
            {
                configOptionSettings.Add(option);
            }

            var reservations = GetReservations(resources);
            string customAmi;
            if (IsCustomAmi(reservations, out customAmi))
                this.CustomAmiID = customAmi;
        }

        private static string TrimCNAME(string cname)
        {
            cname = cname.Trim();
            cname = cname.TrimEnd(new char[] { '/' });
            int indexOfEB = cname.LastIndexOf(".elasticbeanstalk.com", StringComparison.OrdinalIgnoreCase);
            if (indexOfEB >= 0)
            {
                cname = cname.Substring(0, indexOfEB);
            }
            return cname;
        }

        private List<Reservation> GetReservations(EnvironmentResourceDescription resources)
        {
            var reservations = EC2Client.DescribeInstances(new DescribeInstancesRequest
            {
                InstanceIds = resources.Instances.Select(i => i.Id).ToList()
            }).Reservations;
            return reservations;
        }

        private static IEnumerable<ConfigurationOptionSetting> FindOptions(IEnumerable<ConfigurationOptionSetting> options, string ns)
        {
            var nsOptions = options.Where(o => string.Equals(o.Namespace, ns, StringComparison.OrdinalIgnoreCase)).ToList();
            return nsOptions;
        }

        private static string GetValue(IEnumerable<ConfigurationOptionSetting> options, string ns, string key)
        {
            var option = FindOption(options, ns, key);

            if (option == null)
                throw new InvalidDataException(string.Format(
                    "Can't find option {0}:{1}", ns, key));

            return option.Value;
        }

        private static ConfigurationOptionSetting FindOption(IEnumerable<ConfigurationOptionSetting> options, string ns, string key)
        {
            var option = options.FirstOrDefault(o =>
                string.Equals(o.Namespace, ns, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(o.OptionName, key, StringComparison.OrdinalIgnoreCase));
            return option;
        }

        #endregion
    }
}
