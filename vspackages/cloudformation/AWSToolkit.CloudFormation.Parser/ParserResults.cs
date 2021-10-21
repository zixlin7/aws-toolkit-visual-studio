using System.Collections.Generic;

namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public class ParserResults
    {
        public ParserResults(TemplateToken rootTemplateToken,
            IEnumerable<TemplateToken> highlightedTemplateTokens,
            IEnumerable<IntellisenseToken> intellisenseTokens,
            int intellisenseStartingPosition, int intellisenseEndingPosition,
            IEnumerable<ErrorToken> errorTokens)
        {
            this.RootTemplateToken = rootTemplateToken;
            this._highlightedTemplateTokens = highlightedTemplateTokens;
            this._intellisenseTokens = intellisenseTokens;
            this.IntellisenseStartingPosition = intellisenseStartingPosition;
            this.IntellisenseEndingPosition = intellisenseEndingPosition;
            this._errorTokens = errorTokens;
        }

        IEnumerable<TemplateToken> _highlightedTemplateTokens;
        public IEnumerable<TemplateToken> HighlightedTemplateTokens => this._highlightedTemplateTokens;

        IEnumerable<IntellisenseToken> _intellisenseTokens;
        public IEnumerable<IntellisenseToken> IntellisenseTokens => this._intellisenseTokens;

        public TemplateToken RootTemplateToken
        {
            get;
        }

        IEnumerable<ErrorToken> _errorTokens;
        public IEnumerable<ErrorToken> ErrorTokens => this._errorTokens;

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
