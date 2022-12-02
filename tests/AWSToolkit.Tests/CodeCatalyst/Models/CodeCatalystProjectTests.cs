using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.CodeCatalyst.Model;

using Xunit;

namespace AWSToolkit.Tests.CodeCatalyst.Models
{
    public class CodeCatalystProjectTests
    {
        private const string _name = "test-name";
        private const string _spaceName = "my-space";
        private const string _displayName = "Test Name";
        private const string _description = "Description for testing.\nBlah blah blah\nyada yada yada";

        private readonly CodeCatalystProject _sut = new CodeCatalystProject(_name, _spaceName, _displayName, _description);


        [Fact]
        public void PropertiesReflectCtorWithPrimitiveArgs()
        {
            Assert.Equal(_name, _sut.Name);
            Assert.Equal(_spaceName, _sut.SpaceName);
            Assert.Equal(_displayName, _sut.DisplayName);
            Assert.Equal(_description, _sut.Description);
        }

        [Fact]
        public void PropertiesReflectCtorWithAwsSdkArgs()
        {
            var projectSummary = new ProjectSummary()
            {
                Name = _name,
                DisplayName = _displayName,
                Description = _description,
            };

            var sut = new CodeCatalystProject(_spaceName, projectSummary);

            Assert.Equal(_name, sut.Name);
            Assert.Equal(_spaceName, sut.SpaceName);
            Assert.Equal(_displayName, sut.DisplayName);
            Assert.Equal(_description, sut.Description);
        }
    }
}
