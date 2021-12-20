using System.Collections.Generic;
using System.Windows.Data;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class EqualityBooleanConverterTests
    {
        public enum SampleEnum
        {
            Foo,
            Bar,
            Baz,
        }

        private readonly EqualityBooleanConverter _sut = new EqualityBooleanConverter();

        public static IEnumerable<object[]> MatchingValues = new List<object[]>()
        {
            new object[] { "hi", "hi" },
            new object[] { 3, 3 },
            new object[] { true, true },
            new object[] { null, null },
            new object[] { SampleEnum.Foo, SampleEnum.Foo },
        };

        [Theory]
        [MemberData(nameof(MatchingValues))]
        public void Convert_Match(object value, object parameter)
        {
            Assert.True(Convert(value, parameter));
        }

        public static IEnumerable<object[]> NonMatchingValues = new List<object[]>()
        {
            new object[] { "hi", "Hi" },
            new object[] { "hi", "hello" },
            new object[] { 3, 5 },
            new object[] { true, false},
            new object[] { 4, null },
            new object[] { null, 4 },
            new object[] { SampleEnum.Foo, SampleEnum.Bar },
        };

        [Theory]
        [MemberData(nameof(NonMatchingValues))]
        public void Convert_NoMatch(object value, object parameter)
        {
            Assert.False(Convert(value, parameter));
        }

        private bool Convert(object value, object parameter)
        {
            return Assert.IsType<bool>(_sut.Convert(value, null, parameter, null));
        }

        public static IEnumerable<object[]> SampleParameters = new List<object[]>()
        {
            new object[] { "hi" },
            new object[] { 3 },
            new object[] { null },
            new object[] { false },
            new object[] { SampleEnum.Baz },
        };

        [Theory]
        [MemberData(nameof(SampleParameters))]
        public void ConvertBack_FromTrue(object parameter)
        {
            Assert.Equal(parameter, ConvertBack(true, parameter));
        }

        [Theory]
        [MemberData(nameof(SampleParameters))]
        public void ConvertBack_FromFalse(object parameter)
        {
            Assert.Equal(Binding.DoNothing, ConvertBack(false, parameter));
        }

        private object ConvertBack(object value, object parameter)
        {
            return _sut.ConvertBack(value, null, parameter, null);
        }
    }
}
