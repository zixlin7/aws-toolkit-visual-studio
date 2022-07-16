
using Amazon.AWSToolkit.Telemetry;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests.Telemetry
{
    public class ClientIdTests
    {
        private const string AutomatedTestClientId = "ffffffff-ffff-ffff-ffff-ffffffffffff";

        private const string TelemetryOptOutClientId = "11111111-1111-1111-1111-111111111111";

        private const string UnknownClientId = "00000000-0000-0000-0000-000000000000";

        [Fact]
        public void ClientIdIsAllFsWhenRunningTests()
        {
            Assert.Equal(AutomatedTestClientId, ClientId.Instance);
        }

        [Fact]
        public void EqualityWorks()
        {
            Assert.Equal(AutomatedTestClientId, ClientId.AutomatedTestClientId);
            Assert.Equal(TelemetryOptOutClientId, ClientId.TelemetryOptOutClientId);
            Assert.Equal(UnknownClientId, ClientId.UnknownClientId);
            Assert.Equal(ClientId.Instance, ClientId.Instance);

            Assert.NotEqual(ClientId.UnknownClientId, ClientId.AutomatedTestClientId);
            Assert.NotEqual(UnknownClientId, ClientId.AutomatedTestClientId);
            Assert.NotEqual(UnknownClientId, ClientId.AutomatedTestClientId);
            Assert.NotEqual(new object(), ClientId.AutomatedTestClientId);
        }

        [Fact]
        public void ToStringWorks()
        {
            Assert.Equal(AutomatedTestClientId, ClientId.Instance.ToString());
        }

        [Fact]
        public void ImplicitConversionToStringWorks()
        {
            Assert.Equal(AutomatedTestClientId, ClientId.Instance);
            Assert.Equal(ClientId.Instance, AutomatedTestClientId);
            Assert.True(AutomatedTestClientId == ClientId.Instance);
            Assert.True(ClientId.Instance == AutomatedTestClientId);
        }
    }
}
