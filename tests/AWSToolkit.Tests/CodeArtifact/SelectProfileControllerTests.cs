using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.CodeArtifact.View;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Shared;
using Moq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
{
    public class SelectProfileControllerTests
    {
        private readonly TelemetryFixture _telemetryFixture = new TelemetryFixture();

        [StaFact]
        public void NoRegisteredAccount()
        {
            List<AccountViewModel> registeredAccounts = new List<AccountViewModel>();
            var mockShell = new Mock<IAWSToolkitShellProvider>();
            var selectProfileController = new SelectProfileController(registeredAccounts, mockShell.Object, _telemetryFixture.TelemetryLogger.Object);
            var result = selectProfileController.Execute(null);
            Assert.False(result.Success);
            AssertFailureMetric();
        }

        [Fact]
        public void SelectedAccountNull()
        {
            List<AccountViewModel> registeredAccounts = new List<AccountViewModel>();
            registeredAccounts.Add(It.IsAny<AccountViewModel>());
            var mockShell = new Mock<IAWSToolkitShellProvider>();

            var selectProfileController = new SelectProfileController(registeredAccounts, mockShell.Object, _telemetryFixture.TelemetryLogger.Object);
            var result = selectProfileController.Persist(null);
            Assert.False(result.Success);
            AssertFailureMetric();
        }

        private void AssertFailureMetric()
        {
            _telemetryFixture.AssertTelemetryRecordCalls(1);
            _telemetryFixture.AssertCodeArtifactMetrics(_telemetryFixture.LoggedMetrics.Single(),
                Result.Failed, "codeartifact_setRepoCredentialProfile", "nuget");
        }
    }
}
