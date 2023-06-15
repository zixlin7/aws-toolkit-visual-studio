using System.Collections.Generic;

using Amazon.AWSToolkit.Lambda.Model;
using Amazon.Lambda.Model;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class LambdaModel
    {
        public static readonly IEnumerable<object[]> DotNetManagedRuntimes = new[]
        {
            new object[] { RuntimeOption.DotNet6 },
        };

        [Theory]
        [MemberData(nameof(DotNetManagedRuntimes))]
        public void RuntimeOptionsAreNetCore(RuntimeOption runtimeOption)
        {
            Assert.True(runtimeOption.IsDotNet);
            Assert.False(runtimeOption.IsNode);
            Assert.False(runtimeOption.IsCustomRuntime);
        }

        [Fact]
        public void RuntimeOptionsAreNode()
        {
            Assert.True(RuntimeOption.NodeJS_v12_X.IsNode);
            Assert.False(RuntimeOption.NodeJS_v12_X.IsDotNet);
            Assert.False(RuntimeOption.NodeJS_v12_X.IsCustomRuntime);
        }

        [Fact]
        public void RuntimeClass()
        {
            var runtime = RuntimeOption.DotNet6;
            Assert.Equal(".NET 6", runtime.DisplayName);
            Assert.Equal("dotnet6", runtime.Value);
        }

        [Fact]
        public void EventSourceWrapperWraps()
        {
            var mockEvent = new Mock<EventSourceMappingConfiguration>();
            mockEvent.Object.EventSourceArn = "a:b:service";
            var wrapper = new EventSourceWrapper(mockEvent.Object);
            Assert.Null(wrapper.ServiceIcon);
            Assert.Equal("service", wrapper.ServiceName);
            Assert.Equal("service", wrapper.ResourceDisplayName);
        }
    }
}
