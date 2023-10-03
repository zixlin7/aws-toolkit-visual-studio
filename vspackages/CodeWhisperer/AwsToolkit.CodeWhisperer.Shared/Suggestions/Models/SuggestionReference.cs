namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    public class SuggestionReference
    {
        /// <summary>
        /// The name of the reference (like a repo name)
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Url to the suggested code's source
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Type of license attributed to the suggestion
        /// </summary>
        public string LicenseName { get; set; }

        /// <summary>
        /// Starting location of attributed code within the suggested text
        /// </summary>
        public int StartIndex { get; set; }

        /// <summary>
        /// Ending location of attributed code within the suggested text
        /// </summary>
        public int EndIndex { get; set; }
    }
}
