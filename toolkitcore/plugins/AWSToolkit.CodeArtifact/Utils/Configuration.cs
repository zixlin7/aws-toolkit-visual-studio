using log4net;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace Amazon.AWSToolkit.CodeArtifact.Utils
{
    public class Configuration
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(Configuration));

        public const string DEFAULT_FILENAME = "configuration.json";
        public string DefaultProfile { get; set; }

        //SourceProfileOverrides will contain the mapping of EndpointUrl and AWS Profile to be used if user wants to set a particular profile for particular repository.
        public IDictionary<string, string> SourceProfileOverrides { get; set; }

        public static Configuration LoadInstalledConfiguration()
        {
            return LoadConfiguration(DetermineConfigurationFileInstall());
        }

        public static Configuration LoadConfiguration(string filePath)
        {
            if (!File.Exists(filePath))
                return new Configuration();
            try
            {
                var config = JsonConvert.DeserializeObject<Configuration>(File.ReadAllText(filePath));
                return config;
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to load existing configuration", e);
                throw new Exception(e.Message);
            }

        }

        public void SaveInstallPath()
        {
            Save(DetermineConfigurationFileInstall());
        }

        private void Save(string filePath)
        {
            try
            {
                var json = JsonConvert.SerializeObject(this);

                var dir = Directory.GetParent(filePath);
                if (!dir.Exists)
                {
                    dir.Create();
                }

                File.WriteAllText(filePath, json);
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to save configuration", e);
            }
        }


        public static string DetermineConfigurationFileInstall()
        {
            return Path.Combine(Utilities.DetermineInstallPath(Utilities.NETCORE_PLUGIN_TYPE), DEFAULT_FILENAME);
        }
    }
}
