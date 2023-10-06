using System.Collections.Generic;

using Amazon.AwsToolkit.CodeWhisperer.Documents;
using Amazon.AWSToolkit.Models.Text;

using Microsoft.VisualStudio.Language.Proposals;
using Microsoft.VisualStudio.Text;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Documents
{
    public class FakeCodeWhispererTextView : ICodeWhispererTextView
    {
        public string FilePath;

        public string GetFilePath()
        {
            return FilePath;
        }

        public Position GetDocumentPosition(int position)
        {
            return new Position(0, position);
        }

        public Proposal CreateProposal(string replacementText, string description)
        {
            var edit = new ProposedEdit(new SnapshotSpan(), replacementText, null);
            var caretPosition = new VirtualSnapshotPoint(new SnapshotPoint());

            return new Proposal(description, new List<ProposedEdit>() { edit }, caretPosition);
        }
    }
}
