using System;
using System.Linq;
using System.IO;

namespace AWSDeployment
{
    public class DeploymentConfigurationWriter
    {
        #region Static one-call outputs

        public static void WriteDeploymentToFile(ConfigurationParameterSets parameterSets, string path)
        {
            using (Stream stream = File.Open(path, FileMode.Create))
            {
                WriteDeploymentToStream(parameterSets, stream);
            }
        }

        public static void WriteDeploymentToStream(ConfigurationParameterSets parameterSets, Stream stream)
        {
            using (StreamWriter writer = new StreamWriter(stream))
            {
                DeploymentConfigurationWriter configWriter = new DeploymentConfigurationWriter()
                {
                    OutputEmptyItems = false,
                    Writer = writer,
                    IncludeHeader = true
                };
                configWriter.Write(parameterSets);
            }
        }

        #endregion

        #region Properties

        /// <summary>
        /// Textwriter to use for outputting the configuration
        /// </summary>
        public TextWriter Writer { get;set;}

        /// <summary>
        /// Flag to signal if empty items should be written out
        /// </summary>
        public bool OutputEmptyItems { get; set; }

        /// <summary>
        /// Flag to signal if an info header should be included
        /// </summary>
        public bool IncludeHeader { get; set; }

        #endregion

        #region Public methods

        /// <summary>
        /// Write a configuration
        /// </summary>
        /// <param name="parameterSets">Configuration to write</param>
        public void Write(ConfigurationParameterSets parameterSets)
        {
            if (IncludeHeader)
            {
                Writer.WriteLine("# For detailed explanation of how these config files should be used and created please see the developer guide here:");
                Writer.WriteLine("#  http://docs.amazonwebservices.com/AWSToolkitVS/latest/UserGuide/tkv-deployment-tool.html");
                Writer.WriteLine();
            }
            else
            {
                Writer.WriteLine();
            }

            Writer.WriteLine("# Edit the parameter line below to set the path to the deployment archive or use");
            Writer.WriteLine("#     /DDeploymentPackage=value");
            Writer.WriteLine("# on the awsdeploy.exe command line for more flexibility.");
            Writer.WriteLine("# DeploymentPackage = <-- path to web deployment archive -->");
            Writer.WriteLine();

            var sets = parameterSets.ParameterSetNames.OrderBy(s => s);
            foreach (var setName in sets)
            {
                var paramSet = parameterSets[setName];
                bool valuesWritten = false;
                foreach (var key in paramSet.Keys.OrderBy(k => k))
                {
                    string paramName = string.IsNullOrEmpty(setName) ? key : setName + "." + key;
                    
                    // done above, skip
                    if (string.Compare(paramName, "DeploymentPackage", StringComparison.InvariantCultureIgnoreCase) == 0)
                        continue;

                    // point user at best practices for embedded keys used to deploy but emit them anyway
                    if (string.Compare(paramName, "AWSAccessKey", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        Writer.WriteLine("# Instead of embedding the AWSAccessKey and AWSSecretKey to be used to deploy");
                        Writer.WriteLine("# artifacts we recommend that you consider using the /DAWSAccessKey and");
                        Writer.WriteLine("# /DAWSSecretKey command line parameter overrides.");
                    }

                    if (string.Compare(paramName, "AWSProfileName", StringComparison.InvariantCultureIgnoreCase) == 0)
                    {
                        Writer.WriteLine("# Profile name is used to look up AWS access key and secret key");
                        Writer.WriteLine("# from either the SDK credentials store, or the credentials file found at");
                        Writer.WriteLine(@"# <userhome-directroy>\.aws\credentials. Alternatively the access key and ");
                        Writer.WriteLine("# secret key can be set using the command line parameters /DAWSAccessKey and /DAWSSecretKey.");
                    }

                    string paramValue = paramSet[key].Value;
                    paramValue = paramValue == null ? string.Empty : paramValue.Trim();
                    if (OutputEmptyItems || !string.IsNullOrEmpty(paramValue))
                    {
                        Writer.WriteLine("{0} = {1}", paramName, paramValue);
                        valuesWritten = true;
                    }
                }

                if (valuesWritten)
                {
                    Writer.WriteLine();
                }
            }
        }

        #endregion
    }
}
