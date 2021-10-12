using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.ElasticBeanstalk.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    /// <summary>
    /// Model class representing a deployed application and
    /// its collection of environments.
    /// </summary>
    public class DeployedApplicationModel
    {
        public string ApplicationName { get; set; }
        public List<EnvironmentDescription> Environments { get; set; }

        public string SelectedEnvironmentName { get; set; }

        public ImageSource ApplicationIcon => ToolkitImages.ElasticBeanstalkApplication;

        public ImageSource EnvironmentIcon => ToolkitImages.ElasticBeanstalkEnvironment;

        internal DeployedApplicationModel(string applicationName)
        {
            ApplicationName = applicationName;
            Environments = new List<EnvironmentDescription>();
        }

        private DeployedApplicationModel() { }

        public bool IsSelectedEnvironmentWindowsSolutionStack
        {
            get
            {
                var envDesc = this.Environments.FirstOrDefault(x => string.Equals(x.EnvironmentName, SelectedEnvironmentName, System.StringComparison.CurrentCultureIgnoreCase));
                if (envDesc == null)
                    return true;

                return Amazon.ElasticBeanstalk.Tools.EBUtilities.IsSolutionStackWindows(envDesc.SolutionStackName);
            }
        }
    }
}
