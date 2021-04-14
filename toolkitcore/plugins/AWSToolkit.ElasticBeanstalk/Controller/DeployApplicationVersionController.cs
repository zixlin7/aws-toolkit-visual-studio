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
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.ElasticBeanstalk.Commands;
using Amazon.AWSToolkit.ElasticBeanstalk.Nodes;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.ElasticBeanstalk;
using Amazon.AWSToolkit.PluginServices.Deployment;


namespace Amazon.AWSToolkit.ElasticBeanstalk.Controller
{
    public class DeployApplicationVersionController
    {
        private readonly ToolkitContext _toolkitContext;
        IAmazonElasticBeanstalk _beanstalkClient;

        public DeployApplicationVersionController(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

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
                {DeploymentWizardProperties.SeedData.propkey_SeedName, applicationViewModel.Application.ApplicationName},
                {DeploymentWizardProperties.AppOptions.propkey_TargetRuntime, "4.0"},
                {BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel, versionLabel}
            };

            // needed so pages know they are running in a Beanstalk-specific environment
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] = DeploymentServiceIdentifiers.BeanstalkServiceName;

            var template = LoadDeploymentTemplate(applicationViewModel.ElasticBeanstalkRootViewModel.Region.Id);
            seedProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_SelectedTemplate] = template;

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.ElasticBeanstalk.View.PublishApplicationVersion", seedProperties);
            wizard.Title = "Publish Application Version";
            wizard.SetSelectedRegion(applicationViewModel.ElasticBeanstalkRootViewModel.Region);

            // register the page groups we expect child pages to fit into
            wizard.RegisterPageGroups(DeploymentWizardPageGroups.DeploymentPageGroups);

            var defaultPages = new IAWSWizardPageController[]
            {
                new StartPageController(_toolkitContext),
                new ApplicationPageController(),
                new AWSOptionsPageController(),
                new VpcOptionsPageController(),
                new ConfigureRollingDeploymentsController(),
                new ApplicationOptionsPageController(),
                new PseudoReviewPageController(),
                new CommonUI.LegacyDeploymentWizard.PageControllers.DeploymentReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);
            if (wizard.Run() == true)
            {
                ThreadPool.QueueUserWorkItem(x =>
                    {
                        var command = new DeployNewApplicationCommand("FakePackage", wizard.CollectedProperties, _toolkitContext);
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
