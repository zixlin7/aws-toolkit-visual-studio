using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Amazon.AWSToolkit.Publish.Converters;
using Amazon.AWSToolkit.Publish.Models;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Converters
{
    public class PublishResourcesToVisibilityConverterTests
    {
        private readonly PublishResourcesToVisibilityConverter _sut = new PublishResourcesToVisibilityConverter();

        public static IEnumerable<object[]> PublishResourceData = new List<object[]>
        {
            new object[] {null, Visibility.Collapsed},
            new object[] {new ObservableCollection<PublishResource>(), Visibility.Collapsed},
            new object[] {new ObservableCollection<PublishResource>() { CreateEmptyPublishResource()}, Visibility.Visible},
        };

        [Theory]
        [MemberData(nameof(PublishResourceData))]
        public void Convert(object value, Visibility expectedVisibility)
        {
            Assert.Equal(expectedVisibility, _sut.Convert(value, typeof(Visibility), null, null));
        }

        [Fact]
        public void ConvertBack()
        {
            Assert.Null(_sut.ConvertBack(null, typeof(Visibility), null, null));
        }

        private static PublishResource CreateEmptyPublishResource()
        {
            return new PublishResource(null, null, null, null);
        }

    }
}
