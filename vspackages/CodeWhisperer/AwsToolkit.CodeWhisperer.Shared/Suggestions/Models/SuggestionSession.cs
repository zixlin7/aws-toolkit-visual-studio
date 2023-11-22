using System.Collections.Generic;
using System.Linq;

namespace Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models
{
    public class SuggestionSession
    {
        public string SessionId { get; set; }

        public IList<Suggestion> Suggestions { get; } = new List<Suggestion>();

        /// <summary>
        /// The epoch time(in milliseconds) when the inline completions request started
        /// </summary>
        public long RequestedAtEpoch { get; set; }

        /// <summary>
        /// Checks if session is valid i.e. session id is not empty and  list of suggestions is not empty
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(SessionId) && Suggestions.Any();
        }
    }
}
