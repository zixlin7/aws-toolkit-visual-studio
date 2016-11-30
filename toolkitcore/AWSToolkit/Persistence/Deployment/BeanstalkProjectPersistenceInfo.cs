using System;
using System.Collections.Generic;
using System.Globalization;
using Amazon.AWSToolkit.PluginServices.Deployment;
using ThirdParty.Json.LitJson;

namespace Amazon.AWSToolkit.Persistence.Deployment
{
    /// <summary>
    /// Handles persistence of DeploymentTypeIdentifiers.VSToolkitDeployment -type deployments to Beanstalk
    /// </summary>
    public class BeanstalkProjectPersistenceInfo : ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase
    {
        const string ACCOUNTID = "AccountID";
        const string REGION = "Region";
        const string DEPLOYMENTS = "Deployments";
        const string REGIONS = "Regions";

        // per regional deployment
        const string APPNAME = "ApplicationName";
        const string APPVERSION = "ApplicationVersion";
        const string ISCUSTOMAPPVERSION = "IsCustomApplicationVersion";
        const string ENVNAME = "EnvironmentName";
        const string BUILDCONFIG = "BuildConfiguration";     // in 'configuration name|platform' format, as user sees it

        // handle incremental as two properties, to allow users to turn it off and then
        // back on without necessarily changing the temp repository location (in case
        // we ever let them change it)
        const string INCREMENTALMODE = "IncrementalMode";
        const string INCREMENTALRESPOSLOCN = "IncrementalRepositoryLocation";

        readonly DeploymentHistories<BeanstalkDeploymentHistory> _deploymentHistories = new DeploymentHistories<BeanstalkDeploymentHistory>();

        public DeploymentHistories<BeanstalkDeploymentHistory> PreviousDeployments
        {
            get { return _deploymentHistories; }
        }

        public override string ServiceOwner { get { return DeploymentServiceIdentifiers.BeanstalkServiceName; } }

        /// <summary>
        /// Serializes to existing Json object context
        /// </summary>
        /// <param name="writer"></param>
        public override void Serialize(JsonWriter writer)
        {
            base.Serialize(writer);

            writer.WritePropertyName(REGION);
            writer.Write(LastRegionDeployedTo);

            writer.WritePropertyName(DEPLOYMENTS);
            writer.WriteArrayStart();

            foreach (var accountID in _deploymentHistories.Accounts)
            {
                var deployments = _deploymentHistories.DeploymentsForAccount(accountID);
                if (deployments != null && deployments.Count > 0)
                {
                    writer.WriteObjectStart();

                    writer.WritePropertyName(ACCOUNTID);
                    writer.Write(accountID);

                    writer.WritePropertyName(REGIONS);
                    writer.WriteArrayStart();

                    foreach (var region in deployments.Keys)
                    {
                        if (deployments[region].IsInvalid)
                            continue;

                        writer.WriteObjectStart();

                        writer.WritePropertyName(REGION);
                        writer.Write(region);

                        writer.WritePropertyName(APPNAME);
                        writer.Write(deployments[region].ApplicationName);

                        writer.WritePropertyName(APPVERSION);
                        writer.Write(deployments[region].ApplicationVersion);

                        writer.WritePropertyName(ISCUSTOMAPPVERSION);
                        writer.Write(deployments[region].IsCustomApplicationVersion);

                        writer.WritePropertyName(ENVNAME);
                        writer.Write(deployments[region].EnvironmentName);

                        writer.WritePropertyName(BUILDCONFIG);
                        writer.Write(deployments[region].BuildConfiguration);

                        writer.WritePropertyName(INCREMENTALMODE);
                        writer.Write(deployments[region].IsIncrementalDeployment);

                        writer.WritePropertyName(INCREMENTALRESPOSLOCN);
                        writer.Write(deployments[region].IncrementalPushRepositoryLocation);

                        writer.WritePropertyName(DEPLOYEDAT);
                        writer.Write(deployments[region].DeployedAt.ToUniversalTime().ToString(ISO8601BasicDateTimeFormat));

                        writer.WriteObjectEnd();
                    }

                    writer.WriteArrayEnd();

                    writer.WriteObjectEnd();
                }
            }

            writer.WriteArrayEnd();
        }

        public override bool Deserialize(int layoutVersion, JsonData data)
        {
            if (base.Deserialize(layoutVersion, data))
            {
                if (data[REGION] != null)
                    LastRegionDeployedTo = (string)data[REGION];

                if (data[DEPLOYMENTS] == null) 
                    return true;

                var deployments = data[DEPLOYMENTS];
                for (var i = 0; i < deployments.Count; i++)
                {
                    var deployment = deployments[i];

                    var account = (string)deployment[ACCOUNTID];

                    var regions = deployment[REGIONS];
                    for (var r = 0; r < regions.Count; r++)
                    {
                        var region = regions[r];
                        var regionName = (string)region[REGION];
                        var appName = (string)region[APPNAME];
                        var appVersion = (string)region[APPVERSION];
                        var isCustomVersion = (bool)region[ISCUSTOMAPPVERSION];
                        var envName = (string)region[ENVNAME];
                        // may be dealing with persisted info from legacy wizard, so we can't
                        // assume release build if not present
                        string buildConfiguration = null;
                        if (region[BUILDCONFIG] != null)
                        {
                            buildConfiguration = (string)region[BUILDCONFIG];
                            // Up to and including toolkit v1.8.1.0, we only persisted the build configuration name and 
                            // discarded the platform. We found we need the platform data to be able to build some more 
                            // complex solution layouts as well as projects with platforms not equal to 'any cpu'.
                            // If the persisted data does not contain a platform, force 'Any CPU' as this is the most 
                            // likely and will allow the deployment wizard to correctly set the build config data for 
                            // redeploy.
                            if (!buildConfiguration.Contains("|"))
                                buildConfiguration = buildConfiguration + "|Any CPU";
                        }

                        var bdh = new BeanstalkDeploymentHistory(appName, appVersion, isCustomVersion, envName);
                        if (buildConfiguration != null)
                            bdh.BuildConfiguration = buildConfiguration;

                        if (region[INCREMENTALMODE] != null)
                            bdh.IsIncrementalDeployment = (bool)region[INCREMENTALMODE];
                        if (region[INCREMENTALRESPOSLOCN] != null)
                            bdh.IncrementalPushRepositoryLocation = (string)region[INCREMENTALRESPOSLOCN];

                        if (region[DEPLOYEDAT] != null)
                        {
                            DateTime dt;
                            if (DateTime.TryParseExact((string)region[DEPLOYEDAT], 
                                                        ISO8601BasicDateTimeFormat, 
                                                        CultureInfo.InvariantCulture, 
                                                        DateTimeStyles.RoundtripKind, 
                                                        out dt))
                                bdh.DeployedAt = dt;
                        }

                        PreviousDeployments.AddDeployment(account, regionName, bdh);
                    }
                }

                return true;
            }

            return false;
        }
    }
}
