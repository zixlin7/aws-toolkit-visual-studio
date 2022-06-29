using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.ECS.Controller;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.ECS;
using Amazon.ECS.Model;

using Moq;

using Xunit;

using Task = Amazon.ECS.Model.Task;

namespace AWSToolkit.Tests.ECS
{
    public class ViewClusterControllerTests
    {
        private readonly ViewClusterController _controller;
        private readonly ToolkitContextFixture _contextFixture = new ToolkitContextFixture();
        private readonly Mock<IAmazonECS> _ecsClient = new Mock<IAmazonECS>();
        private readonly Task _sampleTask = new Task() { TaskDefinitionArn = "td-arn", TaskArn = "arn/task1" };

        private static readonly string _sampleContainer = "sample-container";

        private readonly Dictionary<string, string> _awsOptions =
            new Dictionary<string, string>() { { "awslogs-group", "sample-lg" }, { "awslogs-stream-prefix", "ecs" } };

        public ViewClusterControllerTests()
        {
            _controller = new ViewClusterController(_contextFixture.ToolkitContext);
        }

        public static IEnumerable<object[]> EmptyContainersData = new List<object[]>
        {
            new object[] { null }, new object[] { new List<ContainerDefinition>() },
        };

        [Theory]
        [MemberData(nameof(EmptyContainersData))]
        public void GetContainerLogs_NoContainers(List<ContainerDefinition> containers)
        {
            StubDescribeTaskDefinitionToReturn(containers);

            var containerToLogs = _controller.GetContainersToLogsDetails(_ecsClient.Object, _sampleTask);

            Assert.Empty(containerToLogs);
        }

        public static IEnumerable<object[]> IncompatibleContainersData = new List<object[]>
        {
            new object[] { new List<ContainerDefinition> { CreateContainerDefinition(LogDriver.Awsfirelens) } },
            new object[]
            {
                new List<ContainerDefinition>
                {
                    CreateContainerDefinition(LogDriver.Fluentd),
                    CreateContainerDefinition(LogDriver.Splunk)
                }
            },
        };


        [Theory]
        [MemberData(nameof(IncompatibleContainersData))]
        public void GetContainerLogs_IncompatibleContainers(List<ContainerDefinition> containers)
        {
            StubDescribeTaskDefinitionToReturn(containers);

            var containerToLogs = _controller.GetContainersToLogsDetails(_ecsClient.Object, _sampleTask);

            Assert.Empty(containerToLogs);
        }

        [Fact]
        public void GetContainerLogs_NoLogsPrefix()
        {
            var options = new Dictionary<string, string>() { { "awslogs-group", "sample-lg" } };
            var containers = new List<ContainerDefinition> { CreateContainerDefinition(LogDriver.Awslogs, options) };
            StubDescribeTaskDefinitionToReturn(containers);

            var containerToLogs = _controller.GetContainersToLogsDetails(_ecsClient.Object, _sampleTask);

            Assert.Empty(containerToLogs);
        }

        [Fact]
        public void GetContainerLogs()
        {
            var containers =
                new List<ContainerDefinition> { CreateContainerDefinition(LogDriver.Awslogs, _awsOptions) };
            StubDescribeTaskDefinitionToReturn(containers);

            var containerToLogs = _controller.GetContainersToLogsDetails(_ecsClient.Object, _sampleTask);

            Assert.Single(containerToLogs);
            Assert.Equal(_sampleContainer, containerToLogs.First().Key);
            Assert.Equal(_awsOptions["awslogs-group"], containerToLogs.First().Value.LogGroup);
            Assert.Equal(GetLogStream(_awsOptions["awslogs-stream-prefix"]), containerToLogs.First().Value.LogStream);
        }

        [Theory]
        [InlineData("ecs", "abcd/task1")]
        [InlineData("hello", "aeeeeeebcd/task1")]
        [InlineData("xyz", "task1")]
        [InlineData("bye", "arn/task1")]
        [InlineData("yes", "arn::abc:great/task1")]
        [InlineData("no", "arn::abc/no:great/task1")]
        public void CreateLogStream(string prefix, string taskArn)
        {
            _sampleTask.TaskArn = taskArn;
            var logStream = _controller.CreateLogStream(_sampleTask, prefix, _sampleContainer);

            var expectedLogStream = GetLogStream(prefix);
            Assert.Equal(expectedLogStream, logStream);
        }

        private void StubDescribeTaskDefinitionToReturn(List<ContainerDefinition> containers)
        {
            var taskDefinition = new TaskDefinition() { ContainerDefinitions = containers };
            var response = new DescribeTaskDefinitionResponse() { TaskDefinition = taskDefinition };
            _ecsClient.Setup(mock => mock.DescribeTaskDefinition(It.IsAny<DescribeTaskDefinitionRequest>()))
                .Returns(response);
        }

        private static ContainerDefinition CreateContainerDefinition(LogDriver logDriver,
            Dictionary<string, string> options = null)
        {
            return new ContainerDefinition
            {
                Name = _sampleContainer,
                LogConfiguration = new LogConfiguration { LogDriver = logDriver, Options = options }
            };
        }

        private string GetLogStream(string prefix)
        {
            return $"{prefix}/{_sampleContainer}/task1";
        }
    }
}
