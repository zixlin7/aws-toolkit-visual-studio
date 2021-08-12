using System;

using Amazon.AWSToolkit.CloudFormation.Parser;

using Xunit;
using Xunit.Abstractions;

namespace Amazon.AWSToolkit.Util.Tests.Settings
{
    public class TemplateParserTests
    {
        private readonly ITestOutputHelper _output;

        public TemplateParserTests(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public void HappyPath()
        {
            // Within a valid token which is quote wrapped
            var document = "\"Parameters\": ";
            var expected = "Parameters";
            var templateParser = new JsonDocument(document);
            var actual = templateParser.ReadToken();

            this._output.WriteLine(actual.ToString());

            Assert.Equal(expected, actual.Text);
        }

        [Fact]
        public void MissingTrailQuote()
        {
            //Token that lacks a trailing quote
            var document = "\"Def ";
            var expected = "Def ";
            var templateParser = new JsonDocument(document);
            var actual = templateParser.ReadToken();

            this._output.WriteLine(actual.ToString());

            Assert.Equal(expected, actual.Text);
        }

        [Theory]
        [InlineData("\"Def \n ")]
        [InlineData("\"Def \r ")]
        public void StopsOnNewLine(string document)
        {
            //Token with a new line that lacks trail quote
            var expected = "Def ";
            var templateParser = new JsonDocument(document);
            var actual = templateParser.ReadToken();

            this._output.WriteLine(actual.ToString());

            Assert.Equal(expected, actual.Text);
        }

        [Theory]
        [InlineData("\"Def : ")]
        [InlineData("\"Def { ")]
        public void StopsOnSeparators(string document)
        {
            //Token that reaches separator before a trail quote
            var expected = "Def ";
            var templateParser = new JsonDocument(document);
            var actual = templateParser.ReadToken();

            this._output.WriteLine(actual.ToString());

            Assert.Equal(expected, actual.Text);
        }

        [Fact]
        public void AvoidsOverwriteToTrailQuote()
        {
            //Token that has distant trail quote after separators/new lines
            var document = "\"Def \n{\n{\"";
            var expected = "Def ";
            var templateParser = new JsonDocument(document);
            var actual = templateParser.ReadToken();

            this._output.WriteLine(actual.ToString());

            Assert.Equal(expected, actual.Text);
        }
    }
}
