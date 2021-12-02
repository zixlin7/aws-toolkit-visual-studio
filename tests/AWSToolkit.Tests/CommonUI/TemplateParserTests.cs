using System.Linq;

using Amazon.AWSToolkit.CloudFormation.Parser;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class TemplateParserTests
    {

        [Fact]
        public void ItShouldHandleBlankString()
        {
            // arrange.
            var parser = new TemplateParser();

            // act.
            var result = parser.Parse("");

            // assert.
            Assert.NotNull(result);
            Assert.Empty(result.HighlightedTemplateTokens);
            Assert.Empty(result.IntellisenseTokens);
            Assert.Equal(-1, result.IntellisenseStartingPosition);
            Assert.Equal(-1, result.IntellisenseEndingPosition);
        }

        [Fact]
        public void ItShouldHandleEmptyObject()
        {
            // arrange.
            var parser = new TemplateParser();

            // act.
            var result = parser.Parse("{}");

            // assert.
            Assert.NotNull(result);
            Assert.Empty(result.HighlightedTemplateTokens);
            Assert.Empty(result.IntellisenseTokens);
            Assert.Equal(-1, result.IntellisenseStartingPosition);
            Assert.Equal(-1, result.IntellisenseEndingPosition);
        }

        [Fact]
        public void ItShouldHandleObjectWithField()
        {
            // arrange.
            var parser = new TemplateParser();

            // act.
            var result = parser.Parse(@"{\""Description\"": \""AwsCloudFormation\""}", 1);

            // assert.
            Assert.NotNull(result);
            Assert.Empty(result.HighlightedTemplateTokens);
            Assert.Equal(10, result.IntellisenseTokens.Count());
            Assert.Equal(-1, result.IntellisenseStartingPosition);
            Assert.Equal(-1, result.IntellisenseEndingPosition);
        }


        [Fact]
        public void ItShouldRevealIntellisenseTokens()
        {
            // arrange.
            var parser = new TemplateParser();

            // act.
            var result = parser.Parse(@"{\""Description\"": \""AwsCloudFormation\""}", 1);

            // assert.
            Assert.Equal(@"""AWSTemplateFormatVersion"" : """"", result.IntellisenseTokens.First().Code);
            Assert.Contains("The template format version is an optional", result.IntellisenseTokens.First().Description);
            Assert.Equal("AWSTemplateFormatVersion", result.IntellisenseTokens.First().DisplayName);
            Assert.Equal(IntellisenseTokenType.ObjectKey, result.IntellisenseTokens.First().Type);
        }

        [Fact]
        public void HappyPath()
        {
            // Within a valid token which is quote wrapped
            var document = "\"Parameters\": ";
            var expected = "Parameters";
            var jsonDocument = new JsonDocument(document);
            var actual = jsonDocument.ReadToken();

            Assert.Equal(expected, actual.Text);
        }

        [Fact]
        public void MissingTrailQuote()
        {
            //Token that lacks a trailing quote
            var document = "\"Def ";
            var expected = "Def ";
            var jsonDocument = new JsonDocument(document);
            var actual = jsonDocument.ReadToken();

            Assert.Equal(expected, actual.Text);
        }

        [Theory]
        [InlineData("\"Def \n ")]
        [InlineData("\"Def \r ")]
        public void StopsOnNewLine(string document)
        {
            //Token with a new line that lacks trail quote
            var expected = "Def ";
            var jsonDocument = new JsonDocument(document);
            var actual = jsonDocument.ReadToken();

            Assert.Equal(expected, actual.Text);
        }

        [Theory]
        [InlineData("AWS::Function")]
        [InlineData("{InnerText}")]
        [InlineData("first-thing,second-thing")]
        public void IgnoresSpecialCharacters(string document)
        {
            var jsonDocument = new JsonDocument(ToJsonValue(document));
            var actual = jsonDocument.ReadToken();

            Assert.Equal(document, actual.Text);
        }

        [Theory]
        [InlineData(@"string with a \""quote\""", "string with a \"quote\"")]
        [InlineData(@"string \n with \f some \r random \t escaped \b chars", "string \n with \f some \r random \t escaped \b chars")]
        public void ProcessesEscapedCharacters(string document, string expected)
        {
            var jsonDocument = new JsonDocument(ToJsonValue(document));
            var actual = jsonDocument.ReadToken();

            Assert.Equal(expected, actual.Text);
        }

        private string ToJsonValue(string s)
        {
            return $@"""{s}""";
        }

        [Fact]
        public void AvoidsOverwriteToTrailQuote()
        {
            //Token that has distant trail quote after separators/new lines
            var document = "\"Def \n{\n{\"";
            var expected = "Def ";
            var jsonDocument = new JsonDocument(document);
            var actual = jsonDocument.ReadToken();

            Assert.Equal(expected, actual.Text);
        }

    }
}
