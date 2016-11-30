using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Globalization;

using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.PluginServices.Deployment;

namespace Amazon.AWSToolkit.Persistence.Deployment
{
    /// <summary>
    /// Records set the set of deployments for a CloudFormation template. This format
    /// differs from VS Toolkit web app/web site deployments since we don't really
    /// care about account/region info, just the set of property overrides that
    /// were used with a given template file.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TemplateDeploymentHistories<T> where T : IDeploymentHistory
    {
        // holds deployments for all templates in a project, keyed by name
        Dictionary<string, TemplateDeploymentHistory> _deploymentHistories = new Dictionary<string, TemplateDeploymentHistory>();

        public string LastDeployedTemplate { get; private set; }

        public TemplateDeploymentHistory DeploymentForTemplate(string templateUri)
        {
            if (_deploymentHistories.ContainsKey(templateUri))
                return _deploymentHistories[templateUri];

            return null;
        }

        public IDictionary<string, TemplateDeploymentHistory> Deployments
        {
            get { return _deploymentHistories; }
        }

        public void AddDeployment(T deploymentHistory)
        {
            var tdh = deploymentHistory as TemplateDeploymentHistory;
            if (_deploymentHistories.ContainsKey(tdh.TemplateFilename))
                _deploymentHistories[tdh.TemplateFilename] = tdh;
            else
                _deploymentHistories.Add(tdh.TemplateFilename, tdh);

            LastDeployedTemplate = tdh.TemplateFilename;
        }
    }

    /// <summary>
    /// Handles persistence of DeploymentTypeIdentifiers.CFNTemplateDeployment -type deployments to CloudFormation
    /// </summary>
    public class CloudFormationTemplatePersistenceInfo : ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase
    {
        const string ACCOUNTID = "AccountID";
        const string REGION = "Region";
        const string DEPLOYMENTS = "Deployments";
        const string REGIONS = "Regions";
        const string LASTSTACK = "LastStack";
        const string LASTTEMPLATE = "LastTemplate";
        const string TEMPLATEFILE = "TemplateFile";
        const string TEMPLATEPROPS = "TemplateProps";

        public override string ServiceOwner { get { return DeploymentServiceIdentifiers.CloudFormationServiceName; } }

        public override string DeploymentType
        {
            get { return DeploymentTypeIdentifiers.CFNTemplateDeployment; }
        }

        TemplateDeploymentHistories<TemplateDeploymentHistory> _deploymentHistories = new TemplateDeploymentHistories<TemplateDeploymentHistory>();

        public TemplateDeploymentHistories<TemplateDeploymentHistory> PreviousDeployments
        {
            get { return _deploymentHistories; }
        }

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

            foreach (string templateUri in _deploymentHistories.Deployments.Keys)
            {
                TemplateDeploymentHistory tdh = _deploymentHistories.DeploymentForTemplate(templateUri);

                writer.WriteObjectStart();

                    writer.WritePropertyName(TEMPLATEFILE);
                    writer.Write(tdh.TemplateFilename);

                    // may not be set if persisting cost estimation 'deployment'
                    writer.WritePropertyName(LASTSTACK);
                    if (!string.IsNullOrEmpty(tdh.LastStack))
                        writer.Write(tdh.LastStack);
                    else
                        writer.Write(string.Empty);

                    writer.WritePropertyName(TEMPLATEPROPS);
                        writer.WriteObjectStart();
                        if (tdh.TemplateProperties != null)
                        {
                            foreach (string propKey in tdh.TemplateProperties.Keys)
                            {
                                writer.WritePropertyName(propKey);
                                writer.Write(tdh.TemplateProperties[propKey].ToString());
                            }
                        }
                        writer.WriteObjectEnd();

                writer.WriteObjectEnd();
            }

            writer.WriteArrayEnd();
        }

        public override bool Deserialize(int layoutVersion, JsonData data)
        {
            if (!base.Deserialize(layoutVersion, data))
                return false;

            if (data[REGION] != null)
                LastRegionDeployedTo = (string)data[REGION];

            if (data[DEPLOYMENTS] != null)
            {
                JsonData deployments = data[DEPLOYMENTS];
                for (int i = 0; i < deployments.Count; i++)
                {
                    JsonData deployment = deployments[i];

					// ignore premature persistence data from Norm's shipment
					// of the tool to alpha testers! (stack name/props is allowed
					// to be empty)
					if (deployment[TEMPLATEFILE] == null)
						continue;
						
                    string templateName = (string)deployment[TEMPLATEFILE];
                    string stackName = string.Empty;
                    if (deployment[LASTSTACK] != null)
                        stackName = (string)deployment[LASTSTACK];

                    var templateProps = new Dictionary<string, object>();
					if (deployment[TEMPLATEPROPS] != null)
					{
						JsonData props = deployment[TEMPLATEPROPS];
						foreach (string key in props.PropertyNames)
						{
							string propVal = (string)props[key];
							templateProps.Add(key, propVal);
						}
					}
					
                    TemplateDeploymentHistory tdh = new TemplateDeploymentHistory(templateName, stackName);
                    tdh.TemplateProperties = templateProps; // avoids unnecessary copying
                    PreviousDeployments.AddDeployment(tdh);
                }
            }

            return true;
        }

    }
}
