using System;
using System.Collections.Generic;
using System.Linq;

namespace AWSDeployment
{
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

        public string[] ParameterSetNames => parameterSets.Keys.ToArray<string>();

        public Dictionary<string, ConfigurationParameter> this[string setName] => parameterSets.ContainsKey(setName) ? parameterSets[setName] : null;

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
