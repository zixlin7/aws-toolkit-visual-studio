using System.Collections.Generic;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class ParserResults
    {
        public ParserResults(IEnumerable<TemplateToken> highlightedTemplateTokens,
            IEnumerable<IntellisenseToken> intellisenseTokens,
            int intellisenseStartingPosition, int intellisenseEndingPosition)
        {
            this._highlightedTemplateTokens = highlightedTemplateTokens;
            this._intellisenseTokens = intellisenseTokens;
            this.IntellisenseStartingPosition = intellisenseStartingPosition;
            this.IntellisenseEndingPosition = intellisenseEndingPosition;
        }

        IEnumerable<TemplateToken> _highlightedTemplateTokens;
        public IEnumerable<TemplateToken> HighlightedTemplateTokens => this._highlightedTemplateTokens;

        IEnumerable<IntellisenseToken> _intellisenseTokens;
        public IEnumerable<IntellisenseToken> IntellisenseTokens => this._intellisenseTokens;

        public int IntellisenseStartingPosition
        {
            get;
        }

        public int IntellisenseEndingPosition
        {
            get;
        }
    }
}
