using System.Windows;

using Amazon.AWSToolkit.Publish.Converters;
using Amazon.AWSToolkit.Publish.ViewModels;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Converters
{
    public class PublishFailureBannerToVisibilityConverterTests
    {
        private readonly PublishFailureBannerToVisibilityConverter _sut = new PublishFailureBannerToVisibilityConverter();

        [Theory]
        [InlineData(ProgressStatus.Fail, Visibility.Visible)]
        [InlineData(ProgressStatus.Loading, Visibility.Collapsed)]
        [InlineData(ProgressStatus.Success, Visibility.Collapsed)]
        public void Convert(ProgressStatus status, Visibility expectedVisibility)
        {
            Assert.Equal(expectedVisibility, _sut.Convert(status, typeof(Visibility), null, null));
        }
    }
}
