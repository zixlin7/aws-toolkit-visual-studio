using Amazon.AWSToolkit.Models.Text;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    public class Suggestion
    {
        /// <summary>
        /// The text that would be placed in the document if accepted
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Optional - indicates what text should be replaced by <see cref="Text"/>
        /// </summary>
        public Range ReplacementRange { get; set; }
    }
}
