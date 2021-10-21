using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.ViewModels;

using AWS.Deploy.ServerMode.Client;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class DeployToolControllerExtensionsTests
    {
        private static readonly Dictionary<string, string> SampleValueMappings = new Dictionary<string, string>()
        {
            {"256", "256"},
            {"512", "512"},
            {"1024", "1024"},
            {"2048", "2048"},
            {"4096", "4096"}
        };

        private static OptionSettingItemSummary CreateSampleSettingA()
        {
            return new OptionSettingItemSummary()
            {
                Type = "String",
                Name = "field-1-parent",
                Value = "parent-value1",
                Description = "description",
                Visible = true,
                SummaryDisplayable = true,
                ReadOnly = false,
            };
        }
        private static OptionSettingItemSummary CreateSampleSettingB()
        {
            return new OptionSettingItemSummary()
            {
                Type = "Int",
                Name = "field-2",
                Value = 2,
                Description = "description",
                Visible = true,
                ReadOnly= false
            };
        }

        private static OptionSettingItemSummary CreateSampleSettingC()
        {
            return new OptionSettingItemSummary()
            {
                Type = "Int",
                Name = "field-3",
                Value = 2,
                Description = "allowed values with value mappings",
                Visible = true,
                AllowedValues = SampleValueMappings.Keys.ToArray(),
                ValueMapping = SampleValueMappings
            };
        }

        private static OptionSettingItemSummary CreateSampleSettingD()
        {
            return new OptionSettingItemSummary()
            {
                Type = "Int",
                Name = "field-4",
                Value = false,
                Description = "allowed values without value mappings",
                Visible = true,
                AllowedValues = SampleValueMappings.Keys.ToArray()
            };
        }
        public static IEnumerable<object[]> CreateValueMappingData()
        {
            return new List<object[]>
            {
                new object[] {CreateSampleSettingA(), new Dictionary<string, string>()},
                new object[] {CreateSampleSettingC(), SampleValueMappings},
                new object[] {CreateSampleSettingD(), SampleValueMappings},
            };
        }

        public static IEnumerable<object[]> CreateValueTypeData()
        {
            return new List<object[]>
            {
                new object[] {CreateSampleSettingB(), typeof(int)},
                new object[] {CreateSampleSettingC(), typeof(string)},
                new object[] {CreateSampleSettingD(), typeof(string)},
            };
        }

        public DeployToolControllerExtensionsTests()
        {
        }

        [Theory]
        [MemberData(nameof(CreateValueMappingData))]
        public void ToConfigurationDetail_HandlesValueMappings(OptionSettingItemSummary sampleItemSummary, Dictionary<string, string> expectedMappings)
        {
            var detail = sampleItemSummary?.ToConfigurationDetail();
            Assert.Equal(expectedMappings, detail?.ValueMappings);
        }


        [Theory]
        [MemberData(nameof(CreateValueTypeData))]
        public void ToConfigurationDetail_HandlesValueTypeWithValueMapping(OptionSettingItemSummary sampleItemSummary, Type expectedValueType)
        {
            var detail = sampleItemSummary?.ToConfigurationDetail();
            Assert.Equal(expectedValueType, detail?.Value.GetType());
        }

        [Fact]
        public void ToConfigurationDetail_HandlesId()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Id, detail?.Id);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesName()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Name, detail?.Name);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesDescription()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Description, detail?.Description);
        }

        [Theory]
        [InlineData("String", typeof(string))]
        [InlineData("Int", typeof(int))]
        [InlineData("Double", typeof(double))]
        [InlineData("Bool", typeof(bool))]
        [InlineData("Object", typeof(object))]
        public void ToConfigurationDetail_HandlesType(string settingType, Type expectedType)
        {
            var sampleSetting = CreateSampleSettingA();
            sampleSetting.Type = settingType;

            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(expectedType, detail?.Type);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesTypeHint()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.TypeHint, detail?.TypeHint);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesValue()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Value, detail?.Value);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesDefaultValue()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Value, detail?.DefaultValue);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesCategory()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Empty(detail.Category);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesAdvanced()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.Advanced, detail?.Advanced);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesSummaryDisplayable()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(sampleSetting.SummaryDisplayable, detail?.SummaryDisplayable);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToConfigurationDetail_HandlesReadOnly(bool readOnly)
        {
            var sampleSetting = CreateSampleSettingA();
            sampleSetting.ReadOnly = readOnly;

            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(readOnly, detail?.ReadOnly);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void ToConfigurationDetail_HandlesVisible(bool visible)
        {
            var sampleSetting = CreateSampleSettingA();
            sampleSetting.Visible = visible;

            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Equal(visible, detail?.Visible);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesNullParent()
        {
            var sampleSetting = CreateSampleSettingA();
            var detail = sampleSetting.ToConfigurationDetail();

            Assert.Null(detail?.Parent);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesParent()
        {
            var sampleSettingA = CreateSampleSettingA();
            var sampleSettingB = CreateSampleSettingB();

            var parent = sampleSettingB.ToConfigurationDetail();

            var detail = sampleSettingA.ToConfigurationDetail(parent);

            Assert.Equal(parent, detail?.Parent);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesChildren_WithNonObjectType()
        {
            var sampleSettingA = CreateSampleSettingA();
            var sampleSettingB = CreateSampleSettingB();
            sampleSettingA.ChildOptionSettings = new List<OptionSettingItemSummary>() {sampleSettingB};

            var detail = sampleSettingA.ToConfigurationDetail();

            Assert.Empty(detail.Children);
        }

        [Fact]
        public void ToConfigurationDetail_HandlesChildren()
        {
            var setting = new OptionSettingItemSummary
            {
                Type = "Object",
                Name = "Person",
                ChildOptionSettings = new List<OptionSettingItemSummary>()
                {
                    new OptionSettingItemSummary
                    {
                        Type = "String",
                        Name = "Name",
                    },
                    new OptionSettingItemSummary
                    {
                        Type = "Int",
                        Name = "Age",
                    },
                    new OptionSettingItemSummary
                    {
                        Type = "Object",
                        Name = "Address",
                        ChildOptionSettings = new List<OptionSettingItemSummary>()
                        {
                            new OptionSettingItemSummary
                            {
                                Type = "String",
                                Name = "Street",
                            },
                            new OptionSettingItemSummary
                            {
                                Type = "String",
                                Name = "City",
                            }
                        }
                    }
                }
            };

            var detail = setting.ToConfigurationDetail();

            Assert.Equal(3, detail.Children.Count);
            var name = Assert.Single(detail.Children, child => child.Name == "Name");
            Assert.Empty(name.Children);
            var age = Assert.Single(detail.Children, child => child.Name == "Age");
            Assert.Empty(age.Children);
            var address = Assert.Single(detail.Children, child => child.Name == "Address");
            Assert.Equal(2, address.Children.Count);
            Assert.Single(address.Children, child => child.Name == "Street");
            Assert.Single(address.Children, child => child.Name == "City");
        }
    }
}
