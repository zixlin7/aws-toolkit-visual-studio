using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Tests.Documents;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class SecurityScanCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly FakeCodeWhispererTextView _textView = new FakeCodeWhispererTextView();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly SecurityScanCommand _sut;

        public SecurityScanCommandTests()
        {
            _sut = new SecurityScanCommand(_manager, _textView, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            await _sut.ExecuteAsync();
            _manager.DidRunSecurityScan.Should().BeTrue();
        }
    }
}
