using Amazon.AWSToolkit.Models.Text;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    /// <summary>
    /// Provides instruction to <see cref="ReferenceLogger"/> about logging references
    /// </summary>
    public class LogReferenceRequest
    {
        /// <summary>
        /// The suggestion being accepted
        /// </summary>
        public Suggestion Suggestion { get; set; }

        /// <summary>
        /// The reference being logged
        /// </summary>
        public SuggestionReference SuggestionReference { get; set; }

        /// <summary>
        /// The file where the suggestion is being accepted
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The position within the file where this reference (license attribution) is located
        /// </summary>
        public Position Position { get; set; }
    }
}
