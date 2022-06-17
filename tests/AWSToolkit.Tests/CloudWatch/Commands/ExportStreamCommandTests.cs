using System.Collections.Generic;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Commands;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Tests.Common.Context;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.CloudWatch.Commands
{
    public class ExportStreamCommandTests
    {
        private readonly Mock<ICloudWatchLogsRepository> _repository = new Mock<ICloudWatchLogsRepository>();
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();

        private readonly ICommand _command;

        public ExportStreamCommandTests()
        {
            var awsConnectionSettings = new AwsConnectionSettings(null, null);
            _repository.SetupGet(m => m.ConnectionSettings).Returns(awsConnectionSettings);

            _command = ExportStreamCommand.Create(_repository.Object, _contextFixture.ToolkitContext);
        }

        public static IEnumerable<object[]> InvalidParameterTypes = new List<object[]>
        {
            new object[] { new object[] { 1, "hello" } },
            new object[] { new object[] { false, 1 } },
            new object[] { new object[] { 2, null } },
        };


        [Theory]
        [MemberData(nameof(InvalidParameterTypes))]
        public void Execute_InvalidParameterTypes(object parameter)
        {
            _command.Execute(parameter);

            _contextFixture.ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Parameters are not of expected type"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsDownload(Result.Failed);
        }

        public static IEnumerable<object[]> InvalidParameters = new List<object[]>
        {
            new object[] { new object[] { null } },
            new object[] { new object[] { "hello" } },
            new object[] { new object[] { "great", 1, "bad" } },
        };

        [Theory]
        [MemberData(nameof(InvalidParameters))]
        public void Execute_InvalidParameters(object parameter)
        {
            _command.Execute(parameter);

            _contextFixture.ToolkitHost.Verify(
                mock => mock.ShowError(It.Is<string>(msg => msg.Contains("Expected parameters: 2"))),
                Times.Once);
            _contextFixture.TelemetryFixture.VerifyRecordCloudWatchLogsDownload(Result.Failed);
        }
    }
}
