using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;

namespace Amazon.AWSToolkit
{
    /// <summary>
    /// Definitions of standard toolkit amis, backed by S3 file
    /// </summary>
    public class ToolkitAMIManifest
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(ToolkitAMIManifest));
        private static readonly object _syncLock = new object();
        private static ToolkitAMIManifest _instance;
        private XElement _amiManifest;

        // backing file xml tags and attribute names of significance
        private const string webDemploymentCategoryValue = "webdeployment";

        public const string TOOLKITAMI_INFO_FILE = @"ToolkitAMIs.xml";

        public enum HostService
        {
            CloudFormation,
            ElasticBeanstalk,
        }

        public static ToolkitAMIManifest Instance
        {
            get
            {
                lock (_syncLock)
                {
                    if (_instance == null)
                        _instance = new ToolkitAMIManifest();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Performs a reverse lookup from the ami ID to the logical container name for a given
        /// sevice and region. If the container cannot be found, an empty string is returned.
        /// </summary>
        /// <remarks>
        /// This is used when saving a deployment configuration to decide if
        /// we should write the container name to the config file, or the specific (custom) ami ID.
        /// </remarks>
        /// <param name="service"></param>
        /// <param name="region"></param>
        /// <param name="amiID"></param>
        /// <returns></returns>
        public string WebDeploymentContainerFromAMI(HostService service, string region, string amiID)
        {
            string serviceName = ServiceNameFromEnum(service);
            var cfdeployments = from el in _amiManifest.Elements(webDemploymentCategoryValue)
                                where (string)el.Attribute("service") == serviceName
                                select el;

            var deploymentContainers = (from container in cfdeployments.Elements("container")
                                        let ami = (from el in container.Elements("region")
                                                   where (string)el.Attribute("systemname") == region
                                                   select el)
                                        select new ContainerAMI
                                        {
                                            ContainerName = (string)container.Attribute("name"),
                                            ID = ami.First().Value,
                                        }).ToList();

            foreach (var c in deploymentContainers)
            {
                if (string.Compare(c.ID, amiID, StringComparison.InvariantCultureIgnoreCase) == 0)
                    return c.ContainerName;
            }

            return string.Empty;
        }

        /// <summary>
        /// Looks up the ID of the ami suitable for web deployment to the given service in the 
        /// specified region and to a logical container name (aka, Windows Server name)
        /// </summary>
        /// <remarks>
        /// Currently this method holds only for images used with CloudFormation, since the 
        /// images for Beanstalk are tied into the solution stack
        /// </remarks>
        /// <param name="service"></param>
        /// <param name="region"></param>
        /// <param name="containerName"></param>
        /// <returns></returns>
        public string QueryWebDeploymentAMI(HostService service, string region, string containerName)
        {
            if (service == HostService.ElasticBeanstalk)
                throw new ArgumentException("Only CloudFormation amis are defined at this time.");

            string serviceName = ServiceNameFromEnum(service);
            var cfdeployments = from el in _amiManifest.Elements(webDemploymentCategoryValue)
                                where (string)el.Attribute("service") == serviceName
                                select el;

            var amiID = from container in cfdeployments.Elements("container")
                        where (string.Compare((string)container.Attribute("name"), 
                                                containerName, 
                                                StringComparison.InvariantCultureIgnoreCase) == 0)
                            let ami = (from el in container.Elements("region")
                                        where (string) el.Attribute("systemname") == region
                                        select el)
                            select ami.First().Value;

            return amiID.FirstOrDefault();
        }

        /// <summary>
        /// Returns a collection of containers and associated amis for a service in a given region, with
        /// an indication of which container is considered the default. This method is intended for UIs that
        /// present a list of possible containers for the user to choose from.
        /// </summary>
        public IEnumerable<ContainerAMI> QueryWebDeploymentContainers(HostService service, string region)
        {
            if (service == HostService.ElasticBeanstalk)
                throw new ArgumentException("Only CloudFormation amis are defined at this time.");

            string serviceName = ServiceNameFromEnum(service);
            var cfdeployments = from el in _amiManifest.Elements(webDemploymentCategoryValue)
                                where (string) el.Attribute("service") == serviceName
                                select el;

            var deploymentContainers = (from container in cfdeployments.Elements("container")
                                          let ami = (from el in container.Elements("region")
                                                     where (string)el.Attribute("systemname") == region
                                                     select el)
                                          select new ContainerAMI
                                          {
                                              ContainerName = (string)container.Attribute("name"),
                                              ID = ami.First().Value,
                                          }).ToList();

            return deploymentContainers;
        }

        /// <summary>
        /// Returns the display name of the default container for the given service
        /// </summary>
        /// <param name="service"></param>
        /// <param name="region"></param>
        /// <returns></returns>
        public string QueryDefaultWebDeploymentContainer(HostService service)
        {
            string serviceName = ServiceNameFromEnum(service);
            var cfdeployments = from el in _amiManifest.Elements(webDemploymentCategoryValue)
                                where (string)el.Attribute("service") == serviceName
                                select el;

            var defaultContainerName = from container in cfdeployments.Elements("container")
                                       let isDefaultAttr = container.Attribute("isDefault")
                                       let isDefaultContainer = isDefaultAttr != null && bool.Parse(isDefaultAttr.Value)
                                       where isDefaultContainer
                                       select (string) container.Attribute("name");
            return defaultContainerName.FirstOrDefault();
        }

        private string ServiceNameFromEnum(HostService service)
        {
            string serviceName = Enum.GetName(typeof (HostService), service);
            if (!string.IsNullOrEmpty(serviceName))
                return serviceName.ToLowerInvariant();
            else
                throw new ArgumentException("Unable to convert HostService enum value");
        }

        private ToolkitAMIManifest()
        {
            try
            {
                string amiManifest = S3FileFetcher.Instance.GetFileContent(TOOLKITAMI_INFO_FILE);
                _amiManifest = XElement.Parse(amiManifest);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error downloading/parsing template manifest file {0}", e);
            }
        }

        /// <summary>
        /// Documents an ami for a given container type
        /// </summary>
        public class ContainerAMI
        {
            public string ID { get; internal set; }
            public string ContainerName { get; internal set; }
        }
    }
}
