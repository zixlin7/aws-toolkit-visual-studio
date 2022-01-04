using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Tests.Publishing.Common;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.ViewModels
{
    public class UnsupportedSettingTypesTests
    {
        private readonly UnsupportedSettingTypes _unsupportedSettingTypes;
        private readonly PublishContextFixture _publishContextFixture = new PublishContextFixture();
        private const string SampleRecipeId = "sampleRecipeId";
        private const string SampleUnsupportedOriginalType = "unsupported1";

        public static IEnumerable<object[]> InvalidConfigDetailsData = new List<object[]>
        {
            new object[] {null},
            new object[] {new List<ConfigurationDetail>()},
        };

        public UnsupportedSettingTypesTests()
        {
            _unsupportedSettingTypes = new UnsupportedSettingTypes();
        }

        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(int))]
        [InlineData(typeof(bool))]
        [InlineData(typeof(double))]
        public void Update_NoUnsupportedSettingType(Type settingType)
        {
            var sampleConfigDetails = CreateSampleConfigDetails(settingType, settingType.ToString());

            _unsupportedSettingTypes.Update(SampleRecipeId, sampleConfigDetails);

            Assert.Empty(_unsupportedSettingTypes.RecipeToUnsupportedSetting.Keys);
        }


        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("     ")]
        public void Update_InvalidRecipe(string recipeId)
        {
            var sampleConfigDetails = CreateSampleConfigDetails(typeof(UnsupportedType), SampleUnsupportedOriginalType);

            _unsupportedSettingTypes.Update(recipeId, sampleConfigDetails);

            Assert.Empty(_unsupportedSettingTypes.RecipeToUnsupportedSetting.Keys);
        }

        [Theory]
        [MemberData(nameof(InvalidConfigDetailsData))]
        public void Update_InvalidConfigDetails(List<ConfigurationDetail> configurationDetails)
        {
            _unsupportedSettingTypes.Update(SampleRecipeId, configurationDetails);

            Assert.Empty(_unsupportedSettingTypes.RecipeToUnsupportedSetting.Keys);
        }

        [Fact]
        public void Update_WhenEmpty()
        {
            var sampleConfigDetails = CreateSampleConfigDetails(typeof(UnsupportedType), SampleUnsupportedOriginalType);

            _unsupportedSettingTypes.Update(SampleRecipeId, sampleConfigDetails);

            var values = _unsupportedSettingTypes.RecipeToUnsupportedSetting[SampleRecipeId];
            Assert.Contains(SampleUnsupportedOriginalType, values);
        }

        [Fact]
        public void Update_WhenNotEmpty()
        {
            var configDetails1 = CreateSampleConfigDetails(typeof(UnsupportedType), SampleUnsupportedOriginalType);
            _unsupportedSettingTypes.Update(SampleRecipeId, configDetails1);

            var configDetails2 = CreateSampleConfigDetails(typeof(UnsupportedType), "unsupported2");
            _unsupportedSettingTypes.Update(SampleRecipeId, configDetails2);

            var settingTypes = _unsupportedSettingTypes.RecipeToUnsupportedSetting[SampleRecipeId];
            Assert.Contains(SampleUnsupportedOriginalType, settingTypes);
            Assert.Contains("unsupported2", settingTypes);
        }

        [Fact]
        public void RecordMetric()
        {
            SetupUnsupportedSettingTypes(SampleUnsupportedOriginalType);
            SetupUnsupportedSettingTypes("unsupported2");

            var publishContext = new PublishApplicationContext(_publishContextFixture.PublishContext);
            _unsupportedSettingTypes.RecordMetric(publishContext);

            _publishContextFixture.TelemetryLogger.Verify(
                mock => mock.Record(It.IsAny<Metrics>()), Times.Exactly(2));
        }

        private void SetupUnsupportedSettingTypes(string optionSettingType)
        {
            var configDetails1 = CreateSampleConfigDetails(typeof(UnsupportedType), optionSettingType);
            _unsupportedSettingTypes.Update(SampleRecipeId, configDetails1);
        }

        private List<ConfigurationDetail> CreateSampleConfigDetails(Type settingType, string originalType)
        {
            var configDetail = ConfigurationDetailBuilder.Create().WithType(settingType)
                .WithOriginalType(originalType).Build();
            return new List<ConfigurationDetail>() { configDetail };
        }
    }
}
