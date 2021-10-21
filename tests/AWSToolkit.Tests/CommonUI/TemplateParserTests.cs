using Amazon.AWSToolkit.CloudFormation.Parser;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class TemplateParserTests
    {

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
