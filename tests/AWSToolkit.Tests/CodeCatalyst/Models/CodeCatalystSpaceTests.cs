using Amazon.AWSToolkit.CodeCatalyst.Models;
using Amazon.CodeCatalyst.Model;

using Xunit;

namespace AWSToolkit.Tests.CodeCatalyst.Models
{
    public class CodeCatalystSpaceTests
    {
        private const string _name = "test-name";
        private const string _displayName = "Test Name";
        private const string _description = "Description for testing.\nBlah blah blah\nyada yada yada";
        private const string _regionId = "un-real-1";

        private readonly CodeCatalystSpace _sut = new CodeCatalystSpace(_name, _displayName, _description, _regionId);

        [Fact]
        public void PropertiesReflectCtorWithPrimitiveArgs()
        {
            Assert.Equal(_name, _sut.Name);
            Assert.Equal(_displayName, _sut.DisplayName);
            Assert.Equal(_description, _sut.Description);
            Assert.Equal(_regionId, _sut.RegionId);
        }

        [Fact]
        public void PropertiesReflectCtorWithAwsSdkArgs()
        {
            var spaceSummary = new SpaceSummary()
            {
                Name = _name,
                DisplayName = _displayName,
                Description = _description,
                RegionName = _regionId
            };

            var sut = new CodeCatalystSpace(spaceSummary);

            Assert.Equal(_name, sut.Name);
            Assert.Equal(_displayName, sut.DisplayName);
            Assert.Equal(_description, sut.Description);
            Assert.Equal(_regionId, sut.RegionId);
        }
    }
}
