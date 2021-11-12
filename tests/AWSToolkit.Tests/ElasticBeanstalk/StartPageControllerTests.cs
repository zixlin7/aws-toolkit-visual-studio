using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.Model;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment;
using Amazon.AWSToolkit.Tests.Common;
using Amazon.AWSToolkit.Tests.Common.Account;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.Wizard;
using Amazon.ElasticBeanstalk.Model;

using AWSToolkit.Tests.ElasticBeanstalk.Wizard;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class StartPageControllerTests
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
        }

        static StartPageControllerTests()
        {
            ToolkitTheme.SetupBaseTheme();
        }

        private readonly ToolkitContext _toolkitContext = new ToolkitContextFixture().ToolkitContext;
        private readonly IAWSWizard _wizard = new InMemoryAWSWizard();
        private readonly StartPage _startPage;

        public StartPageControllerTests()
        {
            _startPage = CreateStartPage();
        }

        private StartPage CreateStartPage()
        {
            return new StartPage(_toolkitContext, _wizard)
            {
                SelectedAccount = AccountFixture.CreateSharedCredentialAccount(),
            };
        }

        [StaFact]
        public void ShouldUseNetCoreDeploymentForLinux()
        {
            // arrange.
            var startPageController = new TestableStartPageController(_toolkitContext, _startPage)
            {
                HostingWizard = _wizard,
                RedeploySelected = true,
                SelectedApplication = CreateDeployedApplication(CreateLinuxEnvironmentDescription())
            };

            // act.
            TransferStateFromPageToWizard(startPageController);

            // assert.
            _wizard.AssertPlatformIsLinux();
            _wizard.AssertUsingEbTools();
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

        private void TransferStateFromPageToWizard(IAWSWizardPageController controller)
        {
            controller.PageDeactivating(AWSWizardConstants.NavigationReason.finishPressed);
        }

        [StaFact]
        public void ShouldUseStandardDeploymentForWindows()
        {
            // arrange.
            var startPageController = new TestableStartPageController(_toolkitContext, _startPage)
            {
                HostingWizard = _wizard,
                RedeploySelected = true,
                SelectedApplication = CreateDeployedApplication(CreateWindowsEnvironmentDescription())
            };

            // act.
            TransferStateFromPageToWizard(startPageController);

            // assert.
            _wizard.AssertPlatformIsWindows();
            _wizard.AssertNotUsingEbTools();
        }

        private EnvironmentDescription CreateWindowsEnvironmentDescription()
        {
            return new EnvironmentDescription()
            {
                SolutionStackName = "64bit Windows Server"
            };
        }
    }
}
