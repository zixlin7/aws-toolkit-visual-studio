namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    /// <summary>
    /// Request model for the CodeWhisperer integration to obtain code suggestions
    /// </summary>
    public class GetSuggestionsRequest
    {
        /// <summary>
        /// Location of code file to request suggestions for
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// 0-indexed line number of the cursor position
        /// </summary>
        public int CursorLine { get; set; }

        /// <summary>
        /// 0-indexed column number of the cursor position
        /// </summary>
        public int CursorColumn { get; set; }

        /// <summary>
        /// Whether or not this request was auto-invoked
        /// </summary>
        public bool IsAutoSuggestion { get; set; }
    }
}
