using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Amazon.Runtime.CredentialManagement;

namespace AWSDeployment
{
    public static class DeploymentConfigurationReader
    {
        const string DeploymentServiceParameterName = "deploymentservice";
        const string CloudFormationServiceName = "cloudformation";
        const string ElasticBeanstalkServiceName = "elasticbeanstalk";

        public static DeploymentEngineBase ReadDeploymentFromFile(string path, bool isRedeploy)
        {
            return ReadDeploymentFromFile(path, isRedeploy, null);
        }

        public static DeploymentEngineBase ReadDeploymentFromFile(string path, bool isRedeploy, DeploymentObserver observer)
        {
            using (var fStream = File.OpenRead(path))
            {
                return ReadDeploymentFromStream(fStream, null, isRedeploy, observer);
            }
        }

        public static DeploymentEngineBase ReadDeploymentFromFile(string path, Dictionary<string, string> overrides, bool isRedeploy, DeploymentObserver observer)
        {
            using (var fStream = File.OpenRead(path))
            {
                return ReadDeploymentFromStream(fStream, overrides, isRedeploy, observer);
            }
        }

        public static DeploymentEngineBase ReadDeploymentFromStream(Stream stream, bool isRedeploy)
        {
            return ReadDeploymentFromStream(stream, null, isRedeploy, null);
        }
        
        public static DeploymentEngineBase ReadDeploymentFromStream(Stream stream, Dictionary<string, string> overrides, bool isRedeploy, DeploymentObserver observer)
        {
            var _observer = observer ?? new DeploymentObserver();
            var parameterSets = GetConfigurationFromStream(stream, overrides, _observer);
            ValidateAWSCredentials(parameterSets);

            // construct correct engine and re-process parameters into the instance
            var deploymentEngine = CreateAndConfigureDeploymentEngine(parameterSets, _observer);

            // could catch error return here and trigger process failure?
            if (deploymentEngine.PostProcessConfigurationSettings(isRedeploy) != DeploymentEngineBase.SUCCESS)
                throw new ConfigurationReaderException("Deployment failed during post-processing of configuration settings.");

            return deploymentEngine;
        }

        private static void ValidateAWSCredentials(ConfigurationParameterSets parameterSets)
        {
            var generalSection = parameterSets[CommonSectionNames.GeneralSection];
            if (generalSection == null)
                throw new ConfigurationReaderException("No credential specified to do the deployment. Either AWSAccessKey and AWSSecretKey or AWSProfileName must be specified");

            if (!generalSection.ContainsKey(DeploymentEngineBase.ACCESS_KEY) &&
                !generalSection.ContainsKey(DeploymentEngineBase.ACCOUNT_PROFILE_NAME))
            {
                // Like the SDK if both keys and profile are given then keys will be preferred.
                throw new ConfigurationReaderException("No credential specified to do the deployment. Either AWSAccessKey and AWSSecretKey or AWSProfileName must be specified");
            }
            
            if (generalSection.ContainsKey(DeploymentEngineBase.ACCESS_KEY) &&
                generalSection.ContainsKey(DeploymentEngineBase.ACCOUNT_PROFILE_NAME))
            {
                // Like the SDK if both keys and profile are given then keys will be preferred.
                generalSection.Remove(DeploymentEngineBase.ACCOUNT_PROFILE_NAME);
            }

            if (generalSection.ContainsKey(DeploymentEngineBase.ACCOUNT_PROFILE_NAME))
            {
                Amazon.Runtime.AWSCredentials credentials;
                var chain = new CredentialProfileStoreChain();
                if (!chain.TryGetAWSCredentials(generalSection[DeploymentEngineBase.ACCOUNT_PROFILE_NAME].Value, out credentials))
                {
                    throw new ConfigurationReaderException(string.Format("Account Profile {0} could not be found.", generalSection[DeploymentEngineBase.ACCOUNT_PROFILE_NAME].Value));
                }
            }
        }

        public static ConfigurationParameterSets GetConfigurationFromStream(Stream stream)
        {
            return GetConfigurationFromStream(stream, null, new DeploymentObserver());
        }

        private static ConfigurationParameterSets GetConfigurationFromStream(Stream stream, Dictionary<string, string> overrides, DeploymentObserver _observer)
        {
            var parameterSets = new ConfigurationParameterSets();

            var sr = new StreamReader(stream);

            _observer.Info("Scanning configuration.");

            // parse the config into sets of parameters, then inspect to determine the best engine
            // to use 
            int lineNo = 0;
            while (!sr.EndOfStream)
            {
                lineNo++;
                string line = sr.ReadLine();
                if (line.StartsWith("#") || line.Length < 1)
                    continue;

                string[] kv = line.Split(new char[] { '=' }, 2);

                if (kv.Length != 2)
                {
                    _observer.Warn("Malformed configuration entry at line {0}: {1}", lineNo, line);
                    continue;
                }

                string key = kv[0].Trim();
                string val = kv[1].Trim();

                parameterSets.SetParameter(key, val, lineNo);
            }

            if (overrides != null)
                ProcessOverrides(parameterSets, overrides, _observer);
            return parameterSets;
        }

        static DeploymentEngineBase CreateAndConfigureDeploymentEngine(ConfigurationParameterSets parameterSets, 
                                                                       DeploymentObserver observer)
        {
            DeploymentEngineBase deploymentEngine;

            string templateName = string.Empty;
            var generalParameters = parameterSets[CommonSectionNames.GeneralSection];
            if (generalParameters.ContainsKey(CommonParameters.GeneralSection_Template))
                templateName = generalParameters[CommonParameters.GeneralSection_Template].Value;
            if (IsBeanstalkTemplate(templateName))
                deploymentEngine = new BeanstalkDeploymentEngine(observer);
            else
                deploymentEngine = new CloudFormationDeploymentEngine(observer);

            string[] parameterSetNames = parameterSets.ParameterSetNames;
            foreach (string setName in parameterSetNames)
            {
                var parameters = parameterSets[setName];
                foreach (string paramKey in parameters.Keys)
                {
                    ProcessLine(deploymentEngine, setName, paramKey, parameters[paramKey]);
                }
            }

            return deploymentEngine;
        }
        
        static void ProcessOverrides(ConfigurationParameterSets parameterSets, Dictionary<string, string> overrides, DeploymentObserver observer)
        {
            foreach(var kv in overrides)
            {
                parameterSets.SetParameter(kv.Key, kv.Value, -1);
            }
        }

        public static void ProcessLine(DeploymentEngineBase deployment, string section, string key, ConfigurationParameterSets.ConfigurationParameter cp)
        {
            if (section == CommonSectionNames.GeneralSection)
                ProcessGeneralLine(deployment, key, cp.Value, cp.LineNumber);
            else
                if (string.Compare(section, CommonSectionNames.ContainerSection, StringComparison.InvariantCultureIgnoreCase) == 0)
                    ProcessContainerLine(deployment, key, cp.Value, cp.LineNumber);
                else
                    deployment.ProcessConfigurationLine(section, key, cp.Value, cp.LineNumber);
        }

        static void ProcessGeneralLine(DeploymentEngineBase deployment, string key, string val, int lineNo)
        {
            switch (key)
            {
                case CommonParameters.GeneralSection_DeploymentPackage:
                    deployment.DeploymentPackage = val;
                    break;
                case CommonParameters.GeneralSection_Region:
                    deployment.Region = val;
                    break;
                case CommonParameters.GeneralSection_UploadBucket:
                    deployment.UploadBucket = val;
                    break;
                case CommonParameters.GeneralSection_KeyPair:
                    deployment.KeyPairName = val;
                    break;
                case CommonParameters.GeneralSection_AWSAccessKey:
                    deployment.AWSAccessKey = val;
                    break;
                case CommonParameters.GeneralSection_AWSProfileName:
                    deployment.SetCredentialsFromProfileName(val);
                    break;
                case CommonParameters.GeneralSection_AWSSecretKey:
                    deployment.AWSSecretKey = val;
                    break;
                default:
                    deployment.ProcessConfigurationLine(null, key, val, lineNo);
                    break;
            }
        }

        private static void ProcessContainerLine(DeploymentEngineBase deployment, string key, string val, int lineNo)
        {
            switch (key)
            {
                case CommonParameters.ContainerSection_Enable32BitApplications:
                    bool enable = false;
                    if (bool.TryParse(val, out enable))
                        deployment.Enable32BitApplications = enable;
                    else
                        deployment.Observer.Warn("Value supplied to Container.Enable32BitApplications is not a boolean."); 
                    break;
                case CommonParameters.ContainerSection_TargetRuntime:
                    deployment.TargetRuntime = val;    // should be in x.y format
                    break;
                case CommonParameters.ContainerSection_TargetV2Runtime: /* deprecated, use TargetRuntime */
                    bool v2 = false;
                    if (bool.TryParse(val, out v2))
                    {
                        if (v2)
                            deployment.TargetRuntime = "2.0";
                        else
                            deployment.TargetRuntime = "4.0";
                    }
                    else
                        deployment.Observer.Warn("Value supplied to Container.TargetV2Runtime is not a boolean."); 
                    break;
                    // sjr - we shipped awsdeploy with a typo in the name, correct quietly
                case CommonParameters.ContainerSection_ApplicationHealhcheckPath:
                case CommonParameters.ContainerSection_ApplicationHealthcheckPath:
                    deployment.ApplicationHealthcheckPath = val;
                    break;
                default:
                    deployment.ProcessConfigurationLine(CommonSectionNames.ContainerSection, key, val, lineNo);
                    break;
            }
        }

        // wanted to put this into the derived engines, but CloudFormation template identity is
        // pretty free-form (what with custom templates) so had to keep here
        static bool IsBeanstalkTemplate(string templateName)
        {
            // give the user some rope in how they identify they want to use Beanstalk
            string[] templateNames = 
            {
                "AWS Elastic Beanstalk",
                "Elastic Beanstalk",
                "ElasticBeanstalk"
            };

            return templateNames.Any(name => string.Compare(templateName, name, StringComparison.InvariantCultureIgnoreCase) == 0);
        }

        public class ConfigurationReaderException : Exception
        {
            public ConfigurationReaderException(string message)
                : base(message)
            {
            }
        }
    }

    /// <summary>
    /// Groups parameters into sets based on dotted notation of parameter name
    /// </summary>
    public class ConfigurationParameterSets
    {
        /// <summary>
        /// Records the parameter value and configuration file line number at which the
        /// parameter key was found
        /// </summary>
        public class ConfigurationParameter
        {
            public int LineNumber { get; set; }
            public string Value { get; set; }
        }

        Dictionary<string, Dictionary<string, ConfigurationParameter>> parameterSets = new Dictionary<string, Dictionary<string, ConfigurationParameter>>(StringComparer.InvariantCultureIgnoreCase);

        public ConfigurationParameterSets() { }

        public void PutParameter(string key, string value)
        {
            PutParameter(null, key, value);
        }

        public void PutParameter(string set, string key, string value)
        {
            string fullName = string.IsNullOrEmpty(set) ? key : set + "." + key;
            SetParameter(fullName, value, -1);
        }

        public void SetParameter(string paramKey, string paramValue, int lineNumber)
        {
            Dictionary<string, ConfigurationParameter> setParameters;
            string paramSection, paramName;

            ParseKey(paramKey, out paramSection, out paramName);
            if (parameterSets.ContainsKey(paramSection))
                setParameters = parameterSets[paramSection];
            else
            {
                setParameters = new Dictionary<string, ConfigurationParameter>(StringComparer.InvariantCultureIgnoreCase);
                parameterSets.Add(paramSection, setParameters);
            }

            if (setParameters.ContainsKey(paramName))
                setParameters[paramName].Value = paramValue;
            else
                setParameters.Add(paramName, new ConfigurationParameter{ LineNumber = lineNumber, Value = paramValue});
        }

        public string[] ParameterSetNames
        {
            get
            {
                return parameterSets.Keys.ToArray<string>();
            }
        }

        public Dictionary<string, ConfigurationParameter> this[string setName]
        {
            get {
                return parameterSets.ContainsKey(setName) ? parameterSets[setName] : null;
            }
        }

        void ParseKey(string paramKey, out string paramSection, out string paramName)
        {
            int dotPos = paramKey.IndexOf('.');

            if (dotPos > -1)
            {
                paramSection = paramKey.Substring(0, dotPos);
                paramName = paramKey.Substring(dotPos + 1);
            }
            else
            {
                paramSection = string.Empty;
                paramName = paramKey;
            }
        }
    }
}
