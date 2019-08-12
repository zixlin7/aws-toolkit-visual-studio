namespace Amazon.AWSToolkit.CloudFormation.Parser
{
    public enum ErrorTokenType { JSONLintvalidation, SchemaValidation };

    public class ErrorToken
    {
        public ErrorToken(ErrorTokenType type, string message, bool showToolTip, int startPos, int endPos)
        {
            this.Type = type;
            this.Message = message;
            this.ShowToolTip = showToolTip;

            this.StartPos = startPos;
            this.EndPos = endPos;
            this.LineNumber = -1;
        }

        public ErrorToken(ErrorTokenType type, string message, bool showToolTip, int lineNumber, int lineStartOffset, int lineEndOffSet)
        {
            this.Type = type;
            this.Message = message;
            this.ShowToolTip = showToolTip;

            this.LineNumber = lineNumber;
            this.LineStartOffset = lineStartOffset;
            this.LineEndOffSet = lineEndOffSet;
        }

        public bool ShowToolTip
        {
            get;
        }

        public ErrorTokenType Type
        {
            get;
        }

        public int LineNumber
        {
            get;
        }

        public int LineStartOffset
        {
            get;
        }

        public int LineEndOffSet
        {
            get;
        }

        public int StartPos
        {
            get;
        }

        public int EndPos
        {
            get;
        }

        public string Message
        {
            get;
        }
    }
}
