using System.Linq;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public static class SampleSuggestionProducer
    {
        public static Suggestion CreateSampleSuggestion(string text)
        {
            return new Suggestion { Text = text };
        }

        public static Suggestion CreateSampleSuggestion(string id, int referenceCount)
        {
            var suggestion = new Suggestion() { Text = $"suggestion-{id}", };

            suggestion.References = Enumerable.Range(0, referenceCount)
                .ToList()
                .Select(referenceId => CreateSampleSuggestionReference(id, referenceId.ToString(), suggestion.Text))
                .ToList();

            return suggestion;
        }

        public static SuggestionReference CreateSampleSuggestionReference(string suggestionId, string referenceId, string suggestionText)
        {
            return new SuggestionReference()
            {
                Name = $"reference-{suggestionId}-{referenceId}",
                Url = $"url-{suggestionId}-{referenceId}",
                LicenseName = $"license-{suggestionId}-{referenceId}",
                StartIndex = 0,
                EndIndex = suggestionText.Length,
            };
        }
    }
}
