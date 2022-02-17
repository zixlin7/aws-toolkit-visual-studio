using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

using Amazon.AWSToolkit.CommonUI.Converters;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class ConverterPipelineTests
    {
        private readonly ConverterPipeline _sut = new ConverterPipeline();

        [Fact]
        public void Convert()
        {
            _sut.Converters.Add(new EqualityBooleanConverter());
            _sut.Converters.Add(new BooleanToVisibilityConverter());

            Assert.Equal(Visibility.Visible, _sut.Convert(123, null, 123, null));
            Assert.Equal(Visibility.Collapsed, _sut.Convert(123, null, 456, null));
        }

        [Fact]
        public void ConvertBack()
        {
            _sut.Converters.Add(new EqualityBooleanConverter());
            _sut.Converters.Add(new BooleanToVisibilityConverter());

            Assert.Equal(123, _sut.ConvertBack(Visibility.Visible, null, 123, null));
            Assert.Equal(Binding.DoNothing, _sut.ConvertBack(Visibility.Collapsed, null, 123, null));
        }
    }
}
