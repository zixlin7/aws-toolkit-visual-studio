using System.Collections.Generic;

using Amazon.AwsToolkit.CodeWhisperer.Suggestions.Models;
using Amazon.AWSToolkit.Collections;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Suggestions
{
    public class SuggestionSessionTests
    {
        public static readonly IEnumerable<object[]> InvalidData = new[]
        {
            new object[] { "hello", new List<Suggestion>() },
            new object[] { "", new List<Suggestion> { new Suggestion() } },
            new object[] { "", new List<Suggestion>() },
            new object[] { null, new List<Suggestion> { new Suggestion() } },
            new object[] { null, new List<Suggestion>() },
        };

        [Fact]
        public void IsValid_WhenTrue()
        {
            var suggestionSession = new SuggestionSession() { SessionId = "sample-sessionId" };
            suggestionSession.Suggestions.Add(new Suggestion());

            var result = suggestionSession.IsValid();
            result.Should().BeTrue();
        }

        [Theory]
        [MemberData(nameof(InvalidData))]
        public void IsValid_WhenFalse(string sessionId, List<Suggestion> suggestions)
        {
            var suggestionSession = new SuggestionSession() { SessionId = sessionId };
            suggestionSession.Suggestions.AddAll(suggestions);

            var result = suggestionSession.IsValid();
            result.Should().BeFalse();
        }
    }
}
