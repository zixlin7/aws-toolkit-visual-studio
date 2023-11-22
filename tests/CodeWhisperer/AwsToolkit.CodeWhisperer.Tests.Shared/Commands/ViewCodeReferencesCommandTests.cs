using System.Threading.Tasks;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AWSToolkit.Tests.Common.Context;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Commands
{
    public class ViewCodeReferencesCommandTests
    {
        private readonly FakeCodeWhispererManager _manager = new FakeCodeWhispererManager();
        private readonly ToolkitContextFixture _toolkitContextFixture = new ToolkitContextFixture();
        private readonly ViewCodeReferencesCommand _sut;

        public ViewCodeReferencesCommandTests()
        {
            _sut = new ViewCodeReferencesCommand(_manager, _toolkitContextFixture.ToolkitContextProvider);
        }

        [Fact]
        public async Task ExecuteAsync()
        {
            await _sut.ExecuteAsync();
            _manager.DidShowReferenceLogger.Should().BeTrue();
        }
    }
}
