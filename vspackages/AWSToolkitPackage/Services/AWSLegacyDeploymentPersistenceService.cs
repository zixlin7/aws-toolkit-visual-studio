using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.PluginServices.Deployment;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.VisualStudio.Shared;
using Microsoft.VisualStudio.Shell;
using System.IO;
using Microsoft.VisualStudio.Project;

namespace Amazon.AWSToolkit.VisualStudio.Services
{
    internal class AWSLegacyDeploymentPersistenceService : SAWSLegacyDeploymentPersistence, IAWSLegacyDeploymentPersistence
    {
        private AWSToolkitPackage _hostPackage;

        public AWSLegacyDeploymentPersistenceService(AWSToolkitPackage hostPackage)
        {
            _hostPackage = hostPackage;
        }

        #region IAWSLegacyDeploymentPersistence

        public Persistence.Deployment.ProjectDeploymentsPersistenceManager PersistenceManager => _hostPackage.LegacyDeploymentsPersistenceManager;

        public Dictionary<string, object> SetTemplateDeploymentSeedData(string projectGuid, string templateUri)
        {
            var seedData = new Dictionary<string, object>();
            if (PersistenceManager.PersistedDeployments(projectGuid) > 0)
            {
                var deployments = PersistenceManager[projectGuid];
                var cftpi = deployments.DeploymentForService(DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.CFNTemplateDeployment)
                                as CloudFormationTemplatePersistenceInfo;
                if (cftpi != null)
                {
                    var deploymentHistory = cftpi.PreviousDeployments;
                    if (deploymentHistory != null)
                    {
                        var tdh = deploymentHistory.DeploymentForTemplate(templateUri);
                        if (tdh != null)
                        {
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, cftpi.AccountUniqueID);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, cftpi.LastRegionDeployedTo);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_SeedName, tdh.LastStack);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_TemplateProperties, tdh.TemplateProperties);
                        }
                    }
                }
            }

            return seedData;
        }

        /// <summary>
        /// Persists data returned from the deployment of a template to CloudFormation
        /// </summary>
        /// <param name="persistableData"></param>
        public void PersistTemplateDeployment(EnvDTE.ProjectItem prjItem, DeployedTemplateData persistableData)
        {
            if (persistableData == null)
                return;

            var projectGuid = VSUtility.QueryProjectIDGuid(prjItem.ContainingProject);
            var cftpi = _hostPackage.GetPersistedInfoForService(projectGuid, DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.CFNTemplateDeployment)
                            as CloudFormationTemplatePersistenceInfo;
            cftpi.AccountUniqueID = persistableData.Account.SettingsUniqueKey;
            cftpi.LastRegionDeployedTo = persistableData.Region.Id;

            var persistenceKey = CalcPersistenceKeyForProjectItem(prjItem);
            var tdh = new TemplateDeploymentHistory(persistenceKey, persistableData.StackName, persistableData.TemplateProperties);
            cftpi.PreviousDeployments.AddDeployment(tdh);
            PersistenceManager.SetLastServiceDeploymentForProject(projectGuid,
                                                                  DeploymentServiceIdentifiers.CloudFormationServiceName,
                                                                  DeploymentTypeIdentifiers.CFNTemplateDeployment);
        }

        /// <summary>
        /// To allow for templates in sub folders with the same name, attempt to make the full path for
        /// the template into a relative path. If we cannot make it relative, give up and use just the
        /// filename we have.
        /// </summary>
        /// <param name="prjItem"></param>
        /// <returns></returns>
        public string CalcPersistenceKeyForProjectItem(EnvDTE.ProjectItem prjItem)
        {
            string fileNameKey = null;
            try
            {
                var projPath = prjItem.ContainingProject.Properties.Item("FullPath").Value.ToString();
                var filePath = prjItem.Properties.Item("FullPath").Value.ToString();

                fileNameKey = PackageUtilities.MakeRelative(projPath, filePath);
                if (Path.IsPathRooted(fileNameKey))
                    fileNameKey = null;
            }
            catch (Exception e)
            {
                _hostPackage.Logger.DebugFormat("Exception attempting to set relative filepath for template {0} - {1}. Filename will be used for persistence key",
                                                (prjItem.Object as FileNode).FileName,
                                                e.Message);
            }
            finally
            {
                if (string.IsNullOrEmpty(fileNameKey))
                    fileNameKey = (prjItem.Object as FileNode).FileName;
            }

            return fileNameKey;
        }

        #endregion

        private AWSLegacyDeploymentPersistenceService() { }
    }
}
