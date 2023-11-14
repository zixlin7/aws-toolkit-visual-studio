using System;

using Amazon.AWSToolkit.Lsp;

using Xunit;

namespace AWSToolkit.Tests.Lsp
{
    public class JsonRpcProxyTests
    {
        private interface ISampleProxy
        {
            [JsonRpcMessageMapping("fooBar")]
            void Foo(string bar);

            void MethodWithoutAttribute();
        }

        [Theory]
        [InlineData("Foo", "fooBar")]
        [InlineData("MethodWithoutAttribute", null)]
        [InlineData("NonExistentMethod", null)]
        public void GetMessageName(string functionName, string expectedMethodName)
        {
            Assert.Equal(expectedMethodName, JsonRpcProxy.GetMessageName<ISampleProxy>(functionName));
        }

        [Theory]
        [InlineData("Foo", "zzz", "fooBar")]
        [InlineData("MethodWithoutAttribute", "zzz", "zzz")]
        [InlineData("NonExistentMethod", "zzz", "zzz")]
        public void GetMessageNameOrDefault(string functionName, string defaultValue, string expectedMethodName)
        {
            Assert.Equal(expectedMethodName, JsonRpcProxy.GetMessageNameOrDefault<ISampleProxy>(functionName, defaultValue));
        }

        [Theory]
        [InlineData("Foo", "fooBar")]
        [InlineData("MethodWithoutAttribute", "MethodWithoutAttribute")]
        [InlineData("NonExistentMethod", "NonExistentMethod")]
        public void MethodNameTransform(string functionName, string expectedMethodName)
        {
            Func<string, string> transform = JsonRpcProxy.MethodNameTransform<ISampleProxy>;
            Assert.Equal(expectedMethodName, transform(functionName));
        }
    }
}
