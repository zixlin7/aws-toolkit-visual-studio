using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

using Amazon;
using Amazon.AWSToolkit;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.Runtime;

namespace AWSDeployment
{
    public abstract class DeploymentEngineBase
    {
        public const int
            SUCCESS = 0,
            CONFIGURATION_ERROR = 1,
            DEPLOYMENT_FAILED = 3;

        public enum DeploymentModes
        {
            /// <summary>
            /// Deploy creating a new stack/environment for the application
            /// </summary>
            DeployNewApplication = 0,
            /// <summary>
            /// Redeploy new version of application to existing stack/environment
            /// </summary>
            RedeployNewVersion = 1,
            /// <summary>
            /// Beanstalk only; redeploy previous version to existing environment
            /// </summary>
            RedeployPriorVersion = 2,
            /// <summary>
            /// Beanstalk only; deploy previous version to existing environment
            /// </summary>
            DeployPriorVersion = 3
        }

        public static string RuntimeFromFramework(string frameworkVersion)
        {
            if (string.IsNullOrEmpty(frameworkVersion))
                throw new ArgumentException();

            if (frameworkVersion.StartsWith("4"))
                return "4.0";
            else
                return "2.0";
        }

        #region Common Deployment Properties

        /// <summary>
        /// The name of a credential profile that can be used to obtain AWS credentials for 
        /// deployment. Deployments will look to obtain credentials from the profile, then 
        /// Credentials property and finally the access and secret key members.
        /// </summary>
        public string AWSProfileName { get; set; }

        /// <summary>
        /// AWS credentials that can be used during deployment.
        /// </summary>
        public AWSCredentials Credentials { get; set; }

        /// <summary>
        /// Access key that can be used, with corresponding AWSSecretKey value,
        /// during deployment.
        /// </summary>
        public string AWSAccessKey { get; set; }

        /// <summary>
        /// Secret key that can be used, with corresponding AWSAccessKey value,
        /// during deployment.
        /// </summary>
        public string AWSSecretKey { get; set; }

        /// <summary>
        /// Deployment will send messages, status, and errors to this object.
        /// </summary>
        public DeploymentObserver Observer { get; set; }

        /// <summary>
        /// Path to the built package to deploy; this can be a packaged zipfile or 
        /// a folder hierarchy of the content
        /// /// </summary>
        public string DeploymentPackage { get; set; }

        /// <summary>
        /// Region to deploy to.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Major version of the .Net runtime required by the application.
        /// Accepts "2", "4", optionally prefixed with "v".
        /// </summary>
        public string TargetRuntime { get; set; }

        /// <summary>
        /// If set to true, the deployment environment will enable 32-bit applications.
        /// </summary>
        public bool? Enable32BitApplications { get; set; }

        /// <summary>
        /// The path from the root of the page in the application to be used for healthcheck pings, if load balancing is enabled.
        /// </summary>
        public string ApplicationHealthcheckPath { get; set; }

        /// <summary>
        /// Name of the key pair to use for EC2 instances; may be created as part of deployment
        /// </summary>
        public string KeyPairName { get; set; }

        /// <summary>
        /// Version label referencing the deployment
        /// </summary>
        public string VersionLabel { get; set; }

        /// <summary>
        /// What kind of deployment or redeployment to perform
        /// </summary>
        public DeploymentModes DeploymentMode { get; set; }

        /// <summary>
        /// If true, we're performing a redeployment to the selected target
        /// </summary>
        public bool IsRedeployment 
        { 
            get { return DeploymentMode != DeploymentModes.DeployNewApplication && DeploymentMode != DeploymentModes.DeployPriorVersion; }
        }

        /// <summary>
        /// S3 Bucket where the deployment materials will be stored.
        /// </summary>
        public string UploadBucket { get; set; }

        /// <summary>
        /// If true, saves the configuration file corresponding to deployment.
        /// </summary>
        public string ConfigFileDestination { get; set; }

        #endregion

        #region AWS Clients

        RegionEndPointsManager.RegionEndPoints _regionEndPoints = null;
        /// <summary>
        /// Return the endpoints object for a given region addressed by system name
        /// </summary>
        [Browsable(false)]
        public RegionEndPointsManager.RegionEndPoints RegionEndPoints
        {
            get
            {
                if (_regionEndPoints == null)
                {
                    _regionEndPoints = RegionEndPointsManager.Instance.GetRegion(Region);
                }

                return _regionEndPoints;
            }
        }

        IAmazonS3 _s3Client = null;
        [Browsable(false)]
        protected IAmazonS3 S3Client
        {
            get
            {
                if (this._s3Client == null)
                {
                    var s3Config = new AmazonS3Config {ServiceURL = RegionEndPoints.GetEndpoint("S3").Url};
                    this._s3Client = new AmazonS3Client(Credentials, s3Config);
                }

                return this._s3Client;
            }
        }

        IAmazonEC2 _ec2Client = null;
        public IAmazonEC2 EC2Client
        {
            get
            {
                if (this._ec2Client == null)
                {
                    var ec2Config = new AmazonEC2Config {ServiceURL = RegionEndPoints.GetEndpoint("EC2").Url};
                    this._ec2Client = new AmazonEC2Client(Credentials, ec2Config);
                }

                return this._ec2Client;
            }
        }

        #endregion

        #region Public API

        internal DeploymentEngineBase()
        {
            DeploymentMode = DeploymentModes.DeployNewApplication;
        }

        /// <summary>
        /// Optional override to process a configuration line entry that the generic reader
        /// does not recognise and which may be deployment service specific.
        /// </summary>
        /// <param name="section">Optional, any .-prefixed name attached to the key that the reader parsed</param>
        /// <param name="key">Name of the configuration key (less any .-prefixed section)</param>
        /// <param name="val">The value parsed for the key</param>
        /// <param name="LineNo">The line in the configuration data that the key/value was declared</param>
        /// <remarks>By default, if not overridden, keys unknown to the base reader are skipped</remarks>
        public virtual void ProcessConfigurationLine(string section, string key, string val, int LineNo)
        {
        }

        /// <summary>
        /// Called when all of the supplied configuration file data has been read
        /// </summary>
        /// <param name="isRedeploy">True if we are operating in redeployment mode</param>
        /// <returns>0 to continue deployment, non-zero on error (stops processing)</returns>
        public virtual int PostProcessConfigurationSettings(bool isRedeploy)
        {
            return SUCCESS;
        }

        /// <summary>
        /// Initiate new deployment of an application, returning the service object representing
        /// the deployment (CloudFormation Stack or Beanstalk Environment)
        /// </summary>
        public object Deploy()
        {
            object serviceContainer;

            try { PreDeploymentValidation(); }
            catch (ArgumentException e)
            {
                Observer.Error("Deployment cannot proceed without the {0} parameter being set.", e.Message);
                throw new ArgumentException(String.Format("Deployment cannot proceed without the {0} parameter being set.", e.Message));
            }

            try
            {
                serviceContainer = ExecuteDeployment();
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new Exception("Unhandled error during deployment: " + e.Message, e);
            }

            try { PostDeploymentValidation(); }
            catch
            {
                // ??? bad engine not following instructions - do we care ???
            }

            return serviceContainer;
        }

        /// <summary>
        /// Initiate redeployment of an application, returning the service object representing
        /// the deployment (CloudFormation Stack or Beanstalk Environment)
        /// </summary>
        public object Redeploy()
        {
            object serviceContainer;

            try { PreDeploymentValidation(); }
            catch (ArgumentException e)
            {
                Observer.Error("Deployment cannot proceed without the {0} parameter being set.", e.Message);
                throw new ArgumentException(String.Format("Deployment cannot proceed without the {0} parameter being set.", e.Message));
            }

            try
            {
                serviceContainer = ExecuteRedeployment();
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new Exception("Unhandled error during deployment", e);
            }

            try { PostDeploymentValidation(); }
            catch
            {
                // ??? bad engine not following instructions - do we care ???
            }

            return serviceContainer;
        }

        /// <summary>
        /// Initiate an update stack
        /// </summary>
        public void UpdateStack()
        {
            try { PreDeploymentValidation(false); }
            catch (ArgumentException e)
            {
                Observer.Error("Update stack cannot proceed without the {0} parameter being set.", e.Message);
                throw new ArgumentException(String.Format("Update stack cannot proceed without the {0} parameter being set.", e.Message));
            }

            try
            {
                ExecuteUpdateStack();
                SaveConfig();
            }
            catch (Exception e)
            {
                throw new Exception("Unhandled error during update stack", e);
            }

            try { PostDeploymentValidation(); }
            catch
            {
                // ??? bad engine not following instructions - do we care ???
            }
        }

        /// <summary>
        /// Override to allow callers to wait until completion of the deployment process,
        /// and return an indicator of success/failure
        /// </summary>
        /// <returns></returns>
        public virtual int WaitForCompletion()
        {
            return 0;
        }

        public static ConfigurationParameterSets CaptureEnvironmentConfig(Dictionary<string, object> settings)
        {
            DeploymentEngineBase engine;
            if (settings.ContainsKey(CloudFormationDeploymentEngine.STACK_NAME))
                engine = new CloudFormationDeploymentEngine();
            else if (settings.ContainsKey(BeanstalkDeploymentEngine.APPLICATION_NAME))
                engine = new BeanstalkDeploymentEngine();
            else
                throw new InvalidOperationException("Cannot determine engine!");

            engine.PopulateEngineBase(settings);
            engine.PopulateEngine(settings);

            var config = engine.GetConfiguration();
            return config;
        }

        #endregion

        public const string ACCOUNT_PROFILE_NAME = "AWSProfileName";
        public const string ACCESS_KEY = "AWSAccessKey";
        public const string SECRET_KEY = "AWSSecretKey";
        public const string REGION = "Region";

        private void PopulateEngineBase(Dictionary<string, object> settings)
        {
            object profileName;
            if (settings.TryGetValue(ACCOUNT_PROFILE_NAME, out profileName) && !string.IsNullOrWhiteSpace(profileName as string))
            {
                SetCredentialsFromProfileName(profileName as string);
            }
            else
            {
                this.AWSAccessKey = settings[ACCESS_KEY] as string;
                this.AWSSecretKey = settings[SECRET_KEY] as string;
            }
            this.Region = settings[REGION] as string;
        }

        public void SetCredentialsFromProfileName(string profileName)
        {
            this.AWSProfileName = profileName;
            Amazon.Runtime.AWSCredentials credentials;
            if (Amazon.Util.ProfileManager.TryGetAWSCredentials(this.AWSProfileName, out credentials))
            {
                this.Credentials = credentials;
            }
        }

        /// <summary>
        /// Populates engine with info described by settings
        /// </summary>
        /// <param name="settings"></param>
        protected virtual void PopulateEngine(Dictionary<string, object> settings)
        {

        }

        protected virtual void PreDeploymentValidation()
        {
            PreDeploymentValidation(true);
        }

        /// <summary>
        /// Implement in derived engines to sanity check service-specific parameters prior
        /// to starting deployment. Derived engine can call the base class implementation
        /// to get validation of common parameters.
        /// </summary>
        /// <remarks>Implementors should throw an ArgumentException for errors</remarks>
        protected virtual void PreDeploymentValidation(bool requirePackage) 
        {
            if (requirePackage && string.IsNullOrEmpty(DeploymentPackage))
                throw new ArgumentException("DeploymentPackage");

            if (string.IsNullOrEmpty(AWSProfileName)
                    && Credentials == null
                    && (string.IsNullOrEmpty(AWSAccessKey) && string.IsNullOrEmpty(AWSSecretKey)))
                throw new InvalidOperationException("No credentials supplied; expected AWSProfileName or Credentials or AWSAccessKey & AWSSecretKey properties to be set.");

            if (Credentials == null)
            {
                if (!string.IsNullOrEmpty(AWSProfileName))
                    SetCredentialsFromProfileName(AWSProfileName);
                else
                    Credentials = new BasicAWSCredentials(AWSAccessKey, AWSSecretKey);
            }
        }

        /// <summary>
        /// Called to allow a derived engine to verify the deployment was succesful
        /// </summary>
        protected virtual void PostDeploymentValidation() { }

        /// <summary>
        /// Required override to perform a new deployment to a service, returning
        /// an instance of the service object (Stack/Environment) containing the
        /// deployed application.
        /// </summary>
        protected abstract object ExecuteDeployment();

        /// <summary>
        /// Required override to perform a redeployment to a service, returning the
        /// service object containing the deployment (Stack/Environment)
        /// </summary>
        protected abstract object ExecuteRedeployment();

        /// <summary>
        /// Required override to perform an update stack
        /// </summary>
        protected abstract void ExecuteUpdateStack();

        private void SaveConfig()
        {
            if (string.IsNullOrEmpty(ConfigFileDestination))
                return;

            Observer.Status("...Determining current configuration");
            var config = GetConfiguration();

            Observer.Status("...Saving configuration file to {0}", ConfigFileDestination);
            DeploymentConfigurationWriter.WriteDeploymentToFile(config, ConfigFileDestination);
        }

        private ConfigurationParameterSets GetConfiguration()
        {
            var config = new ConfigurationParameterSets();

            PopulateBaseConfiguration(config);
            PopulateConfiguration(config);

            return config;
        }

        /// <summary>
        /// Populates ConfigurationParameterSets with current settings
        /// </summary>
        /// <param name="config"></param>
        protected virtual void PopulateConfiguration(ConfigurationParameterSets config)
        {
        }

        private void PopulateBaseConfiguration(ConfigurationParameterSets config)
        {
            // common parameters
            config.PutParameter("Region", this.Region);
            if (!string.IsNullOrEmpty(this.AWSProfileName))
            {
                config.PutParameter(ACCOUNT_PROFILE_NAME, this.AWSProfileName);
            }
            else
            {
                if (this.Credentials != null)
                {
                    var credentialKeys = Credentials.GetCredentials();
                    config.PutParameter("AWSAccessKey", credentialKeys.AccessKey);
                    config.PutParameter("AWSSecretKey", credentialKeys.SecretKey);
                }
                else
                {
                    config.PutParameter("AWSAccessKey", AWSAccessKey);
                    config.PutParameter("AWSSecretKey", AWSSecretKey);
                }
            }

            if (!string.IsNullOrEmpty(this.UploadBucket))
                config.PutParameter("UploadBucket", this.UploadBucket);
            if (!string.IsNullOrEmpty(this.KeyPairName))
                config.PutParameter("KeyPair", this.KeyPairName);

            // container parameters
            if (!string.IsNullOrEmpty(this.ApplicationHealthcheckPath))
                config.PutParameter("Container", "ApplicationHealthcheckPath", this.ApplicationHealthcheckPath);
            if (this.Enable32BitApplications.HasValue)
                config.PutParameter("Container", "Enable32BitApplications", this.Enable32BitApplications.Value.ToString());
            if (!string.IsNullOrEmpty(this.TargetRuntime))
                config.PutParameter("Container", "TargetRuntime", this.TargetRuntime);
        }

        protected bool PrepareUploadBucket(string bucketName)
        {
            bool ret = false;
            Observer.Info("...making sure upload bucket '{0}' exists", bucketName);
            try
            {
                var response = S3Client.PutBucket(new PutBucketRequest() { BucketName = bucketName, BucketRegionName = RegionEndPoints.SystemName });
                ret = true;
            }
            catch (AmazonS3Exception exc)
            {
                if (System.Net.HttpStatusCode.Conflict != exc.StatusCode)
                {
                    Observer.Error("Attempt to create deployment upload bucket caught AmazonS3Exception, StatusCode '{0}', Message '{1}'", exc.StatusCode, exc.Message);
                }
                else
                {
                    // conflict may occur if bucket belongs to another user or if bucket owned by this user but in a different region
                    if (exc.ErrorCode != "BucketAlreadyOwnedByYou")
                        Observer.Error("Unable to use bucket name '{0}'; bucket exists but is not owned by you\r\n...S3 error was '{1}'.", bucketName, exc.Message);
                    else
                    {
                        Observer.Info("..a bucket with name '{0}' already exists and will be used for upload", bucketName);
                        ret = true;
                    }
                }
            }
            catch (Exception exc)
            {
                Observer.Error("Attempt to create deployment upload bucket caught Exception, Message '{0}'", exc.Message);
            }

            return ret;
        }

        string _lastS3UploadMessage;
        protected void UploadProgress(object sender, UploadProgressArgs args)
        {
            int percent = (int)((double)args.TransferredBytes * 100.0 / (double)args.TotalBytes);
            string message = string.Format("......uploaded {0}% of application deployment package", percent);
            if (!string.Equals(message, this._lastS3UploadMessage))
            {
                this._lastS3UploadMessage = message;
                Observer.Progress(message);
            }

            if (args.TransferredBytes >= args.TotalBytes)
                Observer.Status("....upload complete");
        }

        protected bool IsCustomAmi(List<Reservation> reservations, out string customAmi)
        {
            var runningInstances = reservations.SelectMany(r => r.Instances);
            var amis = runningInstances.Select(r => r.ImageId).Distinct().ToList();
            var images = EC2Client.DescribeImages(new DescribeImagesRequest
            {
                ImageIds = amis
            }).Images;

            // sjr_todo 
            // we could do this using the toolkitamimanifest file now, and know 
            // exactly if the image is ours rather than check ownership or hash
            bool areImagesAmazonOwned = images.All(image =>
            {
                if (string.Equals(image.ImageOwnerAlias, "amazon", StringComparison.OrdinalIgnoreCase))
                    return true;
                var ownerHash = GetHash(image.OwnerId);
                if (string.Equals(ownerHash, awsSdkAccountHash, StringComparison.Ordinal))
                    return true;
                return false;
            });

            if (!areImagesAmazonOwned)
            {
                customAmi = images.First().ImageId;
                return true;
            }
            else
            {
                customAmi = null;
                return false;
            }
        }

        protected static string GetHash(string value)
        {
            // step 1, calculate MD5 hash from input
            MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(value);
            byte[] hash = md5.ComputeHash(inputBytes);

            // step 2, convert byte array to hex string
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                sb.Append(hash[i].ToString("X2"));
            }
            return sb.ToString();
        }
        static string awsSdkAccountHash = "3FBBEADAFBC3E89D1606D92BB004B9BD";
    }
}
