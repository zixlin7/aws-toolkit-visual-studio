using System.Collections.Generic;
using System.Runtime.InteropServices;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.Shared;

namespace Amazon.AwsToolkit.VsSdk.Common
{
    public abstract class AWSLegacyDeploymentPersistenceGuids
    {
        public const string IAWSLegacyDeploymentPersistenceIdentifier = "3867e280-e8ec-4ad7-967a-7cc334547346";
        public const string SAWSLegacyDeploymentPersistenceIdentifier = "8105a276-187e-4f52-aa2d-5bae0b3b2ade";
    }

    /// <summary>
    /// Interface to be implemented by the shell that is hosting the toolkit.
    /// </summary>
    [Guid(ShellProviderServiceGuids.IAWSToolkitShellProviderIdentifier)]
    [ComVisible(true)]
    public interface IAWSLegacyDeploymentPersistence
    {
        /// <summary>
        /// Returns the core persistence manager instance handling legacy deployment
        /// data contained in the .suo file.
        /// </summary>
        ProjectDeploymentsPersistenceManager PersistenceManager { get; }

        /// <summary>
        /// Populates a dictionary with any seed data available from a previous deployment.
        /// </summary>
        /// <param name="projectGuid"></param>
        /// <param name="templateUri"></param>
        /// <returns></returns>
        Dictionary<string, object> SetTemplateDeploymentSeedData(string projectGuid, string templateUri);

        void PersistTemplateDeployment(EnvDTE.ProjectItem prjItem, DeployedTemplateData persistableData);

        string CalcPersistenceKeyForProjectItem(EnvDTE.ProjectItem prjItem);
    }

    /// <summary>
    /// Marker interface exposing the core AWSToolkit across the rest of our toolkit packages
    /// </summary>
    [Guid(ShellProviderServiceGuids.SAWSToolkitShellProviderIdentifier)]
    public interface SAWSLegacyDeploymentPersistence
    {
    }
}
