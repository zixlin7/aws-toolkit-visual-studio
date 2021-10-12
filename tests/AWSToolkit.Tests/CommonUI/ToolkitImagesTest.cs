using System.Linq;
using System.Reflection;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Images;

using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class ToolkitImagesTests
    {
        /// <summary>
        /// Verifies that there is a property in ToolkitImages for each
        /// const string in <see cref="AwsImageResourcePath"/>
        /// </summary>
        [Fact]
        public void ContainsPropertiesForToolkitServiceImages()
        {
            var propertyNames = typeof(ToolkitImages).GetProperties()
                .Select(property => property.Name)
                .ToList();

            var enumValues = typeof(AwsImageResourcePath).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Select(x => x.Name).ToList();

            // If this test fails, a property is missing from ToolkitImages for the listed toolkit image values
            Assert.Empty(enumValues.Where(value => !propertyNames.Contains(value)));
        }
    }
}
