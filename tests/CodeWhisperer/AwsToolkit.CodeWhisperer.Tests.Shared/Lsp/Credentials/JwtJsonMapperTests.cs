using Amazon.AwsToolkit.CodeWhisperer.Lsp.Credentials;

using FluentAssertions;

using Xunit;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.Lsp.Credentials
{
    public class JwtJsonMapperTests
    {
        private class SampleData
        {
            public string Name { get; set; }
            public int CustomerId { get; set; }
        }

        private readonly JwtJsonMapper _sut = new JwtJsonMapper();
        private readonly SampleData _sampleData = new SampleData()
        {
            Name = "Foo",
            CustomerId = 123456,
        };

        [Fact]
        public void Serialize()
        {
            var json = _sut.Serialize(_sampleData);
            json.Should().Contain("\"name\"")
                .And.Contain($"\"{_sampleData.Name}\"")
                .And.Contain("\"customerId\"");
        }

        [Fact]
        public void Parse()
        {
            var json = _sut.Serialize(_sampleData);
            var sampleData = _sut.Parse<SampleData>(json);
            sampleData.Name.Should().Be(_sampleData.Name);
            sampleData.CustomerId.Should().Be(_sampleData.CustomerId);
        }
    }
}
