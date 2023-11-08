using Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols;

using FluentAssertions;

using Xunit;

using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using ToolkitPosition = Amazon.AWSToolkit.Models.Text.Position;
using ToolkitRange = Amazon.AWSToolkit.Models.Text.Range;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Protocols
{
    public class ProtocolsExtensionMethodsTests
    {
        private readonly LspPosition _sampleLspPosition = new LspPosition(123, 456);

        private readonly LspRange _sampleLspRange = new LspRange()
        {
            Start = new LspPosition(2, 5),
            End = new LspPosition(4, 100),
        };

        [Fact]
        public void AsLspPosition_WhenNull()
        {
            ToolkitPosition position = null;
            position.AsLspPosition().Should().BeNull();
        }

        [Fact]
        public void AsLspPosition()
        {
            var position = new ToolkitPosition(_sampleLspPosition.Line, _sampleLspPosition.Character);
            position.AsLspPosition().Should().BeEquivalentTo(_sampleLspPosition);
        }

        [Fact]
        public void AsToolkitPosition_WhenNull()
        {
            LspPosition position = null;
            position.AsToolkitPosition().Should().BeNull();
        }

        [Fact]
        public void AsToolkitPosition()
        {
            var expectedPosition = new ToolkitPosition(_sampleLspPosition.Line, _sampleLspPosition.Character);
            _sampleLspPosition.AsToolkitPosition().Should().BeEquivalentTo(expectedPosition);
        }

        [Fact]
        public void AsToolkitRange_WhenNull()
        {
            LspRange range = null;
            range.AsToolkitRange().Should().BeNull();
        }

        [Fact]
        public void AsToolkitRange()
        {
            var expectedRange = new ToolkitRange()
            {
                Start = new ToolkitPosition(_sampleLspRange.Start.Line, _sampleLspRange.Start.Character),
                End = new ToolkitPosition(_sampleLspRange.End.Line, _sampleLspRange.End.Character),
            };

            _sampleLspRange.AsToolkitRange().Should().BeEquivalentTo(expectedRange);
        }
    }
}
