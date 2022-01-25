using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class KeyValuesConversionTests
    {
        static readonly string SampleJson = "{\"hello\":\"world\",\"foo\":\"bar\"}";

        private static readonly ICollection<KeyValue> SampleKeyValues = new List<KeyValue>()
        {
            new KeyValue() { Key = "hello", Value = "world" },
            new KeyValue() { Key = "foo", Value = "bar" },
        };

        [Fact]
        public void FromJson()
        {
            var keyValues = KeyValuesConversion.FromJson(SampleJson);
            Assert.Equal(SampleKeyValues, keyValues);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void FromJson_Empty(string json)
        {
            var keyValues = KeyValuesConversion.FromJson(json);
            Assert.Empty(keyValues);
        }

        [Theory]
        [InlineData("{ 'foo': ")]
        [InlineData("{ \"foo\": ")]
        public void FromJson_Invalid(string json)
        {
            Assert.ThrowsAny<Exception>(() => { KeyValuesConversion.FromJson(json); });
        }

        [Theory]
        [InlineData("{ \"hello\": [1, 2, 3] }")]
        [InlineData("{ \"hello\": { 'foo': 'bar' } }")]
        public void FromJson_NonDictionary(string json)
        {
            Assert.ThrowsAny<Exception>(() => { KeyValuesConversion.FromJson(json); });
        }

        [Fact]
        public void ToJson()
        {
            var json = KeyValuesConversion.ToJson(SampleKeyValues);
            Assert.Equal(SampleJson, json);
        }
    }
}
