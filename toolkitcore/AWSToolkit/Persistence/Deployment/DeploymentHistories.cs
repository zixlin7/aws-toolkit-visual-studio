using System;
using System.Collections.Generic;

namespace Amazon.AWSToolkit.Persistence.Deployment
{
    // marker base to allow type restriction on template
    public interface IDeploymentHistory
    {
        DateTime DeployedAt { get; set; }
        // set during redeployment probing if we find the data instance
        // can be stripped from the .suo file
        bool IsInvalid { get; set; }
    }

    public class BeanstalkDeploymentHistory : IDeploymentHistory
    {
        public BeanstalkDeploymentHistory(string appName, string appVersion, bool isCustomVersion, string envName)
            : this(appName, appVersion, isCustomVersion, envName, false, string.Empty, null)
        {
        }

        public BeanstalkDeploymentHistory(string appName, 
                                          string appVersion, 
                                          bool isCustomVersion, 
                                          string envName,
                                          bool incrementalDeployment,
                                          string incrementalPushRepositoryLocation,
                                          string buildConfiguration)
        {
            this.ApplicationName = appName;
            this.ApplicationVersion = appVersion;
            this.IsCustomApplicationVersion = isCustomVersion;
            this.EnvironmentName = envName;
            this.BuildConfiguration = buildConfiguration;

            this.IsIncrementalDeployment = incrementalDeployment;
            this.IncrementalPushRepositoryLocation = incrementalPushRepositoryLocation;
            
            // set an initial dt that can be overridden - allows us to load historical
            // data that didn't have this persisted
            this.DeployedAt = DateTime.UtcNow;
        }

        public string ApplicationName { get; set; }
        public string ApplicationVersion { get; set; }
        public bool IsCustomApplicationVersion { get; set; }
        public string EnvironmentName { get; set; }
        public string BuildConfiguration { get; set; }
        public bool IsIncrementalDeployment { get; set; }
        public string IncrementalPushRepositoryLocation { get; set; }
        public DateTime DeployedAt { get; set; }
        public bool IsInvalid { get; set; }
    }

    public class CloudFormationDeploymentHistory : IDeploymentHistory
    {
        public CloudFormationDeploymentHistory(string lastStack)
        {
            this.LastStack = lastStack;
            // set an initial dt that can be overridden - allows us to load historical
            // data that didn't have this persisted
            this.DeployedAt = DateTime.UtcNow;
        }

        public string LastStack { get; set; }
        public DateTime DeployedAt { get; set; }
        public bool IsInvalid { get; set; }
    }

    public class TemplateDeploymentHistory : IDeploymentHistory
    {
        public TemplateDeploymentHistory(string templateFilename, string lastStack)
            : this(templateFilename, lastStack, null)
        {
        }

        public TemplateDeploymentHistory(string templateFilename, string lastStack, IDictionary<string, object> templateProperties)
        {
            this.TemplateFilename = templateFilename;
            this.LastStack = lastStack;
            if (templateProperties != null)
                this.TemplateProperties = new Dictionary<string,object>(templateProperties);
            // set an initial dt that can be overridden - allows us to load historical
            // data that didn't have this persisted
            this.DeployedAt = DateTime.UtcNow;
        }

        public string TemplateFilename { get; set; }
        public string LastStack { get; set; }
        public Dictionary<string, object> TemplateProperties { get; set; }
        public DateTime DeployedAt { get; set; }
        public bool IsInvalid { get; set; }
    }

    /// <summary>
    /// Templatised class to allow service deployment persistence to be passed between
    /// the deployment wizard's template/redeployment selector page and eventual
    /// persistence in the solution option (.suo) file. Templatised type is 
    /// responsible for building the actual data for a service deployment to a
    /// given region.
    /// </summary>
    public class DeploymentHistories<T> where T:IDeploymentHistory
    {
        // <accountid: <region, T>>
        readonly Dictionary<string, Dictionary<string, T>> _deploymentHistories = new Dictionary<string, Dictionary<string, T>>();

        public Dictionary<string, T> DeploymentsForAccount(string accountID)
        {
            if (_deploymentHistories.ContainsKey(accountID))
                return _deploymentHistories[accountID];

            return new Dictionary<string, T>();
        }

        public IEnumerable<string> Accounts => _deploymentHistories.Keys;

        // might want to tighten this to has accounts and at least one has a deployment stack
        public bool HasPriorDeployments => _deploymentHistories.Keys.Count > 0;

        public bool HasPriorDeploymentsForAccount(string accountID)
        {
            bool ret = false;

            if (_deploymentHistories.ContainsKey(accountID))
                ret = _deploymentHistories[accountID].Count > 0;

            return ret;
        }

        public void AddDeployment(string accountID, string region, T deploymentHistory)
        {
            Dictionary<string, T> deploymentsByRegion = null;
            if (_deploymentHistories.ContainsKey(accountID))
                deploymentsByRegion = _deploymentHistories[accountID];
            else
            {
                deploymentsByRegion = new Dictionary<string, T>();
                _deploymentHistories.Add(accountID, deploymentsByRegion);
            }

            if (deploymentsByRegion.ContainsKey(region))
                deploymentsByRegion[region] = deploymentHistory;
            else
                deploymentsByRegion.Add(region, deploymentHistory);
        }
    }
}
