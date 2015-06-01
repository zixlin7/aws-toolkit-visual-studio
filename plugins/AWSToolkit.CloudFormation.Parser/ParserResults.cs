using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        public IEnumerable<TemplateToken> HighlightedTemplateTokens
        {
            get { return this._highlightedTemplateTokens; }
        }

        IEnumerable<IntellisenseToken> _intellisenseTokens;
        public IEnumerable<IntellisenseToken> IntellisenseTokens
        {
            get { return this._intellisenseTokens; }
        }

        public TemplateToken RootTemplateToken
        {
            get;
            private set;
        }

        IEnumerable<ErrorToken> _errorTokens;
        public IEnumerable<ErrorToken> ErrorTokens
        {
            get { return this._errorTokens; }
        }

        public int IntellisenseStartingPosition
        {
            get;
            private set;
        }

        public int IntellisenseEndingPosition
        {
            get;
            private set;
        }
    }
}
