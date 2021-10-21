using System.Collections.Generic;
using System.Windows.Media;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Util
{
    public class RecipeServiceImageResolverTests
    {
        public static IEnumerable<object[]> ServiceImageSourceData = new List<object[]>
        {
            new object[] {string.Empty, ToolkitImages.Aws},
            new object[] {null, ToolkitImages.Aws},
            new object[] {"12356sds", ToolkitImages.Aws},
            new object[] {"ABC", ToolkitImages.Aws},
            new object[] {"Amazon EC2", ToolkitImages.Aws},
            new object[] {"Elastic Beanstalk", ToolkitImages.Aws},
            new object[] {"Amazon Elastic Container Service", ToolkitImages.ElasticContainerService},
            new object[] {"AWS Elastic Beanstalk", ToolkitImages.ElasticBeanstalk},
            new object[] {"Amazon S3", ToolkitImages.SimpleStorageService},
            new object[] {"AWS App Runner", ToolkitImages.AppRunner},
        };

        [Theory]
        [MemberData(nameof(ServiceImageSourceData))]
        public void GetServiceImage(string service, ImageSource expectedImageSource)
        {
            ShouldGetExpectedServiceImage(service, expectedImageSource);
        }

        private static void ShouldGetExpectedServiceImage(string service, ImageSource expectedIImageSource)
        {
            var actualImageSource = RecipeServiceImageResolver.GetServiceImage(service);
            Assert.True(expectedIImageSource.IsEqual(actualImageSource));
        }
    }
}
