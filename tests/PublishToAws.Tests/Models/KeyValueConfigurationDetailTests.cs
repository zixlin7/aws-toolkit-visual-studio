using System.Collections.Generic;

using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class KeyValueConfigurationDetailTests
    {
        static readonly string SampleJson = "{\"hello\":\"world\",\"foo\":\"bar\"}";

        private static readonly ICollection<KeyValue> SampleKeyValues = new List<KeyValue>()
        {
            new KeyValue() { Key = "hello", Value = "world" },
            new KeyValue() { Key = "foo", Value = "bar" },
        };

        private readonly KeyValueConfigurationDetail _sut = new KeyValueConfigurationDetail();

        [Fact]
        public void ValueUpdatesKeyValues()
        {
            _sut.Value = SampleJson;
            Assert.Equal(SampleKeyValues, _sut.KeyValues.Collection);
        }

        [Theory]
        [InlineData("{ 'foo': ")]
        [InlineData("{ \"foo\": ")]
        [InlineData("{ \"hello\": [1, 2, 3] }")]
        [InlineData("{ \"hello\": { 'foo': 'bar' } }")]
        public void ValueUpdatesKeyValues_NonValidData(string json)
        {
            _sut.Value = json;
            Assert.Empty(_sut.KeyValues.Collection);
        }

        [Fact]
        public void SetKeyValues()
        {
            _sut.SetKeyValues(SampleKeyValues);
            Assert.Equal(SampleJson, _sut.Value);
        }

        [Fact]
        public void GetSummaryValue()
        {
            _sut.SetKeyValues(SampleKeyValues);
            var summary = _sut.GetSummaryValue();

            Assert.Equal("hello: world,\r\nfoo: bar", summary);
        }
    }
}
