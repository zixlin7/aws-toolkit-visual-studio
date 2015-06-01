using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using ThirdParty.Json.LitJson;

using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;

using log4net;
using AWSDeploymentCryptoUtility;

namespace AWSDeploymentHostManager
{
    public class HostManagerConfig
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(HostManagerConfig));

        private const int USER_DATA_LOAD_MAX_ATTEMPTS = 5;
        
        private const string
            CONFIG_EC2_INSTANCE_ID     = "ec2InstanceId",
            CONFIG_EC2_RES_ID          = "ec2ReservationId",
            CONFIG_ASP_LOG_LOCATION    = "aspLogLocation",
            CONFIG_ENABLE_32_BIT       = "Enable 32-bit Applications",
            CONFIG_TARGET_RUNTIME      = "Target Runtime",
            CONFIG_REQ_PUB_TO_EXPORT   = "Require Publication To Export",
            CONFIG_EVENT_LOG_PUB       = "Publish Event Logs";

        private static System.Version defaultTargetRuntime = new System.Version(4, 0);
        internal static int EventsNeededToPublishEventLog = 100;

        private static string _ec2UserData;
        private static string _waitConditionSignalURL;
        private static Dictionary<string, string> _parameters = new Dictionary<string, string>();

        private static bool _metaDataUserIsConfigured;
        private static string _iamUserAccessKey;
        private static string _iamUserSecretKey;
        private static string _logicalResourceId;
        private static string _cloudFormationEndpoint;
        private static string _stackName;


        private const string
            EC2_METADATA_URL = "http://169.254.169.254/latest/meta-data/",
            EC2_USER_DATA_URL = "http://169.254.169.254/latest/user-data",
            EC2_METADATA_INSTANCE_ID = "instance-id",
            EC2_METADATA_RESERVATION_ID = "reservation-id";

        public static readonly string
            SECTION_APPLICATION = "Application",
            SECTION_ENVIRONMENT = "Environment Properties",
            SECTION_AWSDEPLOYMENT = "AWSDeployment",
            SECTION_PROVISIONING = "Provisioning",
            SECTION_HOSTMANAGER = "HostManager",
            SECTION_CONTAINER = "Container",
            SECTION_CREDENTIALS = "credentials",
            SECTION_CONFIGURATION = "configuration",
            SECTION_RUNTIME = "Runtime Options";

        public static readonly string
            CONFIG_PARAM1 = "PARAM1",
            CONFIG_PARAM2 = "PARAM2",
            CONFIG_PARAM3 = "PARAM3",
            CONFIG_PARAM4 = "PARAM4",
            CONFIG_PARAM5 = "PARAM5",
            CONFIG_CONNECTION_STRING = "JDBC_CONNECTION_STRING",
            CONFIG_SECRET_KEY = "AWS_SECRET_KEY",
            CONFIG_ACCESS_KEY = "AWS_ACCESS_KEY_ID",
            CONFIG_S3KEY = "s3key",
            CONFIG_S3BUCKET = "s3bucket",
            CONFIG_S3VERSION = "s3version",
            CONFIG_BUNDLE_SIZE = "MaxSourceBundleSizeInBytes",
            CONFIG_QUERY_PARAMS = "queryParams",
            CONFIG_FULL_URL = "fullurl",
            CONFIG_HEAD_URL = "headurl",
            CONFIG_APP_HEALTH_URL = "Application Healthcheck URL",
            CONFIG_DIGEST = "digest",
            CONFIG_LOG_PUB_CONTROL = "LogPublicationControl",
            CONFIG_CHANGE_SEVERITY = "Change Severity",
            CONFIG_ENVIRONMENT_ID = "EnvironmentId",
            CONFIG_SERVICE_PORT = "ServicePort",
            CONFIG_KEY = "key",
            CONFIG_IV = "iv",
            CONFIG_DBSUBSTITUTE = "DBConnectionSubstitute",
            CONFIG_DBSERVER = "DBInstance",
            CONFIG_DBUSER = "DBUsername",
            CONFIG_DBPASSWORD = "DBPassword",
            CONFIG_IAM_ACCESSKEY = "AccessKey",
            CONFIG_IAM_SECRETKEY = "SecretKey",
            CONFIG_LOGICAL_RESOURCE = "LogicalResourceName",
            CONFIG_STACK_NAME = "StackName",
            CONFIG_CLOUDFORMATION_ENDPOINT = "CloudFormationEndpoint",
            USER_ACCESS = "accessKey",
            USER_SECRET = "secretKey";

        private JsonData config;
        
        public HostManagerConfig(string configJson) 
        {
            config = JsonMapper.ToObject(configJson);

            #region Configuration Defaults

            if (Ec2InstanceId == null)
                config[CONFIG_EC2_INSTANCE_ID] = GetEC2Metadata(EC2_METADATA_INSTANCE_ID);

            if (Ec2ReservationId == null)
                config[CONFIG_EC2_RES_ID] = GetEC2Metadata(EC2_METADATA_RESERVATION_ID);

            if (ASPLogLocation == null)
                config[CONFIG_ASP_LOG_LOCATION] = @"C:\inetpub\logs\LogFiles\W3SVC1";

            #endregion
        }

        public string this[string path]
        {
            get
            {
                JsonData val = FetchKeyForPath(config, path);
                if (val != null)
                    return val.ToString();
                return null;
            }
            private set
            {
                SetKeyForPath(config, path, value);
            }
        }

        private JsonData FetchKeyForPath(JsonData json, string path)
        {
            if (null == json)
                return null;

            if (path.Contains('/'))
            {
                int slash = path.IndexOf('/');
                string key = path.Substring(0, slash);
                string rest = path.Substring(slash + 1);
                return FetchKeyForPath(json[key], rest);
            }
            
            return json[path];
        }

        internal void SetKeyForPath(string path, string value)
        {
            SetKeyForPath(config, path, value);
        }

        private void SetKeyForPath(JsonData json, string path, string value)
        {
            if (null == json || null == path || path.Length < 1)
                return;

            if (path.Contains('/'))
            {
                int slash = path.IndexOf('/');
                string key = path.Substring(0, slash);
                string rest = path.Substring(slash + 1);
                if (null == json[key])
                    json[key] = new JsonData();
                SetKeyForPath(json[key], rest, value);
            }
            else
            {
                json[path] = value;
            }
        }

        private string GetVersionNumberFromEnd(string input)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            string[] parts = input.Split(' ');
            string vNumber = parts[parts.Length - 1];
            if (vNumber.StartsWith("v",StringComparison.InvariantCultureIgnoreCase))
            {
                vNumber = vNumber.Substring(1);
            }
            return vNumber;
        }

        public string ApplicationFullURL { get { return this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_APPLICATION, CONFIG_FULL_URL)]; } }
        public string ApplicationHeadURL { get { return this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_APPLICATION, CONFIG_HEAD_URL)]; } }
        public string ApplicationDigest  { get { return this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_APPLICATION, CONFIG_DIGEST)]; } }

        public string Ec2InstanceId             { get { return this[CONFIG_EC2_INSTANCE_ID]; } }
        public string Ec2ReservationId          { get { return this[CONFIG_EC2_RES_ID]; } }
        public string ASPLogLocation            { get { return this[CONFIG_ASP_LOG_LOCATION]; } }

        internal string Key { get { return this[String.Join("/", SECTION_CREDENTIALS, CONFIG_KEY)]; } }
        internal string IV { get { return this[String.Join("/", SECTION_CREDENTIALS, CONFIG_IV)]; } }

        public bool RequirePublicationToExport
        {
            get
            {
                Boolean ret;
                if (!Boolean.TryParse((string)this[String.Join("/",SECTION_AWSDEPLOYMENT,SECTION_HOSTMANAGER,CONFIG_REQ_PUB_TO_EXPORT)], out ret))
                {
                    ret = true;
                }
                return ret;
            }
        }
        public bool RotateEventLogsToS3
        {
            get
            {
                Boolean ret;
                if (!Boolean.TryParse((string)this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_HOSTMANAGER, CONFIG_EVENT_LOG_PUB)], out ret))
                {
                    ret = true;
                }
                return ret;
            }
        }

        public bool RotateLogsToS3
        {
            get
            {
                Boolean ret;
                if (!Boolean.TryParse((string)this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_HOSTMANAGER, CONFIG_LOG_PUB_CONTROL)], out ret))
                {
                    ret = true;
                }
                return ret;
            }
        }
        public bool Enable32Bit
        {
            get
            {
                Boolean ret;
                if (!Boolean.TryParse((string)this[String.Join("/", SECTION_CONTAINER, SECTION_RUNTIME, CONFIG_ENABLE_32_BIT)], out ret))
                {
                    //should take this out once stablized to other name.
                    Boolean.TryParse((string)this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_CONTAINER, "dotnet", "enable32bitapps", CONFIG_ENABLE_32_BIT)], out ret); //Default value is false
                }
                return ret;
            }
        }
        public System.Version TargetRuntime
        { 
            get 
            {
                System.Version ret;
                if (!System.Version.TryParse(GetVersionNumberFromEnd((string)this[String.Join("/",SECTION_CONTAINER,SECTION_RUNTIME,CONFIG_TARGET_RUNTIME)]), out ret))
                {
                    //should take this out once stablized to other name.
                    if (!System.Version.TryParse(GetVersionNumberFromEnd((string)this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_CONTAINER, "dotnet", "targetruntime", CONFIG_TARGET_RUNTIME)]), out ret))
                    {
                        //If we couldn't find the version set it to default to value;
                        ret = defaultTargetRuntime;
                    }
                }
                return ret;
            } 
        }
        public System.Version DefaultTargetRuntime { get { return defaultTargetRuntime; } }
        internal JsonData Environment { get { return FetchKeyForPath(config, String.Join("/",SECTION_APPLICATION,SECTION_ENVIRONMENT)); } }

        public string ApplicationHealthcheckUrl { get { return (string)this[String.Join("/", SECTION_AWSDEPLOYMENT, SECTION_APPLICATION, CONFIG_APP_HEALTH_URL)]; } }


        private string GetEC2Metadata(string key)
        {
            return GetEC2Metadata(key, 5);
        }

        private string GetEC2Metadata(string key, int tries)
        {
            for (int i = 0; i < tries; i++)
            {
                System.Threading.Thread.Sleep(i * 1000);

                try
                {
                    LOGGER.InfoFormat("Getting EC2 metadata {0}", key);
                    WebRequest req = WebRequest.Create(String.Format("{0}{1}", EC2_METADATA_URL, key));
                    WebResponse res = req.GetResponse();

                    StreamReader sr = new StreamReader(res.GetResponseStream());
                    string value = sr.ReadToEnd().Trim();
                    LOGGER.InfoFormat("Received EC2 metadata for key {0} as {1}", key, value);

                    return value;
                }
                catch (Exception e)
                {
                    LOGGER.Error(string.Format("Failed to retrieve metadata for {0}", key), e);
                }
            }

            return null;
        }
        internal bool VerifyCryptoValues()
        {
            bool ret = false;
            string metadata_instance_id =  GetEC2Metadata(EC2_METADATA_INSTANCE_ID);
            string metadata_reservation_id = GetEC2Metadata(EC2_METADATA_RESERVATION_ID);

            if (!String.Equals(this[CONFIG_EC2_INSTANCE_ID],metadata_instance_id,StringComparison.InvariantCultureIgnoreCase))
            {
                if (this[CONFIG_EC2_INSTANCE_ID] == null)
                {
                    LOGGER.InfoFormat("ec2 instance id was not set, setting to {0}", metadata_instance_id);
                    config[CONFIG_EC2_INSTANCE_ID] = metadata_instance_id;
                    ret = true;
                }
                else
                {
                    if (metadata_instance_id != null)
                    {
                        LOGGER.WarnFormat("ec2 instance id does not match expected value (updating),\n\texpected:{0}\n\tfound:{1}",this[CONFIG_EC2_INSTANCE_ID],metadata_instance_id);
                        config[CONFIG_EC2_INSTANCE_ID] = metadata_instance_id;
                        ret = true;
                    }
                }
            }

            if (!String.Equals(this[CONFIG_EC2_RES_ID],metadata_reservation_id,StringComparison.InvariantCultureIgnoreCase))
            {
                if (this[CONFIG_EC2_RES_ID] == null)
                {
                    LOGGER.InfoFormat("ec2 reservation id was not set, setting to {0}", metadata_reservation_id);
                    config[CONFIG_EC2_RES_ID] = metadata_reservation_id;
                    ret = true;
                }
                else
                {
                    if (metadata_instance_id != null)
                    {
                        LOGGER.WarnFormat("ec2 reservation id does not match expected value (updating),\n\texpected:{0}\n\tfound:{1}", this[CONFIG_EC2_RES_ID], metadata_reservation_id);
                        config[CONFIG_EC2_RES_ID] = metadata_reservation_id;
                        ret = true;
                    }
                }
            }
            return ret;
        }

        public static string EC2UserData
        {
            get
            {
                if (null == _ec2UserData)
                {
                    LoadEC2UserData();
                }
                return _ec2UserData;
            }
        }

        public static string WaitConditionSignalURL
        {
            get
            {
                // Null checking on _ec2UserData because wait condition might not be set
                if (null == _ec2UserData)
                {
                    LoadEC2UserData();
                }
                return _waitConditionSignalURL;
            }
        }

        private static void LoadEC2UserData()
        {
            int attempt = 1;

            do
            {
                try
                {
                    WebRequest req = WebRequest.Create(EC2_USER_DATA_URL);
                    WebResponse res = req.GetResponse();
                    StreamReader sr = new StreamReader(res.GetResponseStream());

                    string content = sr.ReadToEnd().Trim();

                    var splits = content.Split('[', ']');

                    // If we get a signal url the format will be [JsonData][WaitConditionURL]
                    if (splits.Length < 5)
                    {
                        _ec2UserData = content;
                        LOGGER.Info("No wait signal found");
                    }
                    else
                    {
                        _ec2UserData = splits[1];
                        _waitConditionSignalURL = splits[3];
                        LOGGER.InfoFormat("Wait signal found {0}", _waitConditionSignalURL);

                        if (splits.Length > 5)
                        {
                            SetupMetaDataUser(splits[5]);
                        }
                        else
                        {
                            HostManager.LOGGER.Info("No IAM user setup");
                        }
                    }

                    return;
                }
                catch (Exception e)
                {
                    LOGGER.Info("Failed to retrieve EC2 user data", e);
                    System.Threading.Thread.Sleep(1000 * attempt);
                }
            }
            while (++attempt < USER_DATA_LOAD_MAX_ATTEMPTS);

            LOGGER.Error("Exceeded maximum attempts to retrieve user data");
            System.Environment.Exit(-2); //Can't get needed data exiting.
            return; //this is needed to convince the compilier that all paths return a value.
        }

        private static void SetupMetaDataUser(string userDataToken)
        {
            var iamProperties = new Dictionary<string, string>();
            var parameters = userDataToken.Split(';');
            foreach (var param in parameters)
            {
                if (string.IsNullOrWhiteSpace(param))
                    continue;

                var tokens = param.Split('=');
                if (tokens.Length >= 2)
                {
                    HostManager.LOGGER.InfoFormat("Adding parameter \"{0}\"", tokens[0]);
                    iamProperties[tokens[0]] = tokens[1];
                }
            }

            _metaDataUserIsConfigured = false;

            if (!iamProperties.TryGetValue(CONFIG_IAM_ACCESSKEY, out _iamUserAccessKey))
            {
                HostManager.LOGGER.Info("Skipping metadata look up no access key");
                return;
            }
            if (!iamProperties.TryGetValue(CONFIG_IAM_SECRETKEY, out _iamUserSecretKey))
            {
                HostManager.LOGGER.Info("Skipping metadata look up no secret key");
                return;
            }
            if (!iamProperties.TryGetValue(CONFIG_CLOUDFORMATION_ENDPOINT, out _cloudFormationEndpoint))
            {
                HostManager.LOGGER.Info("Skipping metadata look up no endpoint");
                return;
            }
            if (!iamProperties.TryGetValue(CONFIG_STACK_NAME, out _stackName))
            {
                HostManager.LOGGER.Info("Skipping metadata look up no stack name");
                return;
            }
            if (!iamProperties.TryGetValue(CONFIG_LOGICAL_RESOURCE, out _logicalResourceId))
            {
                HostManager.LOGGER.Info("Skipping metadata look up no logical resource id");
                return;
            }

            HostManager.LOGGER.InfoFormat("Metadata reader configured for logical resource {0} from stack {1}", _logicalResourceId, _stackName);
            _metaDataUserIsConfigured = true;
        }

        public Dictionary<string, string> LoadResourceMetaData()
        {
            if (!_metaDataUserIsConfigured)
                return new Dictionary<string,string>();

            try
            {
                HostManager.LOGGER.InfoFormat("Looking up metadata on logical resource {0} from stack {1}", _logicalResourceId, _stackName);

                var config = new AmazonCloudFormationConfig() { ServiceURL = _cloudFormationEndpoint };
                var client = new AmazonCloudFormationClient(_iamUserAccessKey, _iamUserSecretKey, config);

                var request = new DescribeStackResourceRequest()
                {
                    StackName = _stackName,
                    LogicalResourceId = _logicalResourceId
                };

                var response = client.DescribeStackResource(request);
                if (response.StackResourceDetail == null ||
                    string.IsNullOrWhiteSpace(response.StackResourceDetail.Metadata))
                {
                    HostManager.LOGGER.Info("No meta data found");
                    return new Dictionary<string, string>();
                }

                var metadata = ThirdParty.Json.LitJson.JsonMapper.ToObject<Dictionary<string, string>>(response.StackResourceDetail.Metadata);
                return metadata;
            }
            catch (Exception e)
            {
                HostManager.LOGGER.Error("Failed to fetch metadata from logical resource " + _logicalResourceId, e);
                return new Dictionary<string, string>();
            }
        }

        // The initial startup, we will have to get the config from userdata
        // This method should create a HostManagerConfig from the userdata.

        public static HostManagerConfig CreateFromUserData()
        {
            return CreateFromUserData(EC2UserData);
        }

        public static HostManagerConfig CreateFromUserData(string json)
        {
            string configJson = null;
            JsonData userData = JsonMapper.ToObject(json);

            try
            {
                string value = S3Util.LoadContent((string)userData[SECTION_CONFIGURATION][CONFIG_FULL_URL]);

                byte[] key = Convert.FromBase64String((string)userData[SECTION_CREDENTIALS][CONFIG_KEY]);
                byte[] iv = Convert.FromBase64String((string)userData[SECTION_CREDENTIALS][CONFIG_IV]);
                configJson = CryptoUtil.DecryptFromBase64EncodedString(value, key, iv);
                LOGGER.InfoFormat("Config: {0}", configJson);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to retrieve content from S3.", e);
                return null;
            }

            var config = new HostManagerConfig(configJson);

            if (userData[SECTION_CREDENTIALS][CONFIG_KEY].IsString)
                config[String.Format("{0}/{1}", SECTION_CREDENTIALS, CONFIG_KEY)] = (string)userData[SECTION_CREDENTIALS][CONFIG_KEY];

            if (userData[SECTION_CREDENTIALS][CONFIG_IV].IsString)
                config[String.Format("{0}/{1}", SECTION_CREDENTIALS, CONFIG_IV)] = (string)userData[SECTION_CREDENTIALS][CONFIG_IV];

            if (ApplicationVersion.LoadLatestVersion() == null)
            {
                ConfigVersion vers = new ConfigVersion((string)userData[SECTION_CONFIGURATION][CONFIG_FULL_URL], (string)userData[SECTION_CREDENTIALS][CONFIG_KEY], (string)userData[SECTION_CREDENTIALS][CONFIG_IV], false);
                vers.Persist();
            }

            return config;
        }

        public static HostManagerConfig CreateFromS3(string url, string key, string iv)
        {
            string json;

            try
            {
                string encrypted = S3Util.LoadContent(url);

                byte[] keybytes = Convert.FromBase64String(key);
                byte[] ivbytes = Convert.FromBase64String(iv);

                json = CryptoUtil.DecryptFromBase64EncodedString(encrypted, keybytes, ivbytes);
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to retrieve content from S3.", e);
                return null;
            }

            return new HostManagerConfig(json);
        }
    }
}
