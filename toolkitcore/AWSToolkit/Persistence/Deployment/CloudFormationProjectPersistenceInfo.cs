using System;
using System.Collections.Generic;
using System.Globalization;
using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.Persistence.Deployment
{
    /// <summary>
    /// Handles persistence of DeploymentTypeIdentifiers.VSToolkitDeployment -type deployments to CloudFormation
    /// </summary>
    public class CloudFormationProjectPersistenceInfo : ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase
    {
        const string ACCOUNTID = "AccountID";
        const string REGION = "Region";
        const string DEPLOYMENTS = "Deployments";
        const string REGIONS = "Regions";
        const string LASTSTACK = "LastStack";

        DeploymentHistories<CloudFormationDeploymentHistory> _deploymentHistories = new DeploymentHistories<CloudFormationDeploymentHistory>();

        public DeploymentHistories<CloudFormationDeploymentHistory> PreviousDeployments => _deploymentHistories;

        public override string ServiceOwner => DeploymentServiceIdentifiers.CloudFormationServiceName;

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

            foreach (string accountID in _deploymentHistories.Accounts)
            {
                Dictionary<string, CloudFormationDeploymentHistory> deployments = _deploymentHistories.DeploymentsForAccount(accountID);
                if (deployments != null && deployments.Count > 0)
                {
                    writer.WriteObjectStart();

                    writer.WritePropertyName(ACCOUNTID);
                    writer.Write(accountID);

                    writer.WritePropertyName(REGIONS);
                    writer.WriteArrayStart();

                        foreach (string region in deployments.Keys)
                        {
                            if (deployments[region].IsInvalid)
                                continue;

                            writer.WriteObjectStart();

                            writer.WritePropertyName(REGION);
                            writer.Write(region);

                            writer.WritePropertyName(LASTSTACK);
                            writer.Write(deployments[region].LastStack);

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
                if (layoutVersion > 1)
                {
                    if (data[REGION] != null)
                        LastRegionDeployedTo = (string)data[REGION];

                    if (data[DEPLOYMENTS] != null)
                    {
                        JsonData deployments = data[DEPLOYMENTS];
                        for (int i = 0; i < deployments.Count; i++)
                        {
                            JsonData deployment = deployments[i];

                            string account = (string)deployment[ACCOUNTID];

                            JsonData regions = deployment[REGIONS];
                            for (int r = 0; r < regions.Count; r++)
                            {
                                JsonData region = regions[r];
                                string regionName = (string)region[REGION];
                                string stackName = (string)region[LASTSTACK];

                                CloudFormationDeploymentHistory cfdh = new CloudFormationDeploymentHistory(stackName);

                                if (region[DEPLOYEDAT] != null)
                                {
                                    DateTime dt;
                                    if (DateTime.TryParseExact((string)region[DEPLOYEDAT],
                                                               ISO8601BasicDateTimeFormat,
                                                               CultureInfo.InvariantCulture,
                                                               DateTimeStyles.RoundtripKind,
                                                               out dt))
                                        cfdh.DeployedAt = dt;
                                }

                                PreviousDeployments.AddDeployment(account, regionName, cfdh);
                            }
                        }
                    }
                }
                else
                {
                    // special case migration of original toolkit release persistence, where
                    // we only held data for one region/stack per project
                    string stackName = null;
                    string region = null;

                    if (data[LASTSTACK] != null)
                        stackName = (string)data[LASTSTACK];

                    // default to us-east-1 for earlier .suo files where we did not persist this data
                    // but were using us-east
                    if (data[REGION] != null)
                        region = (string)data[REGION];
                    else
                        region = RegionEndPointsManager.GetInstance().GetDefaultRegionEndPoints().SystemName;

                    if (!string.IsNullOrEmpty(stackName))
                    {
                        // we have no datetime persisted for these
                        PreviousDeployments.AddDeployment(this.AccountUniqueID, region, new CloudFormationDeploymentHistory(stackName));
                    }
                }

                return true;
            }
            else
                return false;
        }

    }
}
