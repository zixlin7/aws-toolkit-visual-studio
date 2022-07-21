using System;
using System.Linq;
using System.Reflection;
using Amazon.AWSToolkit.CommonUI.Images;
using Microsoft.VisualStudio.Imaging;
using Xunit;

namespace AwsToolkit.Vs.Tests.VsSdk.Common.CommonUI
{
    public class VsKnownImagesTests
    {
        /// <summary>
        /// This test verifies that enum values added to <see cref="VsKnownImages"/>
        /// have a matching property in <see cref="KnownMonikers"/>.
        /// </summary>
        [Fact]
        public void EnumValuesMatchKnownMonikers()
        {
            var mismatchedValues = Enum.GetValues(typeof(VsKnownImages))
                .OfType<VsKnownImages>()
                .Where(knownImage => !IsKnownMoniker(knownImage))
                .ToList();

            // If this test fails, the listed enum values could not be located in KnownMonikers
            Assert.Empty(mismatchedValues);
        }

        private bool IsKnownMoniker(VsKnownImages knownImage)
        {
            var imageStr = knownImage.ToString();
            var property = typeof(KnownMonikers).GetProperty(imageStr,
                BindingFlags.Static | BindingFlags.Public);

            return property != null;
        }
    }
}
