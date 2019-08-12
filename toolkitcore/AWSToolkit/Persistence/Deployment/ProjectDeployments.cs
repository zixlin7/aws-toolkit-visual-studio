using System;
using System.Collections.Generic;
using System.Text;
using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Account;

namespace Amazon.AWSToolkit.Persistence.Deployment
{
    /// <summary>
    /// Class tracking last-run deployment options for one or more
    /// projects in a solution. Data held here will be persisted
    /// on a per-project basis
    /// </summary>
    public class ProjectDeploymentsPersistenceManager
    {
        object _syncLock = new object();

        // v2 was cloudformation web project/site deployments only
        // v3 added web project/site deployments to beanstalk too
        // v4 added deployment types to the mix for cloudformation; either web project/site deployments (aka vstoolkit type)
        // or template deployments (from cfn editor)
        readonly int _dataLayoutVersion = 4; 

        internal static class JsonDocumentFields
        {
            internal const string VERSION = "Version";
            internal const string PROJECTS = "Projects";  

            // the guid identifying the project in the solution
            internal const string PROJECT_GUID = "ProjectGuid";

            // the AWS service involved in the last deployment run; this dictates which
            // data blob will be used to seed the deployment wizard on next run for this
            // project
            internal const string PROJECT_LASTDEPLOYEDSERVICE = "LastDeployedService";
            
            // the unique toolkit account id last-used to deploy this project
            internal const string PROJECT_ACCOUNTUNIQUEID = "AccountUniqueID";
            
            // the last region to which this project had a deployment
            internal const string PROJECT_REGION = "Region";

            // the set of deployments for this project, to all deployment services used
            internal const string PROJECT_SERVICEDEPLOYMENTS = "ServiceDeployments";

            // which service accepted the deployment
            internal const string SERVICE_OWNER = "ServiceOwner";

            // what type of deployment a particular service deployment was
            internal const string DEPLOYMENT_TYPE = "DeploymentType";
        }

        /// <summary>
        /// Tracks all persisted data for a given project, crossing all services and deployment types
        /// </summary>
        public class PersistedProjectDeployments
        {
            public PersistedProjectDeployments()
            {
                this.LastServiceDeployment = string.Empty;
                this.ProjectDeployments = new List<PersistedProjectInfoBase>();
            }

            /// <summary>
            /// The name of the service that this project was last-deployed to
            /// </summary>
            public string LastServiceDeployment { get; set; }

            /// <summary>
            /// The type of deployment that was performed to LastServiceDeployment
            /// </summary>
            public string LastDeploymentType { get; set; }

            /// <summary>
            /// The full set of derived deployments info for the project (one 
            /// PersistedProjectInfoBase-derived instance per service/deployment 
            /// type the project has been deployed to)
            /// </summary>
            public List<PersistedProjectInfoBase> ProjectDeployments { get; }

            /// <summary>
            /// If we can accurately determine the last service and stack/environment the project
            /// was deployed to, format the menu text to reflect it otherwise return a generic
            /// entry for the service used.
            /// </summary>
            /// <returns></returns>
            public string QueryRepublishCommandText(string deploymentType)
            {
                try
                {
                    IDeploymentHistory deploymentHistory = LastDeploymentOfType(deploymentType);
                    if (deploymentHistory != null)
                    {
                        if (deploymentHistory is BeanstalkDeploymentHistory)
                        {
                            BeanstalkDeploymentHistory lastDeployment = deploymentHistory as BeanstalkDeploymentHistory;
                            return string.Format("Republish to Environment '{0}'", lastDeployment.EnvironmentName);
                        }
                        
                        if (deploymentHistory is CloudFormationDeploymentHistory)
                        {
                            CloudFormationDeploymentHistory lastDeployment = deploymentHistory as CloudFormationDeploymentHistory;
                            return string.Format("Republish to Stack '{0}'", lastDeployment.LastStack);
                        }

                    }
                }
                catch { }
                return null;
            }

            /// <summary>
            /// Returns the last-deployment history record from the persisted data; this will be scoped
            /// by the last account used, the service, deployment type and the region
            /// </summary>
            public IDeploymentHistory LastDeploymentOfType(string deploymentType)
            {
                try
                {
                    var serviceDeployments = DeploymentForService(LastServiceDeployment, deploymentType);
                    if (LastServiceDeployment == DeploymentServiceIdentifiers.BeanstalkServiceName)
                    {
                        BeanstalkProjectPersistenceInfo bppi = serviceDeployments as BeanstalkProjectPersistenceInfo;
                        if (bppi == null)
                            return null;

                        var regionalDeployments = bppi.PreviousDeployments.DeploymentsForAccount(bppi.AccountUniqueID);
                        return regionalDeployments[bppi.LastRegionDeployedTo];
                    }
                    else
                    {
                        switch (deploymentType)
                        {
                            case DeploymentTypeIdentifiers.VSToolkitDeployment:
                                {
                                    CloudFormationProjectPersistenceInfo cfppi = serviceDeployments as CloudFormationProjectPersistenceInfo;
                                    var regionalDeployments = cfppi.PreviousDeployments.DeploymentsForAccount(cfppi.AccountUniqueID);
                                    return regionalDeployments[cfppi.LastRegionDeployedTo];
                                }

                            case DeploymentTypeIdentifiers.CFNTemplateDeployment:
                                {
                                    CloudFormationTemplatePersistenceInfo cftpi = serviceDeployments as CloudFormationTemplatePersistenceInfo;
                                    string lastTemplate = cftpi.PreviousDeployments.LastDeployedTemplate;
                                    if (!string.IsNullOrEmpty(lastTemplate))
                                        return cftpi.PreviousDeployments.DeploymentForTemplate(lastTemplate);
                                }
                                break;

                            default:
                                break;
                        }
                    }
                }
                catch { }

                return null;
            }

            public void AddProjectInfo(PersistedProjectInfoBase info)
            {
                this.ProjectDeployments.Add(info);
            }

            /// <summary>
            /// Returns the deployment history for a specific service, or null if none
            /// are available
            /// </summary>
            /// <param name="serviceName"></param>
            /// <returns></returns>
            public PersistedProjectInfoBase DeploymentForService(string serviceName, string deploymentType)
            {
                foreach (PersistedProjectInfoBase ppib in ProjectDeployments)
                {
                    if (ppib.ServiceOwner == serviceName && ppib.DeploymentType == deploymentType)
                        return ppib;
                }

                return null;
            }
        }

        /// <summary>
        /// Class tracks persistable per-service deployment data for a given IDE project
        /// </summary>
        public abstract class PersistedProjectInfoBase
        {
            // to support MRU-style listing for multiple deployments of one project
            public const string ISO8601BasicDateTimeFormat = "yyyyMMddTHHmmssZ";
            public const string DEPLOYEDAT = "DeployedAt";

            public PersistedProjectInfoBase() { }

            /// <summary>
            /// Internal id of the account which had last-activity on the project
            /// </summary>
            public string AccountUniqueID { get; set; }

            /// <summary>
            /// For the account that was used, records the region that the deployment
            /// went to. Along with account, this is enough to identify the exact
            /// history record for that deployment. 
            /// </summary>
            /// <remarks>Set and maintained by derived persistence classes</remarks>
            public string LastRegionDeployedTo { get; set; }

            /// <summary>
            /// Returns the service name to which the persistence data applies; this is
            /// used to distinguish the derived types when deserializing the stream
            /// </summary>
            public abstract string ServiceOwner { get; }

            /// <summary>
            /// Returns the type of the deployment to ServiceOwner that the persisted
            /// data corresponds to. For legacy, the default type is always a VS toolkit
            /// web app/web site deployment. Beanstalk only supports this type; CloudFormation
            /// supports both toolkit and template deployments.
            /// </summary>
            public virtual string DeploymentType => DeploymentTypeIdentifiers.VSToolkitDeployment;

            /// <summary>
            /// Saves the unique toolkit account id that was used with this deployment service.
            /// </summary>
            /// <param name="writer"></param>
            public virtual void Serialize(JsonWriter writer)
            {
                writer.WritePropertyName(JsonDocumentFields.PROJECT_ACCOUNTUNIQUEID);
                writer.Write(AccountUniqueID);
            }

            /// <summary>
            /// Reads the last-used account id with this deployment service.
            /// </summary>
            /// <param name="layoutVersion"></param>
            /// <param name="data"></param>
            /// <returns></returns>
            public virtual bool Deserialize(int layoutVersion, JsonData data)
            {
                if (data[JsonDocumentFields.PROJECT_ACCOUNTUNIQUEID] == null)
                    return false;

                AccountUniqueID = (string)data[JsonDocumentFields.PROJECT_ACCOUNTUNIQUEID];
                return true;
            }
        }

        // key is VS project ID guid; a single project can be deployed to multiple different services 
        Dictionary<string, PersistedProjectDeployments> _persistedProjects = new Dictionary<string, PersistedProjectDeployments>();

        // factory instantiates a derived-PersistableProjectInfo instance for a given deployment service
        PersistableProjectInfoFactory _ppiFactory;
        public delegate PersistedProjectInfoBase PersistableProjectInfoFactory(string ownerServiceName, string deploymentType);

        public ProjectDeploymentsPersistenceManager(PersistableProjectInfoFactory ppiFactory) 
        {
            if (ppiFactory == null)
                throw new ArgumentNullException();

            _ppiFactory = ppiFactory;
        }

        private ProjectDeploymentsPersistenceManager() { }

        public void ClearDeployments()
        {
            lock (_syncLock)
                _persistedProjects.Clear();
        }

        public int Count
        {
            get 
            {
                int count;
                lock (_syncLock)
                    count = _persistedProjects.Count;

                return count;
            }
        }

        public void SetLastServiceDeploymentForProject(string projectGuid, string serviceName, string deploymentType)
        {
            PersistedProjectDeployments ppd = this[projectGuid];
            ppd.LastServiceDeployment = serviceName;
            ppd.LastDeploymentType = deploymentType;
        }

        /// <summary>
        /// Returns the persistable data object for a given project, creating a new
        /// instance if the project has never been deployed in the current solution
        /// </summary>
        /// <param name="projectGuid"></param>
        /// <returns>Collection of persisted deployments</returns>
        public PersistedProjectDeployments this[string projectGuid]
        {
            get
            {
                PersistedProjectDeployments ppd = null;
                if (_persistedProjects.ContainsKey(projectGuid))
                    ppd = _persistedProjects[projectGuid];
                else
                {
                    ppd = new PersistedProjectDeployments();
                    _persistedProjects.Add(projectGuid, ppd);
                }
                return ppd;
            }
        }

        // if dev's trade .suo files, the account unique id will not be valid off
        // of the original machine which can cause trouble later and/or complicated code
        // to resolve it in the wizard - easier to verify now and kill existing deployment 
        // data in this scenario. Note that we do this on a per-deployment basis before
        // killing the entire structure.
        public int PersistedDeployments(string projectGuid)
        {
            int persistedDeployments = 0;
            if (_persistedProjects.ContainsKey(projectGuid))
            {
                PersistedProjectDeployments ppd = _persistedProjects[projectGuid];
                for (int i = ppd.ProjectDeployments.Count - 1; i >= 0; i--)
                {
                    PersistedProjectInfoBase info = ppd.ProjectDeployments[i];
                    AccountViewModel account = ToolkitFactory
                                                    .Instance
                                                    .RootViewModel
                                                    .AccountFromIdentityKey(info.AccountUniqueID);
                    if (account == null)
                        ppd.ProjectDeployments.RemoveAt(i);
                }

                if (ppd.ProjectDeployments.Count == 0)
                    _persistedProjects.Remove(projectGuid);
                else
                    persistedDeployments = ppd.ProjectDeployments.Count;
            }

            return persistedDeployments;
        }

        public string ToJson()
        {
            if (Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            JsonWriter writer = new JsonWriter(sb);

            writer.WriteObjectStart();

            writer.WritePropertyName(JsonDocumentFields.VERSION);
            writer.Write(_dataLayoutVersion);

            writer.WritePropertyName(JsonDocumentFields.PROJECTS);
            writer.WriteArrayStart();
            foreach (string projectGuid in _persistedProjects.Keys)
            {
                PersistedProjectDeployments ppd = _persistedProjects[projectGuid];
                if (ppd.ProjectDeployments.Count == 0)
                    continue;

                writer.WriteObjectStart();

                writer.WritePropertyName(JsonDocumentFields.PROJECT_GUID);
                writer.Write(projectGuid);
                
                writer.WritePropertyName(JsonDocumentFields.PROJECT_LASTDEPLOYEDSERVICE);
                writer.Write(ppd.LastServiceDeployment);

                writer.WritePropertyName(JsonDocumentFields.DEPLOYMENT_TYPE);
                writer.Write(ppd.LastDeploymentType);
 
                writer.WritePropertyName(JsonDocumentFields.PROJECT_SERVICEDEPLOYMENTS);
                writer.WriteArrayStart();

                    foreach (PersistedProjectInfoBase info in ppd.ProjectDeployments)
                    {
                        writer.WriteObjectStart();

                        writer.WritePropertyName(JsonDocumentFields.SERVICE_OWNER);
                        writer.Write(info.ServiceOwner);

                        writer.WritePropertyName(JsonDocumentFields.DEPLOYMENT_TYPE);
                        writer.Write(info.DeploymentType);

                        info.Serialize(writer);
                        writer.WriteObjectEnd();
                    }

                writer.WriteArrayEnd();

                writer.WriteObjectEnd();
            }

            writer.WriteArrayEnd();

            writer.WriteObjectEnd();

            return writer.TextWriter.ToString();
        }

        public void FromJson(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString))
                return;

            JsonData jdata = JsonMapper.ToObject(jsonString);

            int layoutVersion = _dataLayoutVersion;   // assume latest :-)
            if (jdata[JsonDocumentFields.VERSION] != null)
                layoutVersion = (int)jdata[JsonDocumentFields.VERSION];

            if (jdata[JsonDocumentFields.PROJECTS] != null)
            {
                JsonData projectsData = jdata[JsonDocumentFields.PROJECTS];

                // v2 layout only had cloudformation as deployment target, so each project entry
                // was a single PersistedProjectInfoBase 'instance'. In v3, we added beanstalk as
                // a target so each project 'instance' now becomes a list of one or two ppis
                foreach (JsonData projectData in projectsData)
                {
                    string projectGuid = (string)projectData[JsonDocumentFields.PROJECT_GUID];

                    if (layoutVersion == 2)
                    {
                        // passing empty string gives us v2 cloudformation/vstoolkit deployments instance
                        // for backwards compat
                        PersistedProjectInfoBase ppib = _ppiFactory(string.Empty, string.Empty);
                        if (ppib.Deserialize(layoutVersion, projectData))
                        {
                            PersistedProjectDeployments ppd = AddProjectPersistenceInfo(projectGuid, ppib);
                            // and of course we know the last service by default :-)
                            ppd.LastServiceDeployment = DeploymentServiceIdentifiers.CloudFormationServiceName;
                            ppd.LastDeploymentType = DeploymentTypeIdentifiers.VSToolkitDeployment;
                        }
                    }
                    else
                    {
                        PersistedProjectDeployments ppd = new PersistedProjectDeployments();
                        _persistedProjects.Add(projectGuid, ppd);
                        ppd.LastServiceDeployment = (string)projectData[JsonDocumentFields.PROJECT_LASTDEPLOYEDSERVICE];

                        if (projectData[JsonDocumentFields.DEPLOYMENT_TYPE] != null)
                            ppd.LastDeploymentType = (string)projectData[JsonDocumentFields.DEPLOYMENT_TYPE];
                        else
                            ppd.LastDeploymentType = DeploymentTypeIdentifiers.VSToolkitDeployment; // for pre-v4 layouts

                        JsonData serviceDeployments = projectData[JsonDocumentFields.PROJECT_SERVICEDEPLOYMENTS];
                        for (int i = 0; i < serviceDeployments.Count; i++)
                        {
                            JsonData serviceDeployment = serviceDeployments[i];

                            string serviceOwner = (string)serviceDeployment[JsonDocumentFields.SERVICE_OWNER];
                            string deploymentType = DeploymentTypeIdentifiers.VSToolkitDeployment;
                            if (serviceDeployment[JsonDocumentFields.DEPLOYMENT_TYPE] != null)
                                deploymentType = (string)serviceDeployment[JsonDocumentFields.DEPLOYMENT_TYPE];

                            PersistedProjectInfoBase ppib = _ppiFactory(serviceOwner, deploymentType);
                            if (ppib.Deserialize(layoutVersion, serviceDeployment))
                            {
                                ppd.AddProjectInfo(ppib);
                            }
                        }
                    }
                }
            }
        }

        public PersistedProjectDeployments AddProjectPersistenceInfo(string projectGuid, PersistedProjectInfoBase ppib)
        {
            PersistedProjectDeployments ppd = null;
            if (_persistedProjects.ContainsKey(projectGuid))
                ppd = _persistedProjects[projectGuid];
            else
            {
                ppd = new PersistedProjectDeployments();
                _persistedProjects.Add(projectGuid, ppd);
            }

            ppd.AddProjectInfo(ppib);
            return ppd;
        }
    }
}
