using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.Tests.Common;
using Amazon.AWSToolkit.Tests.Common.Account;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.ElasticBeanstalk.Model;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class StartPageControllerTests : BeanstalkWizardControllerTests
    {
        public class TestableStartPageController : StartPageController
        {
            public TestableStartPageController(ToolkitContext toolkitContext, StartPage startPage) : base(toolkitContext)
            {
                _pageUI = startPage;
            }

            protected override IAWSEC2 GetEc2PluginService() => null;

            public bool RedeploySelected { get; set; }

            protected override bool IsRedeploy() => RedeploySelected;

            public DeployedApplicationModel SelectedApplication { get; set; }

            protected override DeployedApplicationModel GetSelectedDeployment() => SelectedApplication;

            protected override List<DeploymentTemplateWrapperBase> GetTemplatesForSelectedRegion() => new List<DeploymentTemplateWrapperBase>();
        }

        static StartPageControllerTests()
        {
            ToolkitTheme.SetupBaseTheme();
        }

        private readonly ToolkitContext _toolkitContext = new ToolkitContextFixture().ToolkitContext;

        private readonly TestableStartPageController _startPageController;

        public StartPageControllerTests()
        {
            var startPage = CreateStartPage();
            _startPageController = CreateStartPageController(startPage);
        }

        private StartPage CreateStartPage()
        {
            return new StartPage(_toolkitContext, Wizard)
            {
                SelectedAccount = AccountFixture.CreateSharedCredentialAccount(),
            };
        }

        private TestableStartPageController CreateStartPageController(StartPage startPage)
        {
            return new TestableStartPageController(_toolkitContext, startPage)
            {
                HostingWizard = Wizard,
            };
        }

        [StaFact]
        public void ShouldUseNetCoreDeployment()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.NetCoreWebProject);

            // act.
            TransferStateFromPageToWizard(_startPageController);

            // assert.
            Wizard.AssertUsingEbTools();
        }

        [StaFact]
        public void ShouldUseStandardDeployment()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.StandardWebProject);

            // act.
            TransferStateFromPageToWizard(_startPageController);

            // assert.
            Wizard.AssertNotUsingEbTools();
        }

        [StaFact]
        public void ShouldUseNetCoreDeploymentForLinuxRedeployment()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.NetCoreWebProject);

            _startPageController.RedeploySelected = true;
            _startPageController.SelectedApplication = CreateDeployedApplication(CreateLinuxEnvironmentDescription());

            // act.
            TransferStateFromPageToWizard(_startPageController);

            // assert.
            Wizard.AssertPlatformIsLinux();
            Wizard.AssertUsingEbTools();
        }

        private DeployedApplicationModel CreateDeployedApplication(EnvironmentDescription environment)
        {
            var deployedApplication = new DeployedApplicationModel("sample-app");

            deployedApplication.Environments.Add(environment);

            return deployedApplication;
        }

        private EnvironmentDescription CreateLinuxEnvironmentDescription()
        {
            return new EnvironmentDescription()
            {
                SolutionStackName = "64bit Amazon Linux 2"
            };
        }

        [StaFact]
        public void ShouldUseNetCoreDeploymentForNetCoreWindowsRedeployment()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.NetCoreWebProject);

            _startPageController.RedeploySelected = true;
            _startPageController.SelectedApplication = CreateDeployedApplication(CreateWindowsEnvironmentDescription());

            // act.
            TransferStateFromPageToWizard(_startPageController);

            // assert.
            Wizard.AssertPlatformIsWindows();
            Wizard.AssertUsingEbTools();
        }

        private EnvironmentDescription CreateWindowsEnvironmentDescription()
        {
            return new EnvironmentDescription()
            {
                SolutionStackName = "64bit Windows Server"
            };
        }

        [StaFact]
        public void ShouldUseStandardDeploymentForStandardWindowsRedeployment()
        {
            // arrange.
            SetProjectTypeTo(DeploymentWizardProperties.StandardWebProject);

            _startPageController.RedeploySelected = true;
            _startPageController.SelectedApplication = CreateDeployedApplication(CreateWindowsEnvironmentDescription());

            // act.
            TransferStateFromPageToWizard(_startPageController);

            // assert.
            Wizard.AssertPlatformIsWindows();
            Wizard.AssertNotUsingEbTools();
        }
    }
}
