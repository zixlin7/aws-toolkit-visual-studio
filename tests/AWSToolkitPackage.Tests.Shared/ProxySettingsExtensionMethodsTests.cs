using System.Collections.Generic;

using Amazon.AWSToolkit.Settings;

using Xunit;

namespace AWSToolkitPackage.Tests
{
    public class ProxySettingsExtensionMethodsTests
    {
        public static IEnumerable<object[]> ProxyUrlData =>
            new List<object[]>
            {
                new object[] { CreateProxySetting("127.0.0.1", null, null, null), null },
                new object[] { CreateProxySetting("127.0.0.1", null, "abc", "xyz"), null },
                new object[] { CreateProxySetting(null, 1234, null, null), null },
                new object[] { CreateProxySetting(null, 1234, "abc", "xyz"), null },
                new object[] { CreateProxySetting("127.0.0.1", 1234, null, null), "http://127.0.0.1:1234" },
                new object[] { CreateProxySetting("http://127.0.0.1", 1234, null, null), "http://127.0.0.1:1234" },
                new object[] { CreateProxySetting("http://www.proxyexample.com/defaul.apx", 1234, null, null), "http://www.proxyexample.com:1234" },
                new object[] { CreateProxySetting("https://127.0.0.1", 1234, null, null), "https://127.0.0.1:1234" },
                new object[] { CreateProxySetting("127.0.0.1", 1234, "abc", null), "http://127.0.0.1:1234" },
                new object[] { CreateProxySetting("127.0.0.1", 1234, null, "xyz"), "http://127.0.0.1:1234" },
                new object[] { CreateProxySetting("127.0.0.1", 1234, "abc", "xyz"), "http://abc:xyz@127.0.0.1:1234" },
                new object[]
                {
                    CreateProxySetting("https://127.0.0.1", 1234, "abc", "xyz"), "https://abc:xyz@127.0.0.1:1234"
                },
                new object[]
                {
                    CreateProxySetting("http://127.0.0.1", 1234, "abc", "xyz"), "http://abc:xyz@127.0.0.1:1234"
                },
            };

        private static ProxySettings CreateProxySetting(string host, int? port, string username, string password)
        {
            return new ProxySettings { Host = host, Port = port, Username = username, Password = password };
        }

        [Fact]
        public void GetProxyUrl_WhenNull()
        {
            ProxySettings settings = null;
            var url = settings.GetProxyUrl();
            Assert.Null(url);
        }


        [Theory]
        [MemberData(nameof(ProxyUrlData))]
        public void GetProxyUrl(ProxySettings proxySettings, string expectedUrl)
        {
            var settings = proxySettings;
            var url = settings.GetProxyUrl();

            Assert.Equal(expectedUrl, url);
        }
    }
}
