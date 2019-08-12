using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.PageControllers;

using Amazon.AWSToolkit.ElasticBeanstalk.Commands;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.ElasticBeanstalk;
using Amazon.AWSToolkit.PluginServices.Deployment;


namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class DeployApplicationVersionController
    {
        IAmazonElasticBeanstalk _beanstalkClient;

        public bool Execute(ApplicationViewModel applicationViewModel, string versionLabel)
        {            
            this._beanstalkClient = applicationViewModel.BeanstalkClient;

            var seedProperties = new Dictionary<string, object>
            {
                {CommonWizardProperties.propkey_NavigatorRootViewModel, ToolkitFactory.Instance.RootViewModel},
                {DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion, true},
                //{DeploymentWizardProperties.DeploymentTemplate.propkey_Redeploy, true},
                {DeploymentWizardProperties.DeploymentTemplate.propkey_RedeployVersion, true},
                {DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, applicationViewModel.AccountViewModel.SettingsUniqueKey},
                {CommonWizardProperties.AccountSelection.propkey_SelectedRegion, RegionEndPointsManager.GetInstance().GetRegion(applicationViewModel.ElasticBeanstalkRootViewModel.CurrentEndPoint.RegionSystemName)},
                {DeploymentWizardProperties.SeedData.propkey_SeedName, applicationViewModel.Application.ApplicationName},
                {DeploymentWizardProperties.AppOptions.propkey_TargetRuntime, "4.0"},
                {BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel, versionLabel}
            };

            // needed so pages know they are running in a Beanstalk-specific environment
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.BeanstalkServiceName;

            var template = LoadDeploymentTemplate(applicationViewModel.ElasticBeanstalkRootViewModel.CurrentEndPoint.RegionSystemName);
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = template;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.ElasticBeanstalk.View.PublishApplicationVersion", seedProperties);
            wizard.Title = "Publish Application Version";

            // register the page groups we expect child pages to fit into
            wizard.RegisterPageGroups(DeploymentWizardPageGroups.DeploymentPageGroups);

            var defaultPages = new IAWSWizardPageController[]
            {
                new WizardPages.PageControllers.Deployment.StartPageController(),
                new WizardPages.PageControllers.Deployment.ApplicationPageController(),
                new WizardPages.PageControllers.Deployment.AWSOptionsPageController(),
                new WizardPages.PageControllers.Deployment.VpcOptionsPageController(),
                new WizardPages.PageControllers.Deployment.ConfigureRollingDeploymentsController(),
                new WizardPages.PageControllers.Deployment.ApplicationOptionsPageController(),
                new WizardPages.PageControllers.Deployment.PseudoReviewPageController(),
                new CommonUI.LegacyDeploymentWizard.PageControllers.DeploymentReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
            {
                ThreadPool.QueueUserWorkItem(x =>
                    {
                        var command = new DeployNewApplicationCommand("FakePackage", wizard.CollectedProperties);
                        command.Execute();
                    });
                return true;
            }

            return false;
        }

        private DeploymentTemplateWrapperBase LoadDeploymentTemplate(string region)
        {
            string templateManifestContent = S3FileFetcher.Instance.GetFileContent(DeploymentTemplateWrapperBase.TEMPLATEMANIFEST_FILE);
            XElement templateManifest = XElement.Parse(templateManifestContent);

            IEnumerable<XElement> regions
                    = from el in templateManifest.Elements("region")
                      where (string)el.Attribute("systemname") == region                      
                      select el;

            IEnumerable<XElement> templates =
                    from el in regions.First().Elements()
                    where (string)el.Attribute("serviceOwner") == "ElasticBeanstalk"
                    select el;

            if (templates.Count<XElement>() != 1)
                throw new ApplicationException("Missing AWS Beanstalk template for deployment");

            XElement template = templates.First();
            var deploymentTemplate = DeploymentTemplateSelectorPageController.ConvertToDeploymentTemplate(template);
            return deploymentTemplate;
        }
    }
}
