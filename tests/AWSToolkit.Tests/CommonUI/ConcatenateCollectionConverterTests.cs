using System.Collections;
using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class ConcatenateCollectionConverterTests
    {
        private readonly ConcatenateCollectionConverter _sut = new ConcatenateCollectionConverter();
        private static readonly IList<string> SampleTextCollection = new List<string>() { "foo", "bar", "baz" };
        private static readonly IList<int> SampleNumberCollection = new List<int>() { 1, 10, 100 };

        public static readonly IEnumerable<object[]> ConvertInputs = new List<object[]>()
        {
            new object[] { SampleTextCollection, "", "foobarbaz" },
            new object[] { SampleTextCollection, "-", "foo-bar-baz" },
            new object[] { SampleTextCollection, ", ", "foo, bar, baz" },
            new object[] { SampleNumberCollection, ", ", "1, 10, 100" },
        };

        [Theory]
        [MemberData(nameof(ConvertInputs))]
        public void Convert(IEnumerable collection, string separator, string expectedResult)
        {
            Assert.Equal(expectedResult, CallConvert(collection, separator));
        }

        private object CallConvert(IEnumerable collection, string separator)
        {
            return _sut.Convert(collection, null, separator, null);
        }
    }
}
