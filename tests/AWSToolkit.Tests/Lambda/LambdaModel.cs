using Amazon.AWSToolkit.Lambda.Model;
using Amazon.Lambda.Model;
using Moq;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class LambdaModel
    {
        public LambdaModel()
        { }

        [Fact]
        public void RuntimeOptionsIsNodeOrIsNetCore()
        {
            Assert.True(RuntimeOption.NetCore_v2_1.IsNetCore);
            Assert.False(RuntimeOption.NetCore_v2_1.IsNode);
            Assert.True(RuntimeOption.NodeJS_v10_X.IsNode);
            Assert.False(RuntimeOption.NodeJS_v10_X.IsNetCore);
        }

        [Fact]
        public void RuntimeClass()
        {
            var x = RuntimeOption.NetCore_v2_1;
            Assert.Equal(".NET Core v2.1", x.DisplayName);
            Assert.Equal("dotnetcore2.1", x.Value);
            Assert.True(x.GetType().GetConstructors().Length == 0, "Public constructor, should not be able to create a runtime option!");
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