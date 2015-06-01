using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using ThirdParty.Json.LitJson;
using log4net;

using AWSDeploymentCryptoUtility;

namespace AWSDeploymentHostManager.Tasks
{
    public class UpdateConfigurationTask : Task
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(UpdateConfigurationTask));

        private HostManagerConfig newConfig = null;

        private const string
            JSON_KEY_CONFIG_URL = "configUrl",
            JSON_KEY_CRYPTO_KEY = "key",
            JSON_KEY_CRYPTO_IV = "iv";

        public override string Execute()
        {
            LOGGER.Info("Execute");

            DateTime lastModified;

            string user = HostManagerConfig.EC2UserData;
            JsonData userData = JsonMapper.ToObject(user);

            string data = S3Util.LoadContent((string)userData[HostManagerConfig.SECTION_CONFIGURATION][HostManagerConfig.CONFIG_FULL_URL], out lastModified);

            LOGGER.InfoFormat("Stored config last updated: {0}", lastModified);

            ConfigVersion currentVersion = ConfigVersion.LoadLatestVersion();

            LOGGER.InfoFormat("Last config update: {0}", currentVersion.Timestamp);

            if (lastModified > currentVersion.Timestamp)
            {
                LOGGER.Info("Found updated Configuration");
                new ConfigVersion(currentVersion.S3Bucket, currentVersion.S3Key).Persist();
                
                string configJson = null;

                try
                {
                    byte[] key = Convert.FromBase64String((string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_KEY]);
                    byte[] iv = Convert.FromBase64String((string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_IV]);
                    configJson = CryptoUtil.DecryptFromBase64EncodedString(data, key, iv);
                    LOGGER.InfoFormat("Config: {0}", configJson);
                }
                catch (Exception e)
                {
                    LOGGER.Error("Failed to retrieve content from S3.", e);
                    return null;
                }
                
                newConfig = new HostManagerConfig(configJson);

                //if (userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.USER_ACCESS].IsString)
                //    newConfig.SetKeyForPath(String.Format("{0}/{1}", HostManagerConfig.SECTION_CREDENTIALS, HostManagerConfig.CONFIG_ACCESS_KEY), (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.USER_ACCESS]);

                //if (userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.USER_SECRET].IsString)
                //    newConfig.SetKeyForPath(String.Format("{0}/{1}", HostManagerConfig.SECTION_CREDENTIALS, HostManagerConfig.CONFIG_SECRET_KEY), (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.USER_SECRET]);

                if (userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_KEY].IsString)
                    newConfig.SetKeyForPath(String.Format("{0}/{1}", HostManagerConfig.SECTION_CREDENTIALS, HostManagerConfig.CONFIG_KEY), (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_KEY]);

                if (userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_IV].IsString)
                    newConfig.SetKeyForPath(String.Format("{0}/{1}", HostManagerConfig.SECTION_CREDENTIALS, HostManagerConfig.CONFIG_IV), (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_IV]);

                ConfigVersion vers = new ConfigVersion((string)userData[HostManagerConfig.SECTION_CONFIGURATION][HostManagerConfig.CONFIG_FULL_URL], (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_KEY], (string)userData[HostManagerConfig.SECTION_CREDENTIALS][HostManagerConfig.CONFIG_IV], false);
                vers.Persist();
            }

            return GenerateResponse(TASK_RESPONSE_DEFER);
        }

        public HostManagerConfig NewConfiguration
        {
            get { return newConfig; }
        }

        public override string Operation
        {
            get { return "UpdateConfiguration"; }
        }
    }
}
