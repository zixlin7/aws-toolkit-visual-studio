using System.Collections.Generic;
using System.IO;
using System.Reflection;
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

        public Stream ApplicationIcon
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.application.png");
            }
        }

        public Stream EnvironmentIcon
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetManifestResourceStream("Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.environment.png");
            }
        }

        internal DeployedApplicationModel(string applicationName)
        {
            ApplicationName = applicationName;
            Environments = new List<EnvironmentDescription>();
        }

        private DeployedApplicationModel() { }
    }
}
