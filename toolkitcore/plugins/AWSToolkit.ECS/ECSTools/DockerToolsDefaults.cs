using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using System.Threading.Tasks;

using Amazon.ECS.Tools.Commands;
using Amazon.ECS.Tools.Options;

using ThirdParty.Json.LitJson;

namespace Amazon.ECS.Tools
{
    /// <summary>
    /// Reads in the json format 'defaults' file for the project.
    /// </summary>
    public static class DockerToolsDefaultsReader
    {
        public const string DEFAULT_FILE_NAME = "aws-docker-tools-defaults.json";

        public static DockerToolsDefaults LoadDefaults(string projectLocation, string configFile)
        {
            string path = Path.Combine(projectLocation, configFile);

            var defaults = new DockerToolsDefaults(path);
            if (!File.Exists(path))
                return defaults;

            using (var reader = new StreamReader(File.OpenRead(path)))
            {
                try
                {
                    JsonData data = JsonMapper.ToObject(reader) as JsonData;
                    return new DockerToolsDefaults(data, path);
                }
                catch (Exception e)
                {
                    throw new DockerToolsException($"Error parsing default config {path}: {e.Message}", DockerToolsException.ErrorCode.DefaultsParseFail, e);
                }
            }
        }
    }

    /// <summary>
    /// This class gives access to the default values for the CommandOptions defined in the project's default json file.
    /// </summary>
    public class DockerToolsDefaults
    {
        JsonData _rootData;

        public DockerToolsDefaults(string sourceFile)
            : this(new JsonData(), sourceFile)
        {
        }

        public DockerToolsDefaults(JsonData data, string sourceFile)
        {
            this._rootData = data ?? new JsonData();
            this.SourceFile = sourceFile;
        }

        /// <summary>
        /// The file the default values were read from.
        /// </summary>
        public string SourceFile
        {
            get;
        }

        /// <summary>
        /// Gets the default value for the CommandOption with the CommandOption's switch string.
        /// </summary>
        /// <param name="fullSwitchName"></param>
        /// <returns></returns>
        public object this[string fullSwitchName]
        {
            get
            {
                if (fullSwitchName.StartsWith("--"))
                    fullSwitchName = fullSwitchName.Substring(2);

                if (this._rootData[fullSwitchName] == null)
                    return null;

                if (this._rootData[fullSwitchName].IsString)
                    return this._rootData[fullSwitchName].ToString();
                if (this._rootData[fullSwitchName].IsInt)
                    return (int)this._rootData[fullSwitchName];
                if (this._rootData[fullSwitchName].IsBoolean)
                    return (bool)this._rootData[fullSwitchName];
                if (this._rootData[fullSwitchName].IsArray)
                {
                    var items = new string[this._rootData[fullSwitchName].Count];
                    for(int i = 0; i < items.Length; i++)
                    {
                        items[i] = this._rootData[fullSwitchName][i].ToString();
                    }
                    return items;
                }
                if (this._rootData[fullSwitchName].IsObject)
                {
                    var obj = new Dictionary<string, string>();
                    foreach (var key in this._rootData[fullSwitchName].PropertyNames)
                    {
                        obj[key] = this._rootData[key]?.ToString();
                    }
                    return obj;
                }

                return null;
            }
        }

        private JsonData GetValue(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            return this._rootData[key];
        }

        /// <summary>
        /// Gets the default if it exists as a string. This is used for display purpose.
        /// </summary>
        /// <param name="option"></param>
        /// <returns></returns>
        public string GetValueAsString(CommandOption option)
        {
            var key = option.Switch.Substring(2);
            var data = this._rootData[key];
            if (data == null)
                return null;

            if (data.IsString)
                return data.ToString();
            else if (data.IsBoolean)
                return ((bool)data).ToString();
            else if (data.IsInt)
                return ((int)data).ToString();

            return null;
        }


        public static string FormatCommaDelimitedList(string[] values)
        {
            if (values == null)
                return null;

            return string.Join(",", values);
        }

        public static string FormatKeyValue(IDictionary<string, string> values)
        {
            if (values == null)
                return null;

            StringBuilder sb = new StringBuilder();

            foreach(var kvp in values)
            {
                if (sb.Length > 0)
                    sb.Append(";");

                sb.Append($"\"{kvp.Key}\"=\"{kvp.Value}\"");
            }

            return sb.ToString();
        }
    }
}
