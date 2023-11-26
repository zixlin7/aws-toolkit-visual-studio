using LspPosition = Microsoft.VisualStudio.LanguageServer.Protocol.Position;
using LspRange = Microsoft.VisualStudio.LanguageServer.Protocol.Range;
using ToolkitPosition = Amazon.AWSToolkit.Models.Text.Position;
using ToolkitRange = Amazon.AWSToolkit.Models.Text.Range;

namespace Amazon.AwsToolkit.CodeWhisperer.Lsp.Protocols
{
    public static class ProtocolsExtensionMethods
    {
        public static LspPosition AsLspPosition(this ToolkitPosition position)
        {
            return position == null ? null : new LspPosition(position.Line, position.Column);
        }

        public static ToolkitPosition AsToolkitPosition(this LspPosition position)
        {
            return position == null ? null : new ToolkitPosition(position.Line, position.Character);
        }

        public static ToolkitRange AsToolkitRange(this LspRange range)
        {
            return range == null
                ? null
                : new ToolkitRange()
                {
                    Start = range.Start.AsToolkitPosition(),
                    End = range.End.AsToolkitPosition(),
                };
        }
    }
}
