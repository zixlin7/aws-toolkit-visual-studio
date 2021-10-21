using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Amazon.AWSToolkit.Publish.Converters;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Converters
{
    public class ConfigurationDetailsToVisibilityConverterTests
    {
        private readonly ConfigurationDetailsToVisibilityConverter _converter =
            new ConfigurationDetailsToVisibilityConverter();

        public static IEnumerable<object[]> ConfigurationEmptyData = new List<object[]>
        {
            new object[] {null, Visibility.Visible},
            new object[] {new ObservableCollection<ConfigurationDetail>(), Visibility.Visible},
        };

        [Theory]
        [MemberData(nameof(ConfigurationEmptyData))]
        public void Convert_WhenEmptyConfigDetails(ObservableCollection<ConfigurationDetail> configurationDetails,
            Visibility expectedVisibility)
        {
            Assert.Equal(expectedVisibility, Convert(configurationDetails, null));
        }

        [Theory]
        [InlineData("hello")]
        [InlineData(56)]
        [InlineData(null)]
        public void ConvertThrows_WhenInvalidParameter(object parameter)
        {
            var sampleDetails =
                new ObservableCollection<ConfigurationDetail>() {new ConfigurationDetail()};

            Assert.Throws<ArgumentException>(() => Convert(sampleDetails, parameter));
        }


        public static IEnumerable<object[]> ConfigurationAdvancedData = new List<object[]>
        {
            new object[] {ConfigurationDetailBuilder.Create().IsAdvanced().IsVisible().Build(), Visibility.Collapsed},
            new object[] {ConfigurationDetailBuilder.Create().IsAdvanced().Build(), Visibility.Visible},
            new object[] {ConfigurationDetailBuilder.Create().IsVisible().Build(), Visibility.Collapsed},
            new object[] {ConfigurationDetailBuilder.Create().Build(), Visibility.Visible},
        };

        [Theory]
        [MemberData(nameof(ConfigurationAdvancedData))]
        public void Convert_WhenAdvanced(ConfigurationDetail inputConfigurationDetail, Visibility expectedVisibility)
        {
            var configurationDetails = new ObservableCollection<ConfigurationDetail>() {inputConfigurationDetail};
            Assert.Equal(expectedVisibility, Convert(configurationDetails, "False"));
        }


        public static IEnumerable<object[]> ConfigurationCoreData = new List<object[]>
        {
            new object[] {ConfigurationDetailBuilder.Create().IsAdvanced().IsVisible().Build(), Visibility.Visible},
            new object[] {ConfigurationDetailBuilder.Create().IsAdvanced().Build(), Visibility.Visible},
            new object[] {ConfigurationDetailBuilder.Create().IsVisible().Build(), Visibility.Collapsed},
            new object[] {ConfigurationDetailBuilder.Create().Build(), Visibility.Visible},
        };


        [Theory]
        [MemberData(nameof(ConfigurationCoreData))]
        public void Convert_WhenCore(ConfigurationDetail inputConfigurationDetail, Visibility expectedVisibility)
        {
            var configurationDetails = new ObservableCollection<ConfigurationDetail>() {inputConfigurationDetail};
            Assert.Equal(expectedVisibility, Convert(configurationDetails, "True"));
        }

        private object Convert(ObservableCollection<ConfigurationDetail> configurationDetails, object parameter)
        {
            return _converter.Convert(configurationDetails, typeof(Visibility), parameter, null);
        }
    }
}
