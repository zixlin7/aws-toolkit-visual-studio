using System;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.Images;
using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class VsImagesTests
    {
        /// <summary>
        /// Verifies that there is a property in VsImages for each
        /// enum value in <see cref="VsKnownImages"/>
        /// </summary>
        [Fact]
        public void ContainsPropertiesForVsKnownImages()
        {
            var propertyNames = typeof(VsImages).GetProperties()
                .Select(property => property.Name)
                .ToList();

            var enumValues = Enum.GetValues(typeof(VsKnownImages))
                .OfType<VsKnownImages>()
                .Select(i => i.ToString())
                .ToList();

            // If this test fails, a property is missing from VsImages for the listed enum values
            Assert.Empty(enumValues.Where(value => !propertyNames.Contains(value)));
        }
    }
}
