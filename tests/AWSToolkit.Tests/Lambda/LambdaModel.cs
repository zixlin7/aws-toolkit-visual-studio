using Microsoft.VisualStudio.TestTools.UnitTesting;
using Amazon.AWSToolkit.Lambda.Model;
using Moq;
using Amazon.Lambda.Model;

namespace AWSTooklit.Tests.Lambda
{
    [TestClass]
    public class LambdaModel
    {
        public LambdaModel()
        { }

        [TestMethod, TestCategory("Unit")]
        public void RuntimeOptionsIsNodeOrIsNetCore()
        {
            Assert.IsTrue(RuntimeOption.NetCore_v2_1.IsNetCore);
            Assert.IsFalse(RuntimeOption.NetCore_v2_1.IsNode);
            Assert.IsTrue(RuntimeOption.NodeJS_v10_X.IsNode);
            Assert.IsFalse(RuntimeOption.NodeJS_v10_X.IsNetCore);
        }

        [TestMethod, TestCategory("Unit")]
        public void RuntimeClass()
        {
            var x = RuntimeOption.NetCore_v2_1;
            Assert.AreEqual(".NET Core v2.1", x.DisplayName);
            Assert.AreEqual("dotnetcore2.1", x.Value);
            Assert.IsTrue(x.GetType().GetConstructors().Length == 0, "Public constructor, should not be able to create a runtime option!");
        }

        [TestMethod, TestCategory("Unit")]
        public void EventSourceWrapperWraps()
        {
            var mockEvent = new Mock<EventSourceMappingConfiguration>();
            mockEvent.Object.EventSourceArn = "a:b:service";
            var wrapper = new EventSourceWrapper(mockEvent.Object);
            Assert.IsNull(wrapper.ServiceIcon);
            Assert.AreEqual("service", wrapper.ServiceName);
            Assert.AreEqual("service", wrapper.ResourceDisplayName);
        }
    }
}