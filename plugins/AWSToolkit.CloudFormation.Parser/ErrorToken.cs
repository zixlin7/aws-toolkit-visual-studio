using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            private set;
        }

        public ErrorTokenType Type
        {
            get;
            private set;
        }

        public int LineNumber
        {
            get;
            private set;
        }

        public int LineStartOffset
        {
            get;
            private set;
        }

        public int LineEndOffSet
        {
            get;
            private set;
        }

        public int StartPos
        {
            get;
            private set;
        }

        public int EndPos
        {
            get;
            private set;
        }

        public string Message
        {
            get;
            private set;
        }
    }
}
