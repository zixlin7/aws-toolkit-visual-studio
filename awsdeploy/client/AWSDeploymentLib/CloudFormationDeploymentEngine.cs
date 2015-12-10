using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using Amazon;
using Amazon.Runtime;
using Amazon.Util;
using Amazon.AWSToolkit;
using Amazon.AutoScaling;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.EC2.Model;
using Amazon.ElasticBeanstalk.Model;
using Amazon.ElasticLoadBalancing;
using Amazon.ElasticLoadBalancing.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using AWSDeploymentCryptoUtility;
using AWSDeploymentHostManagerClient;
using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.Util;

namespace AWSDeployment
{
    public class CloudFormationDeploymentEngine : DeploymentEngineBase
    {
        static Regex paramPattern = new Regex(@"^PARAM\d$");
        const string TEMPLATES_URI = "http://vstoolkit.amazonwebservices.com/CloudFormationTemplates/";

        private const string EVENT_DEPLOY_FAIL_MESSAGE = "Deploy failed";
        private const string EVENT_DIGEST_MISMATCH_MESSAGE = "Digest mismatch";
        private const string EVENT_SUCCESS_MESSAGE = "UpdateAppVersion Completed";

        private const int STATUS_CHECK_INTERVAL = 10000;
        private const int MINUTES_TILL_SUCCESSFUL_UPDATE = 10;

        public const string STACK_NAME = "StackName";

        #region CloudFormation-specific Deployment Properties

        /// <summary>
        /// Override parameter values in the template.
        /// </summary>
        public Dictionary<string, string> TemplateParameters { get; private set; }

        /// <summary><para>
        /// Additional configuration parameters for the deployed application. These will be available as 
        /// appSettings keys in the web.config of the application. The valid keys are 'PARAM1' through 'PARAM5',
        /// 'AWSAccessKey', and 'AWSSecretKey'.</para>
        /// <para>
        /// The AWSAccessKey and AWSSecretKey can be (and as a best practice, should be) distinct from the credentials
        /// used to create the CloudFormation stack and deploy the application.
        /// </para></summary>
        public Dictionary<string, string> EnvironmentProperties { get; set; }

        /// <summary>
        /// Name of the Stack to create or deploy to.
        /// </summary>
        public string StackName { get; set; }

        /// <summary>
        /// CloudFormation template to use in stack creation.
        /// </summary>
        public string Template { get; set; }

        /// <summary>
        /// Template source.
        /// </summary>
        public string TemplateFilename { get; set; }

        /// <summary>
        /// Key to the config file placed in the upload bucket
        /// </summary>
//        public string ConfigFileKey { get; set; }

        /// <summary>
        /// Used to control aspects of creating the CloudFormation stack. 
        /// </summary>
        public CreateStackSettings Settings { get; set; }

        /// <summary>
        /// For a deploy operation, returns the stack id which was created.
        /// </summary>
        public string StackId { get; private set; }

        // key and initial vector for encrypting the configuration file.
        private Byte[] _key = null;
        private Byte[] _iv = null;

        // this is a holder for any container.type parameter; we'll combine this
        // with region to get the default ami id in config post-processing
        private string ContainerType { get; set; }

        #endregion

        #region AWS Clients

        IAmazonCloudFormation _cfClient = null;
        protected IAmazonCloudFormation CloudFormationClient
        {
            get
            {
                if (this._cfClient == null)
                {
                    var cfConfig = new AmazonCloudFormationConfig {ServiceURL = RegionEndPoints.GetEndpoint("CloudFormation").Url};
                    var keys = CredentialKeys;
                    this._cfClient = new AmazonCloudFormationClient(keys.AccessKey, keys.SecretKey, cfConfig);
                }

                return _cfClient;
            }
        }

        IAmazonAutoScaling _asClient = null;
        protected IAmazonAutoScaling AutoScalingClient
        {
            get
            {
                if (this._asClient == null)
                {
                    var asConfig = new AmazonAutoScalingConfig {ServiceURL = RegionEndPoints.GetEndpoint("AutoScaling").Url};
                    var keys = CredentialKeys;
                    this._asClient = new AmazonAutoScalingClient(keys.AccessKey, keys.SecretKey, asConfig);
                }

                return this._asClient;
            }
        }

        IAmazonElasticLoadBalancing _elbClient = null;
        protected IAmazonElasticLoadBalancing ELBClient
        {
            get
            {
                if (this._elbClient == null)
                {
                    var elbConfig = new AmazonElasticLoadBalancingConfig {ServiceURL = RegionEndPoints.GetEndpoint("ELB").Url};
                    var keys = CredentialKeys;
                    this._elbClient = new AmazonElasticLoadBalancingClient(keys.AccessKey, keys.SecretKey, elbConfig);
                }

                return this._elbClient;
            }
        }

        #endregion

        internal CloudFormationDeploymentEngine()
            : this(new DeploymentObserver())
        {
        }

        internal CloudFormationDeploymentEngine(DeploymentObserver observer)
        {
            VersionLabel = "v" + DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
            Region = "us-east-1";
            Observer = observer;
            Settings = new CreateStackSettings();
            TemplateParameters = new Dictionary<string, string>();
            EnvironmentProperties = new Dictionary<string, string>();
            ApplicationHealthcheckPath = "/";
            InitializeKeyMatter();
        }

        #region DeploymentEngineBase Overrides

        public override void ProcessConfigurationLine(string section, string key, string val, int lineNo)
        {
            if (string.IsNullOrEmpty(section))
                ProcessGeneralKey(key, val, lineNo);
            else if (string.Compare(section, "environment", true) == 0)
                ProcessEnvironmentKey(key, val, lineNo);
            else if (string.Compare(section, "settings", true) == 0)
                ProcessSettingsKey(key, val, lineNo);
            else if (string.Compare(section, "container", true) == 0)
                ProcessContainerKey(key, val, lineNo);
            else if (string.Compare(section, "template", true) == 0)
                ProcessTemplateParameterKey(key, val, lineNo);
            else
                Observer.Warn("Unrecognised settings section '{0}', contents ignored.", section);
        }

        public override int PostProcessConfigurationSettings(bool isRedeploy)
        {
            int ret = DeploymentEngineBase.CONFIGURATION_ERROR;
            string templateURI = null;

            string templateLocation = Template;
            string region = Region.Equals("us-east-1") ? "" : Region;

            if (templateLocation.Equals("SingleInstance") || Template.Equals("LoadBalanced"))
            {
                templateURI = String.Format("{0}{1}{2}{3}.template",
                    TEMPLATES_URI, templateLocation, region.Length > 0 ? "-" : "", region);
                Observer.Info("Retrieving standard template {0}", templateLocation);
            }
            else if (templateLocation.StartsWith("http://") || templateLocation.StartsWith("https://"))
            {
                templateURI = templateLocation;
                Observer.Info("Retrieving custom template from {0}", templateLocation);
            }

            if (templateURI != null)
            {
                var request = HttpWebRequest.Create(templateURI) as HttpWebRequest;

                for (int i = 1; i < 4; i++)
                {
                    try
                    {
                        var response = request.GetResponse() as HttpWebResponse;
                        using (var wStream = response.GetResponseStream())
                        {
                            var sr = new StreamReader(wStream);
                            Template = sr.ReadToEnd();
                            Observer.Info("Download complete");

                            if (!string.IsNullOrEmpty(this.ContainerType))
                            {
                                 // override ami id in template from the specified container default
                                string amiOverride 
                                    = ToolkitAMIManifest.Instance.QueryWebDeploymentAMI(ToolkitAMIManifest.HostService.CloudFormation, 
                                                                                        this.Region, 
                                                                                        this.ContainerType);
                                if (!string.IsNullOrEmpty(amiOverride))
                                    this.TemplateParameters["AmazonMachineImage"] = amiOverride;
                            }

                            ret = DeploymentEngineBase.SUCCESS;
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        Observer.Warn("Failed download attempt {0}: {1}", i, e.Message);
                        int delay = (int)Math.Pow(4, i) * 100;
                        System.Threading.Thread.Sleep(delay);
                    }
                }
            }
            else // Assume it is a file path
            {
                if (File.Exists(templateLocation))
                {
                    Observer.Info("Reading custom template from {0}", templateLocation);

                    try
                    {
                        using (Stream fStream = File.OpenRead(templateLocation))
                        {
                            StreamReader sr = new StreamReader(fStream);
                            Template = sr.ReadToEnd().Trim();
                        }
                        ret = DeploymentEngineBase.SUCCESS;
                    }
                    catch (Exception e)
                    {
                        Observer.Error("Error reading file: {0}", e.Message);
                    }
                }
            }

            return ret;
        }

        protected override void ExecuteUpdateStack()
        {
            try
            {
                Observer.Status("...starting updating stack to AWS CloudFormation stack '{0}'", StackName);

                var response = this.CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = StackName });
                if (response.Stacks.Count != 1)
                {
                    Observer.Error("Failed to find existing stack '{0}' to update", StackName);
                    return;
                }

                var existingStack = response.Stacks[0];

                var updateStackRequest = new UpdateStackRequest()
                {
                    StackName = StackName,
                    TemplateBody = Template
                };

                updateStackRequest.Parameters.Add(new Parameter() { ParameterKey = "KeyPair", ParameterValue = KeyPairName });

                foreach (var kv in TemplateParameters)
                {
                    var param = new Parameter() { ParameterKey = kv.Key, ParameterValue = kv.Value };
                    Observer.Info("Template parameter '{0}' = '{1}'", kv.Key, kv.Value);
                    updateStackRequest.Parameters.Add(param);
                }

                // Add pass-through output parameters
                var bucketName = existingStack.Parameters.FirstOrDefault(x => x.ParameterKey == "BucketName");
                if(bucketName == null)
                {
                    Observer.Error("Existing stack '{0}' missing required BucketName parameter.", StackName);
                    return;
                }
                var configFile = existingStack.Parameters.FirstOrDefault(x => x.ParameterKey == "ConfigFile");
                if (configFile == null)
                {
                    Observer.Error("Existing stack '{0}' missing required ConfigFile parameter.", StackName);
                    return;
                }
                var userData = existingStack.Parameters.FirstOrDefault(x => x.ParameterKey == "UserData");
                if (userData == null)
                {
                    Observer.Error("Existing stack '{0}' missing required UserData parameter.", StackName);
                    return;
                }

                updateStackRequest.Parameters.Add(new Parameter() { ParameterKey = "BucketName", ParameterValue = bucketName.ParameterValue });
                updateStackRequest.Parameters.Add(new Parameter() { ParameterKey = "ConfigFile", ParameterValue = configFile.ParameterValue });
                updateStackRequest.Parameters.Add(new Parameter() { ParameterKey = "UserData", ParameterValue = userData.ParameterValue });

                updateStackRequest.Capabilities.Add("CAPABILITY_IAM");

                this.CloudFormationClient.UpdateStack(updateStackRequest);
            }
            catch (Exception e)
            {
                Observer.Error("Update stack to AWS CloudFormation failed with exception: {0}", e.Message);
                throw new Exception(String.Format("Update stack Failed: {0}", e.Message), e);
            }
        }

        /// <summary>
        /// Create a new CloudFormation stack based on the template, and deploy the application to it.
        /// </summary>
        protected override object ExecuteDeployment()
        {
            try
            {
                Observer.Status("..starting deployment to AWS CloudFormation stack '{0}'", StackName);

                if (!PrepareUploadBucket(UploadBucket))
                    throw new Exception("Detected error in deployment bucket preparation; abandoning deployment");

                S3Location s3PackageLocation = UploadDeploymentPackage(UploadBucket, StackName);

                string deploymentConfig = ConstructDeploymentConfig(s3PackageLocation);
                S3Location s3ConfigLocation = UploadDeploymentConfig(deploymentConfig, UploadBucket, StackName);

                var request = new CreateStackRequest()
                {
                    StackName = StackName,
                    TemplateBody = Template
                };

                // Configure the lifecycle of the deployment

                if (!string.IsNullOrEmpty(Settings.SNSTopic))
                    request.NotificationARNs = new List<string>(){ Settings.SNSTopic};

                if (Settings.CreationTimeout > 0)
                    request.TimeoutInMinutes = Settings.CreationTimeout;

                request.DisableRollback = !Settings.RollbackOnFailure;

                // Add template parameters.

                request.Parameters.Add(new Parameter() { ParameterKey = "KeyPair", ParameterValue = KeyPairName });

                foreach (var kv in TemplateParameters)
                {
                    var param = new Parameter() { ParameterKey = kv.Key, ParameterValue = kv.Value };
                    Observer.Info("Template parameter '{0}' = '{1}'", kv.Key, kv.Value);
                    request.Parameters.Add(param);
                }

                // Add pass-through output parameters

                request.Parameters.Add(new Parameter() { ParameterKey = "BucketName", ParameterValue = UploadBucket });
                request.Parameters.Add(new Parameter() { ParameterKey = "ConfigFile", ParameterValue = s3ConfigLocation.S3Key });

                // Put together the EC2 user data

                string userData = ConstructUserData(s3ConfigLocation);
                request.Parameters.Add(new Parameter() { ParameterKey = "UserData", ParameterValue = userData });

                request.Capabilities.Add("CAPABILITY_IAM");

                var response = CloudFormationClient.CreateStack(request);
                StackId = response.StackId;
                Observer.Status("...creating stack '{0}'", StackName);

                return StackInstance();
            }
            catch (Exception e)
            {
                Observer.Error("Publish to AWS CloudFormation failed with exception: {0}", e.Message);
                throw new Exception(String.Format("Publish Failed: {0}", e.Message), e);
            }
        }

        /// <summary>
        /// Deploy the application to an existing stack.
        /// </summary>
        protected override object ExecuteRedeployment()
        {
            Observer.Status("...redeploying application to instance(s) within the AWS CloudFormation stack '{0}'.", StackName);

            try
            {
                Observer.Status("......retrieving stack resources");
                var stackResources = AmazonCloudFormationClientExt.GetStackResources(CloudFormationClient, StackName);

                Dictionary<string, object> fetchedDescribes = new Dictionary<string, object>();

                var instanceIds = AmazonCloudFormationClientExt.GetListOfInstanceIdsForStack(
                    AutoScalingClient, ELBClient, stackResources, fetchedDescribes);

                Observer.Info("......found {0} instances associated with the stack", instanceIds.Count);

                if (instanceIds != null && instanceIds.Count > 0)
                {
                    var describeInstancesRequest = new DescribeInstancesRequest() { InstanceIds = instanceIds.ToList() };
                    var describeInstanceResponse = EC2Client.DescribeInstances(describeInstancesRequest);

                    UpdateInstances(describeInstanceResponse.Reservations);
                }
                else
                    Observer.Warn("...unable to find any running instances within stack; redeployment cancelled.");

                return StackInstance();
            }
            catch (Exception e)
            {
                Observer.Error("Error during redeployment: {0}", e.Message);
                throw new Exception(String.Format("Redeployment failed: {0}", e.Message), e);
            }
        }

        protected override void PreDeploymentValidation()
        {
            base.PreDeploymentValidation();
        }

        protected override void PreDeploymentValidation(bool requirePackage)
        {
            base.PreDeploymentValidation(requirePackage);

            // validate CloudFormation-specific parameters
            if (string.IsNullOrEmpty(UploadBucket))
                throw new ArgumentException("UploadBucket");
            if (string.IsNullOrEmpty(StackName))
                throw new ArgumentException("StackName");
            if (!IsRedeployment)
            {
                if (string.IsNullOrEmpty(KeyPairName))
                    throw new ArgumentException("KeyPair");
                if (string.IsNullOrEmpty(Template))
                    throw new ArgumentException("Template");

                JsonData data = null;
                try
                {
                    data = JsonMapper.ToObject(this.Template);
                }
                catch
                {
                    throw new ApplicationException("Template is not a valid Json document.");
                }

                var parameters = data["Parameters"];
                if (parameters == null)
                    throw new ApplicationException("Invalid template, no parameters defined");

                CheckTemplateForParameter(parameters, "InstanceType");
                CheckTemplateForParameter(parameters, "KeyPair");
                CheckTemplateForParameter(parameters, "SecurityGroup");
                CheckTemplateForParameter(parameters, "BucketName");
                CheckTemplateForParameter(parameters, "ConfigFile");
                CheckTemplateForParameter(parameters, "AmazonMachineImage");
                CheckTemplateForParameter(parameters, "UserData");

                var outputs = data["Outputs"];
                if (outputs == null)
                    throw new ApplicationException("Invalid template, no output parameters defined");

                CheckTemplateForOutput(outputs, "URL");
                CheckTemplateForOutput(outputs, "Bucket");
                CheckTemplateForOutput(outputs, "ConfigFile");
                CheckTemplateForOutput(outputs, "VSToolkitDeployed");
            }
        }

        public override int WaitForCompletion()
        {
            TimeSpan THIRTY_SECONDS = new TimeSpan(0, 0, 30);
            bool rollback = false;

            for (/* LOOP */ ; /* FOR */ ; /* EVAR */ )
            {
                System.Threading.Thread.Sleep(THIRTY_SECONDS);
                var info = GetStackInfo();
                if (info != null)
                {
                    if (info.StackStatus.Equals("CREATE_COMPLETE"))
                    {
                        Observer.Status("Application deployment completed.");
                        var url = from output in info.Outputs where output.OutputKey.Equals("URL") select output.OutputValue;
                        if (url != null)
                        {
                            Observer.Info("URL is {0}", url.First());
                        }
                        return 0;
                    }
                    else if (info.StackStatus.Equals("CREATE_FAILED"))
                    {
                        Observer.Error("Application deployment failed: {0}", info.StackStatusReason);
                        return DEPLOYMENT_FAILED;
                    }
                    else if (info.StackStatus.Equals("ROLLBACK_IN_PROGRESS") && !rollback)
                    {
                        Observer.Error("Stack creation being rolled back: {0}", info.StackStatusReason);
                        rollback = true;
                    }
                    else if (info.StackStatus.Equals("ROLLBACK_COMPLETE"))
                    {
                        Observer.Error("Rollback complete.");
                        return DEPLOYMENT_FAILED;
                    }
                    else if (info.StackStatus.Equals("UPDATE_COMPLETE") || info.StackStatus.Equals("UPDATE_COMPLETE_CLEANUP_IN_PROGRESS"))
                    {
                        Observer.Info("Stack update complete");
                        return 0;
                    }
                    else if (info.StackStatus.Equals("UPDATE_ROLLBACK_IN_PROGRESS"))
                    {
                        Observer.Error("Update stack being rolled back: {0}", info.StackStatusReason);
                        rollback = true;
                    }
                    else if (info.StackStatus.Equals("UPDATE_ROLLBACK_FAILED"))
                    {
                        Observer.Error("Rollback of update stack failed: {0}", info.StackStatusReason);
                        return DEPLOYMENT_FAILED;
                    }
                    else if (info.StackStatus.Equals("UPDATE_ROLLBACK_COMPLETE") ||
                        info.StackStatus.Equals("UPDATE_ROLLBACK_COMPLETE_CLEANUP_IN_PROGRESS"))
                    {
                        Observer.Error("Rollback of update stack complete.");
                        return DEPLOYMENT_FAILED;
                    }
                }
            }
        }

        #endregion

        private Stack StackInstance()
        {
            const int maxRetries = 5;
            for (var i = 1; i <= maxRetries; i++)
            {
                try
                {
                    var response = CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = StackName });
                    return response.Stacks[0];
                }
                catch
                {
                }
                Thread.Sleep(200 * i);
            }

            Observer.Warn("Unable to retrieve the Stack instance corresponding to the deployment after 5 attempts.");
            return null;
        }

        private void InitializeKeyMatter()
        {
            var aes = Aes.Create();
            aes.KeySize = 256;
            _key = aes.Key;
            _iv = aes.IV;
        }

        private void CheckTemplateForParameter(JsonData parameters, string name)
        {
            if (parameters[name] == null)
            {
                string message = string.Format("Invalid template, parameter \"{0}\" is required and missing.", name);
                throw new ApplicationException(message);
            }
        }

        private void CheckTemplateForOutput(JsonData outputs, string name)
        {
            CheckTemplateForOutput(outputs, name, null);
        }

        private void CheckTemplateForOutput(JsonData outputs, string name, string message)
        {
            if (outputs[name] == null)
            {
                string finalMessage;
                if (message == null)
                    finalMessage = string.Format("Invalid template, output \"{0}\" is required and missing.", name);
                else
                    finalMessage = message;

                throw new ApplicationException(finalMessage);
            }
        }

        private static void SetRedeploymentUserAgentStringRequestEventHandler(object sender, RequestEventArgs args)
        {
            if (args is WebServiceRequestEventArgs)
            {
                string currentUserAgent = ((WebServiceRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader];
                ((WebServiceRequestEventArgs)args).Headers[AWSSDKUtils.UserAgentHeader] = currentUserAgent + " CloudFormationAppRedeployment";
            }
        }

        void UpdateInstances(List<Reservation> reservations)
        {
            bool newConfigUploaded = false;

            var lastUpdatedTimes = new Dictionary<string, DateTime>();
            try
            {
                var descRequest = new DescribeStacksRequest(){StackName = StackName };
                ((Amazon.Runtime.Internal.IAmazonWebServiceRequest)descRequest).AddBeforeRequestHandler(CloudFormationDeploymentEngine.SetRedeploymentUserAgentStringRequestEventHandler);

                var descResponse = CloudFormationClient.DescribeStacks(descRequest);
                if (descResponse.Stacks.Count != 1)
                    throw new Exception(string.Format("Error, stack '{0}' no longer exists", StackName));

                var bucketOutput = descResponse.Stacks[0].Outputs.FirstOrDefault(x => x.OutputKey == "Bucket");
                if (bucketOutput == null)
                    throw new Exception("Stack missing output paramter 'Bucket'");

                var uploadBucket = bucketOutput.OutputValue;

                if (!PrepareUploadBucket(uploadBucket))
                    throw new Exception("Detected error in deployment bucket preparation; abandoning update");

                S3Location s3PackageLocation = UploadDeploymentPackage(uploadBucket, StackName);

                bool containsOldHostManagers = false;
                foreach (var reservation in reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        var hostManagerVersion = GetHostManagerVersion(reservation, instance);

                        // If this is a not the original host manager find the latest timestamp of an event.
                        if (hostManagerVersion != DeploymentConstants.HOST_MANAGER_VERSION_V1)
                        {
                            // Search for events in the last day for the most recent event.
                            var events = GetHostManagerEvents(reservation, instance, DateTime.Now.AddDays(-1)).OrderByDescending(x => x.Timestamp).ToList();
                            if (events.Count > 0)
                                lastUpdatedTimes[instance.InstanceId] = events[0].Timestamp;
                            else
                                lastUpdatedTimes[instance.InstanceId] = DateTime.Now.AddDays(-1);
                        }
                        else
                        {
                            containsOldHostManagers = true;
                        }

                        HostManagerClientRequest client = new HostManagerClientRequest();
                        client.InstanceId = instance.InstanceId;
                        client.ReservationId = reservation.ReservationId;
                        client.Hostname = instance.PublicIpAddress;

                        if (!newConfigUploaded)
                        {
                            client.TaskName = "SystemInfo";

                            string responseStr = null;
                            int wait = 0;

                            while (responseStr == null)
                            {
                                try
                                {
                                    responseStr = client.SendRequest();
                                }
                                catch (Exception)
                                {
                                    int delay = (int)Math.Pow(4, wait) * 100;
                                    System.Threading.Thread.Sleep(delay);
                                    wait++;
                                    if (wait == 5)
                                    {
                                        throw;
                                    }
                                }
                            }

                            JsonData response = JsonMapper.ToObject(responseStr);
                            JsonData payload = JsonMapper.ToObject((string)response["payload"]);

                            _key = Convert.FromBase64String((string)payload["key"]);
                            _iv = Convert.FromBase64String((string)payload["iv"]);

                            string deploymentConfig = ConstructDeploymentConfig(s3PackageLocation);
                            S3Location s3ConfigLocation = UploadDeploymentConfig(deploymentConfig, uploadBucket, StackName);

                            newConfigUploaded = true;
                        }


                        Observer.Status("......notifying instance {0} of new version", instance.InstanceId);

                        client.TaskName = "UpdateConfiguration";
                        client.SendRequest();
                    }
                }

                if (!containsOldHostManagers)
                {
                    int updated = WaitForRedeployComplete(reservations, lastUpdatedTimes);
                    Observer.Status("...{0} out of {1} instances were updated successfully with new version", updated, lastUpdatedTimes.Count);
                }
                else
                {
                    Observer.Status("...finished updating instances");
                }
            }
            catch (Exception e)
            {
                Observer.Error("Error during update: {0}", e.Message);
                throw;
            }
        }

        private int WaitForRedeployComplete(List<Reservation> reservations, Dictionary<string, DateTime> lastUpdatedTimes)
        {
            Observer.Status("...waiting for instances to be updated");

            var finishedInstances = new HashSet<string>();
            int failedInstances = 0;
            DateTime expireDate = DateTime.Now.AddMinutes(MINUTES_TILL_SUCCESSFUL_UPDATE);

            while (finishedInstances.Count < lastUpdatedTimes.Count)
            {
                foreach (var reservation in reservations)
                {
                    foreach (var instance in reservation.Instances)
                    {
                        if (finishedInstances.Contains(instance.InstanceId))
                            continue;

                        IList<HostManagerEvent> events = GetHostManagerEvents(reservation, instance, lastUpdatedTimes[instance.InstanceId]);

                        foreach (var evnt in events)
                        {
                            if (evnt.Message.StartsWith(EVENT_SUCCESS_MESSAGE))
                            {
                                finishedInstances.Add(instance.InstanceId);
                                Observer.Status("......instance {0} successfully updated", instance.InstanceId);
                                break;
                            }
                            else if (evnt.Message.StartsWith(EVENT_DEPLOY_FAIL_MESSAGE) || evnt.Message.StartsWith(EVENT_DIGEST_MISMATCH_MESSAGE))
                            {
                                finishedInstances.Add(instance.InstanceId);
                                Observer.Error("......instance {0} has failed to update with the following message: {1}", instance.InstanceId, evnt.Message);
                                failedInstances++;
                                break;
                            }
                        }
                    }
                }

                if (finishedInstances.Count < lastUpdatedTimes.Count)
                {
                    if (expireDate < DateTime.Now)
                        throw new Exception(string.Format("Failed to get completed update status from all the instances within a {0} minute period.", MINUTES_TILL_SUCCESSFUL_UPDATE));

                    Thread.Sleep(STATUS_CHECK_INTERVAL);
                }
            }

            return finishedInstances.Count - failedInstances;
        }

        private string GetHostManagerStatus(Reservation reservation, Amazon.EC2.Model.Instance instance)
        {
            int retries = 0;

            while (true)
            {
                try
                {
                    HostManagerClientRequest client = new HostManagerClientRequest();
                    client.InstanceId = instance.InstanceId;
                    client.ReservationId = reservation.ReservationId;
                    client.Hostname = instance.PublicIpAddress;
                    client.TaskName = "Status";

                    var responseBody = client.SendRequest();
                    return responseBody;
                }
                catch (Exception e)
                {
                    retries++;
                    if (retries == 3)
                        throw new Exception(string.Format("Error getting host manager status from instance {0}.", instance.InstanceId), e);
                    else
                        Thread.Sleep(STATUS_CHECK_INTERVAL);
                }
            }
        }

        private string GetHostManagerVersion(Reservation reservation, Amazon.EC2.Model.Instance instance)
        {
            string status = GetHostManagerStatus(reservation, instance);
            JsonData jStatus = JsonMapper.ToObject(status);
            string payload = (string)jStatus["payload"];
            if (string.IsNullOrEmpty(payload))
                return null;

            JsonData jPayload = JsonMapper.ToObject(payload);
            if (jPayload == null)
                return null;

            JsonData jVersions = jPayload["versions"];
            if (jVersions == null)
                return null;

            JsonData jHostManager = jVersions["hostmanager"];
            if (jHostManager == null)
                return null;

            string version = (string)jHostManager["version"];
            return version;
        }

        private IList<HostManagerEvent> GetHostManagerEvents(Reservation reservation, Amazon.EC2.Model.Instance instance, DateTime startTime)
        {
            int retries = 0;

            while (true)
            {
                try
                {
                    HostManagerClientRequest client = new HostManagerClientRequest();
                    client.InstanceId = instance.InstanceId;
                    client.ReservationId = reservation.ReservationId;
                    client.Hostname = instance.PublicIpAddress;
                    client.TaskName = "Events";
                    client.Parameters["StartTime"] = startTime.ToUniversalTime().ToString(Amazon.Util.AWSSDKUtils.ISO8601DateFormat);

                    var responseBody = client.SendRequest();
                    JsonData jResponseBody = JsonMapper.ToObject(responseBody);

                    string payload = (string)jResponseBody["payload"];
                    if (string.IsNullOrEmpty(payload))
                        return new List<HostManagerEvent>();

                    JsonData jPayload = JsonMapper.ToObject(payload);
                    if (jPayload == null)
                        return new List<HostManagerEvent>();

                    JsonData jEvents = jPayload["events"];
                    if (jEvents == null || !jEvents.IsArray)
                        return new List<HostManagerEvent>();

                    var events = new List<HostManagerEvent>();
                    foreach (JsonData jEvent in jEvents)
                    {
                        var evnt = new HostManagerEvent(DateTime.Parse((string)jEvent["timestamp"]), (string)jEvent["source"], (string)jEvent["message"], (string)jEvent["severity"]);
                        events.Add(evnt);
                    }

                    return events;
                }
                catch (Exception e)
                {
                    retries++;
                    if (retries == 3)
                        throw new Exception(string.Format("Error checking update status on instance {0}.", instance.InstanceId), e);
                    else
                        Thread.Sleep(STATUS_CHECK_INTERVAL);
                }
            }

        }

        // opportunity to make this common
        protected S3Location UploadDeploymentPackage(string bucketName, string subFolder)
        {
            Observer.Status("...uploading application deployment package to Amazon S3");
            if (string.IsNullOrEmpty(subFolder))
            {
                Observer.Error("Stack name missing and required for uploading deployment package");
                throw new Exception("Stack name missing and required for uploading deployment package");
            }

            string packageFileKey = string.Concat(subFolder, "/", VersionLabel, "/", Path.GetFileName(DeploymentPackage));

            var fileInfo = new FileInfo(DeploymentPackage);
            Observer.Info("......uploading from file path {0}, size {1} bytes", DeploymentPackage, fileInfo.Length);

            var config = new TransferUtilityConfig() { DefaultTimeout = Amazon.AWSToolkit.Constants.DEFAULT_S3_TIMEOUT };
            TransferUtility transfer = new TransferUtility(S3Client,config);

            var request = new TransferUtilityUploadRequest()
            {
                BucketName = bucketName,
                Key = packageFileKey,
                FilePath = DeploymentPackage
            };
            request.UploadProgressEvent += this.UploadProgress;
            transfer.Upload(request);

            var loc = new S3Location()
            {
                S3Bucket = bucketName,
                S3Key = packageFileKey
            };

            return loc;
        }

        protected S3Location UploadDeploymentConfig(string config, string bucketName, string stackName)
        {
            string s3VersionKey = string.Format("{0}/{1}/{0}.config", stackName, VersionLabel);
            string s3MasterKey = string.Format("{0}/{0}.config", stackName);
            Observer.Info("...uploading configuration to S3 key {0}", s3VersionKey);

            string encodedConfig = CryptoUtil.EncryptToBase64EncodedString(config, _key, _iv);
            string tempFile = Path.GetTempFileName();
            using (StreamWriter writer = new StreamWriter(tempFile))
            {
                writer.Write(encodedConfig);
            }

            S3Client.PutObject(new PutObjectRequest()
            {
                BucketName = bucketName,
                Key = s3VersionKey,
                FilePath = tempFile
            });

            S3Client.CopyObject(new CopyObjectRequest()
            {
                SourceBucket = bucketName,
                SourceKey = s3VersionKey,
                DestinationBucket = bucketName,
                DestinationKey = s3MasterKey
            });

            var loc = new S3Location()
            {
                S3Bucket = bucketName,
                S3Key = s3MasterKey
            };
            return loc;
        }

        protected string ConstructDeploymentConfig(S3Location s3PackageLocation)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);

            writer.WriteObjectStart();
            {
                writer.WritePropertyName("Application");
                writer.WriteObjectStart();
                {
                    writer.WritePropertyName("Environment Properties");
                    writer.WriteObjectStart();
                    {
                        for (int i = 1; i < 6; i++)
                        {
                            string key = String.Format("PARAM{0}", i);
                            if (EnvironmentProperties.ContainsKey(key))
                            {
                                writer.WritePropertyName(key);
                                writer.Write(EnvironmentProperties[key] as string);
                            }
                        }

                        if (EnvironmentProperties.ContainsKey("AWSAccessKey"))
                        {
                            writer.WritePropertyName("AWSAccessKey");
                            writer.Write(EnvironmentProperties["AWSAccessKey"] as string);
                        }

                        if (EnvironmentProperties.ContainsKey("AWSSecretKey"))
                        {
                            writer.WritePropertyName("AWSSecretKey");
                            writer.Write(EnvironmentProperties["AWSSecretKey"] as string);
                        }
                    }
                    writer.WriteObjectEnd();
                }
                writer.WriteObjectEnd();

                writer.WritePropertyName("AWSDeployment");
                writer.WriteObjectStart();
                {
                    writer.WritePropertyName("Application");
                    writer.WriteObjectStart();
                    {
                        writer.WritePropertyName("fullurl");
                        writer.Write(GetUrl(s3PackageLocation));

                        writer.WritePropertyName("headurl");
                        writer.Write(GetUrl(s3PackageLocation, HttpVerb.HEAD));

                        writer.WritePropertyName("digest");
                        writer.Write(CalculateFileDigest(DeploymentPackage));

                        writer.WritePropertyName("Application Healthcheck URL");
                        writer.Write(ApplicationHealthcheckPath);

                    }
                    writer.WriteObjectEnd();

                    writer.WritePropertyName("HostManager");
                    writer.WriteObjectStart();
                    {
                        writer.WritePropertyName("LogPublicationControl");
                        writer.Write(false);
                    }
                    writer.WriteObjectEnd();
                }
                writer.WriteObjectEnd();

                writer.WritePropertyName("Container");
                writer.WriteObjectStart();
                {
                    writer.WritePropertyName("Runtime Options");
                    writer.WriteObjectStart();
                    {
                        writer.WritePropertyName("Target Runtime");
                        if (TargetRuntime.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
                            writer.Write(TargetRuntime);
                        else
                            writer.Write(string.Format("v{0}", TargetRuntime));

                        writer.WritePropertyName("Enable 32-bit Applications");
                        writer.Write(Enable32BitApplications.GetValueOrDefault());
                    }
                    writer.WriteObjectEnd();
                }
                writer.WriteObjectEnd();
            }
            writer.WriteObjectEnd();

            return writer.TextWriter.ToString();
        }

        protected string GetUrl(S3Location fileLocation)
        {
            return GetUrl(fileLocation, HttpVerb.GET);
        }

        protected string GetUrl(S3Location fileLocation, HttpVerb verb)
        {
            return S3Client.GetPreSignedURL(new GetPreSignedUrlRequest()
            {
                BucketName = fileLocation.S3Bucket,
                Key = fileLocation.S3Key,
                Expires = DateTime.Now.AddDays(-1).AddYears(1),
                Verb = verb
            });
        }

        protected static string CalculateFileDigest(string fileName)
        {
            string checksum = null;
            using (BufferedStream bstream = new BufferedStream(File.OpenRead(fileName), 1024 * 1024))
            {
                // Use an MD5 instance to compute the has for the stream
                byte[] hashed = MD5.Create().ComputeHash(bstream);
                StringBuilder sBuilder = new StringBuilder();
                for (int i = 0; i < hashed.Length; i++)
                {
                    sBuilder.Append(hashed[i].ToString("x2"));
                }
                checksum = sBuilder.ToString();
            }

            return checksum;
        }

        protected string ConstructUserData(S3Location configFileLocation)
        {
            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);

            writer.WriteObjectStart();
            {
                writer.WritePropertyName("configuration");
                writer.WriteObjectStart();
                {
                    writer.WritePropertyName("fullurl");
                    writer.Write(GetUrl(configFileLocation));
                }
                writer.WriteObjectEnd();

                writer.WritePropertyName("credentials");
                writer.WriteObjectStart();
                {
                    writer.WritePropertyName("key");
                    writer.Write(Convert.ToBase64String(_key));
                    writer.WritePropertyName("iv");
                    writer.Write(Convert.ToBase64String(_iv));
                }
                writer.WriteObjectEnd();
            }
            writer.WriteObjectEnd();

            return writer.TextWriter.ToString();
        }

        public static CryptoUtil.EncryptionKeyTimestampIntegrator ConstructIntegrator(string instanceID, string reservationID)
        {
            return delegate(string ts)
            {
                return string.Format("{0}{1}{2}", instanceID, reservationID, ts);
            };
        }

        public Stack GetStackInfo()
        {
            var result = CloudFormationClient.DescribeStacks(new DescribeStacksRequest() { StackName = StackName });
            if (result.Stacks.Count > 0)
                return result.Stacks[0];

            return null;
        }

        #region Configuration File Processing

        private void ProcessGeneralKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case CloudFormationParameters.GeneralSection_StackName:
                    this.StackName = val;
                    break;
                case CommonParameters.GeneralSection_Template:
                    this.Template = val; // which template to use depends on configables that may be later in the file.
                    break;
                default:
                    Observer.Warn("Unknown general configuration key '{0}', ignored.", key);
                    break;
            }
        }

        private void ProcessEnvironmentKey(string key, string val, int lineNo)
        {
            if (paramPattern.Matches(key).Count == 1 
                    || key.Equals(CommonParameters.GeneralSection_AWSAccessKey) 
                    || key.Equals(CommonParameters.GeneralSection_AWSSecretKey))
            {
                this.EnvironmentProperties[key] = val;
                return;
            }
            Observer.Warn("Unknown environment configuration key '{0}', ignored.", key);
        }

        private void ProcessTemplateParameterKey(string key, string val, int lineNo)
        {
            // Take these at face. Any validation relative to the actual template should be done by the Deployment object itself.
            this.TemplateParameters[key] = val;
        }

        private void ProcessContainerKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case CommonParameters.ContainerSection_Type: 
                    this.ContainerType = val.Trim(new char[] { '"', ' ' });
                    break;
                default:
                    Observer.Warn("Unknown container configuration key '{0}', ignored.", key);
                    break;
            }
        }

        private void ProcessSettingsKey(string key, string val, int lineNo)
        {
            switch (key)
            {
                case CloudFormationParameters.SettingsSection_SNSTopic:
                    this.Settings.SNSTopic = val;
                    break;
                case CloudFormationParameters.SettingsSection_CreationTimeout:
                    int timeout = 0;
                    if (int.TryParse(val, out timeout))
                        this.Settings.CreationTimeout = timeout;
                    else
                        Observer.Warn(string.Format("Value supplied to {0}.{1} is not an integer.", 
                                      CloudFormationParameters.SettingsSection,
                                      CloudFormationParameters.SettingsSection_CreationTimeout));
                    break;
                case CloudFormationParameters.SettingsSection_RollbackOnFailure:
                    bool rollback = false;
                    if (bool.TryParse(val, out rollback))
                        this.Settings.RollbackOnFailure = rollback;
                    else
                        Observer.Warn(string.Format("Value supplied to {0}.{1} is not a boolean.",
                                        CloudFormationParameters.SettingsSection,
                                        CloudFormationParameters.SettingsSection_RollbackOnFailure));
                    break;
                default:
                    Observer.Warn("Unknown settings configuration key '{0}', ignored.", key);
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Populates ConfigurationParameterSets with current settings
        /// </summary>
        /// <param name="config"></param>
        protected override void PopulateConfiguration(ConfigurationParameterSets config)
        {
            // common parameters
            string templateType = "SingleInstance";
            if (string.IsNullOrEmpty(this.TemplateFilename))
            {
                if (string.Equals(this.Template, "SingleInstance", StringComparison.Ordinal) ||
                    string.Equals(this.Template, "LoadBalanced", StringComparison.Ordinal))
                {
                    templateType = this.Template;
                }
            }
            else
            {
                if (this.TemplateFilename.IndexOf("LoadBalanced", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    templateType = "LoadBalanced";
                }
            }
            config.PutParameter(CommonParameters.GeneralSection_Template, templateType);
            config.PutParameter(CloudFormationParameters.GeneralSection_StackName, this.StackName);

            // environment parameters
            foreach (var kvp in this.EnvironmentProperties)
            {
                config.PutParameter(CommonSectionNames.EnvironmentSection, kvp.Key, kvp.Value);
            }

            // settings parameters
            if (Settings != null)
            {
                config.PutParameter(CloudFormationParameters.SettingsSection, CloudFormationParameters.SettingsSection_SNSTopic, this.Settings.SNSTopic);
                config.PutParameter(CloudFormationParameters.SettingsSection, CloudFormationParameters.SettingsSection_CreationTimeout, this.Settings.CreationTimeout.ToString(CultureInfo.InvariantCulture));
                config.PutParameter(CloudFormationParameters.SettingsSection, CloudFormationParameters.SettingsSection_RollbackOnFailure, this.Settings.RollbackOnFailure.ToString(CultureInfo.InvariantCulture).ToLower());
            }

            // template parameters
            foreach (var templateParam in this.TemplateParameters)
            {
                if (templateParam.Key == "AmazonMachineImage")
                {
                    // if the user is employing one of our latest ami's, see if we
                    // can write the logical Container.Type name instead of ami id
                    var containerName 
                        = ToolkitAMIManifest.Instance.WebDeploymentContainerFromAMI(ToolkitAMIManifest.HostService.CloudFormation, 
                                                                                    this.Region, 
                                                                                    templateParam.Value);
                    if (!string.IsNullOrEmpty(containerName))
                    {
                        config.PutParameter(CommonSectionNames.ContainerSection, CommonParameters.ContainerSection_Type, containerName);
                        continue;
                    }
                }

                config.PutParameter(CommonSectionNames.TemplateSection, templateParam.Key, templateParam.Value);
            }
        }

        /// <summary>
        /// Populates engine with info described by settings
        /// </summary>
        /// <param name="settings"></param>
        protected override void PopulateEngine(Dictionary<string, object> settings)
        {
            this.StackName = settings[STACK_NAME] as string;
            this.DeploymentPackage = this.StackName + ".zip";
            var stacks = CloudFormationClient.DescribeStacks(new DescribeStacksRequest
            {
                StackName = this.StackName
            }).Stacks;
            var stack = stacks.First();

            var parameters = stack.Parameters;
            foreach (var parameter in parameters)
            {
                switch (parameter.ParameterKey)
                {
                    case "KeyPair":
                        this.KeyPairName = parameter.ParameterValue;
                        break;
                    case "BucketName":
                        this.UploadBucket = parameter.ParameterValue;
                        break;
                    case "ConfigFile":
                    case "UserData":
                        // skip these
                        break;
                    default:
                        this.TemplateParameters[parameter.ParameterKey] = parameter.ParameterValue;
                        break;
                }
            }

            this.Template = "SingleInstance";

            var resources = CloudFormationClient.ListStackResources(new ListStackResourcesRequest
            {
                StackName = this.StackName
            }).StackResourceSummaries;

            foreach (var resource in resources)
            {
                switch (resource.ResourceType)
                {
                    case "AWS::AutoScaling::AutoScalingGroup":
                        this.Template = "LoadBalanced";
                        break;
                    case "AWS::ElasticLoadBalancing::LoadBalancer":
                        var load = GetLoadBalancerDetails(resource.PhysicalResourceId);
                        this.ApplicationHealthcheckPath = ParseHealthCheckTarget(load.HealthCheck.Target);
                        break;
                }
            }

            this.Settings.RollbackOnFailure = !stack.DisableRollback;
            if (stack.NotificationARNs != null && stack.NotificationARNs.Count > 0)
            {
                this.Settings.SNSTopic = stack.NotificationARNs.First();
            }
            this.Settings.CreationTimeout = stack.TimeoutInMinutes;

            var instances = GetInstances().ToList();
            var reservations = EC2Client.DescribeInstances(new DescribeInstancesRequest
            {
                InstanceIds = instances
            }).Reservations;

            var cfnConfig = CloudFormationUtil.GetConfig(S3Client, stack, reservations.FirstOrDefault());
            var environmentProperties = cfnConfig["Application"]["Environment Properties"];
            foreach(string key in environmentProperties.PropertyNames)
            {
                var value = environmentProperties[key];
                if (value != null)
                {
                    EnvironmentProperties[key] = value.ToString();
                }
            }
            var containerProperties = cfnConfig["Container"]["Runtime Options"];
            var runtimeValue = containerProperties["Target Runtime"];
            if (runtimeValue != null)
            {
                this.TargetRuntime = runtimeValue.ToString();
            }

            var enable32BitAppsValue = containerProperties["Enable 32-bit Applications"];
            if (enable32BitAppsValue != null)
            {
                bool enable32BitApps;
                if (bool.TryParse(enable32BitAppsValue.ToString(), out enable32BitApps))
                {
                    this.Enable32BitApplications = enable32BitApps;
                }
            }

            string customAmi;
            if (IsCustomAmi(reservations, out customAmi))
                this.TemplateParameters["AmazonMachineImage"] = customAmi;
        }

        private const string defaultHealthCheck = "/";
        private static string ParseHealthCheckTarget(string target)
        {
            if (string.IsNullOrEmpty(target))
                return defaultHealthCheck;

            var colonSplit = target.Split(new char[] { ':' }, 2, StringSplitOptions.None);
            if (colonSplit.Length != 2)
                return defaultHealthCheck;

            string scheme = colonSplit[0];
            string portAndPath = colonSplit[1];

            var slashSplit = portAndPath.Split(new char[] { '/' }, 2, StringSplitOptions.None);
            if (slashSplit.Length != 2)
                return defaultHealthCheck;

            string port = slashSplit[0];
            string path = "/" + slashSplit[1];

            // validate
            if (!string.Equals(scheme, "HTTP", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(port, "80", StringComparison.OrdinalIgnoreCase))
                return defaultHealthCheck;

            return path;
        }

        private IEnumerable<string> GetInstances()
        {
            var resourcesRequest = new DescribeStackResourcesRequest(){StackName = StackName};
            var resourcesResponse = CloudFormationClient.DescribeStackResources(resourcesRequest);
            var stackResources = resourcesResponse.StackResources;

            var fetchedDescribes = new Dictionary<string, object>();
            var instances = AmazonCloudFormationClientExt.GetListOfInstanceIdsForStack(
                this.AutoScalingClient, this.ELBClient, stackResources, fetchedDescribes);
            return instances;
        }

        public Amazon.ElasticLoadBalancing.Model.LoadBalancerDescription GetLoadBalancerDetails(string name)
        {
            var request = new DescribeLoadBalancersRequest() { LoadBalancerNames = new List<string>() { name } };
            var response = this.ELBClient.DescribeLoadBalancers(request);
            if (response.LoadBalancerDescriptions.Count != 1)
                return null;

            var load = response.LoadBalancerDescriptions.First();
            return load;
        }
    }
}
