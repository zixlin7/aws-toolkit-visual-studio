using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;

using Amazon.AWSToolkit.Publish.Converters;
using Amazon.AWSToolkit.Publish.Models;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Converters
{
    public class TargetsToVisibilityConverterTests
    {

        public class TestTargetsToVisibilityConverter : TargetsToVisibilityConverter
        {
            protected override bool HasTargets(object targets)
            {
                return false;
            }
        }

        private static readonly PublishRecommendation _sampleRecommendation = new PublishRecommendation(null);

        private static readonly RepublishTarget _sampleRepublishTarget = new RepublishTarget(null);

        public static IEnumerable<object[]> PublishTargetsData = new List<object[]>
        {
            new object[] {null, Visibility.Visible},
            new object[] {new ObservableCollection<PublishRecommendation>(), Visibility.Visible},
            new object[] {new ObservableCollection<PublishRecommendation>() { _sampleRecommendation }, Visibility.Collapsed},
        };


        public static IEnumerable<object[]> RepublishTargetsData = new List<object[]>
        {
            new object[] {null, Visibility.Visible},
            new object[] {new ObservableCollection<RepublishTarget>(), Visibility.Visible},
             new object[] { new ObservableCollection<RepublishTarget>() { _sampleRepublishTarget }, Visibility.Collapsed},
        };

        [Theory]
        [MemberData(nameof(PublishTargetsData))]
        public void Convert_WhenEmptyPublishTargets(ObservableCollection<PublishRecommendation> targets,
            Visibility expectedVisibility)
        {
            var converter = new PublishTargetsToVisibilityConverter();
            var input = new object[] { true, targets };

            Assert.Equal(expectedVisibility, Convert(converter, input));
        }

        [Theory]
        [MemberData(nameof(RepublishTargetsData))]
        public void Convert_WhenEmptyRepublishTargets(ObservableCollection<RepublishTarget> targets,
          Visibility expectedVisibility)
        {
            var converter = new RepublishTargetsToVisibilityConverter();
            var input = new object[] { true, targets };

            Assert.Equal(expectedVisibility, Convert(converter, input));
        }

        [Theory]
        [InlineData(false, Visibility.Collapsed)]
        [InlineData(true, Visibility.Visible)]
        public void Convert_WhenTargetsLoaded(bool isTargetsLoaded, Visibility expectedVisibility)
        {
             var converter = new TestTargetsToVisibilityConverter();
             var input = new object[] { isTargetsLoaded, null };

            Assert.Equal(expectedVisibility, Convert(converter, input));
        }


        private object Convert(TargetsToVisibilityConverter converter, object[] targets)
        {
            return converter.Convert(targets, typeof(Visibility), null, null);
        }
    }
}
