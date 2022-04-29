using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.Util;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using AWS.Deploy.ServerMode.Client;

using Moq;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class ConfigurationDetailFactoryTests
    {
        private static readonly Dictionary<string, DetailType> TypeMappings = new Dictionary<string, DetailType>()
        {
            {"String", DetailType.String},
            {"Int", DetailType.Integer},
            {"Double", DetailType.Double},
            {"Bool", DetailType.Boolean},
            {"Object", DetailType.Blob},
            {"List", DetailType.List},
            {"SomeFutureType", DetailType.Unsupported}
        };

        private readonly Mock<IPublishToAwsProperties> _publishProperties = new Mock<IPublishToAwsProperties>();
        private readonly Mock<IDialogFactory> _dialogFactory = new Mock<IDialogFactory>();
        private readonly ConfigurationDetailFactory _sut;

        public ConfigurationDetailFactoryTests()
        {
            _sut = new ConfigurationDetailFactory(_publishProperties.Object, _dialogFactory.Object);
        }

        [Fact]
        public void CreateFrom()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create().UseSampleData().Build();
            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Theory]
        [InlineData("String", "string-value")]
        [InlineData("Int", 1234)]
        [InlineData("Double", 1234.56)]
        [InlineData("Bool", true)]
        [InlineData("Object", "object-value")]
        [InlineData("SomeFutureType", null)]
        public void CreateFrom_WithType(string settingType, object value)
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType(settingType)
                .WithValue(value)
                .Build();
            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Fact]
        public void CreateFrom_WithList()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType("List")
                .WithValue("[\"value1\", \"value2\"]")
                .WithValueMapping("display 1", "value1")
                .WithValueMapping("display 2", "value2")
                .WithValueMapping("display 3", "value3")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary) as ListConfigurationDetail;
            AssertPropertiesMatch(configurationDetail, itemSummary);

            var expectedItems = new[]
            {
                new ListConfigurationDetail.ListItem("display 1", "value1"),
                new ListConfigurationDetail.ListItem("display 2", "value2"),
                new ListConfigurationDetail.ListItem("display 3", "value3")
            };

            var expectedSelectedItems = new[]
            {
                new ListConfigurationDetail.ListItem("display 1", "value1"),
                new ListConfigurationDetail.ListItem("display 2", "value2")
            };

            Assert.True(expectedItems.SequenceEqual(configurationDetail.Items));
            Assert.True(expectedSelectedItems.SequenceEqual(configurationDetail.SelectedItems));
        }

        [Fact]
        public void CreateFrom_WithValueMapping()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithValueMapping("Key1", "Value 1")
                .WithValueMapping("Key2", "Value 2")
                .WithValue("Key2")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Fact]
        public void CreateFrom_WithAllowedValues()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithAllowedValue("Value 1")
                .WithAllowedValue("Value 2")
                .WithAllowedValue("Value 3")
                .WithValue("Value 2")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Fact]
        public void CreateFrom_WithValueMappingAndAllowedValue()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithValueMapping("Key1", "Value 1")
                .WithValueMapping("Key2", "Value 2")
                .WithValueMapping("Key3", "Value 3")
                .WithAllowedValue("Key1")
                .WithAllowedValue("Key2")
                .WithValue("Key2")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Fact]
        public void CreateFrom_WithChildren()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithName("I have children")
                .WithType("Object")
                .WithChild(OptionSettingItemSummaryBuilder.Create().UseSampleData().WithName("child 1"))
                .WithChild(OptionSettingItemSummaryBuilder.Create().UseSampleData().WithName("child 2"))
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        [Fact]
        public void CreateFrom_WithNestedChildren()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithName("I have nested children")
                .WithType("Object")
                .WithChild(
                    OptionSettingItemSummaryBuilder.Create().UseSampleData().WithName("child").WithType("Object")
                        .WithChild(OptionSettingItemSummaryBuilder.Create().UseSampleData().WithName("grandchild")))
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            AssertPropertiesMatch(configurationDetail, itemSummary);
        }

        private void AssertPropertiesMatch(ConfigurationDetail configurationDetail, OptionSettingItemSummary expectedProperties)
        {
            Assert.NotNull(configurationDetail);
            Assert.Equal(expectedProperties.Id, configurationDetail.Id);
            Assert.Equal(expectedProperties.Name, configurationDetail.Name);
            Assert.Equal(expectedProperties.Description, configurationDetail.Description);
            Assert.Equal(TypeMappings[expectedProperties.Type], configurationDetail.Type);
            Assert.Equal(expectedProperties.TypeHint, configurationDetail.TypeHint);
            Assert.Equal(GetExpectedValue(expectedProperties), configurationDetail.Value);
            Assert.Equal(expectedProperties.Value, configurationDetail.DefaultValue);
            Assert.Equal(expectedProperties.Advanced, configurationDetail.Advanced);
            Assert.Equal(expectedProperties.ReadOnly, configurationDetail.ReadOnly);
            Assert.Equal(expectedProperties.Visible, configurationDetail.Visible);
            Assert.Equal(expectedProperties.SummaryDisplayable, configurationDetail.SummaryDisplayable);

            AssertValueMappingProperties(expectedProperties, configurationDetail.ValueMappings);

            Assert.Equal(expectedProperties.ChildOptionSettings?.Count ?? 0, configurationDetail.Children.Count);
            Enumerable.Range(0, configurationDetail.Children.Count)
                .ToList()
                .ForEach(index =>
                {
                    var childOption = expectedProperties.ChildOptionSettings.Skip(index).First();
                    var childDetail = configurationDetail.Children[index];

                    Assert.Equal(configurationDetail, childDetail.Parent);
                    AssertPropertiesMatch(childDetail, childOption);
                });
        }

        private object GetExpectedValue(OptionSettingItemSummary expectedProperties)
        {
            if (expectedProperties.HasValueMapping() || expectedProperties.HasAllowedValues())
            {
                return Convert.ToString(expectedProperties.Value, CultureInfo.InvariantCulture);
            }
            return expectedProperties.Value;
        }

        private void AssertValueMappingProperties(OptionSettingItemSummary expectedProperties, IDictionary<string, string> valueMappings)
        {
            Assert.NotNull(valueMappings);

            if (expectedProperties.HasValueMapping())
            {
                Assert.Equal(expectedProperties.ValueMapping, valueMappings);
            }
            else if (expectedProperties.HasAllowedValues())
            {
                Assert.Equal(expectedProperties.AllowedValues.ToDictionary(x => x, x => x), valueMappings);
            }
        }

        [Fact]
        public void CreateFrom_IamRoleTypeHint()
        {
            var itemSummary = CreateSampleIamRoleItemSummary()
                .WithTypeHintData(IamRoleConfigurationDetail.TypeHintDataKeys.ServicePrincipal, "some-service-principal")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            var roleDetail = Assert.IsType<IamRoleConfigurationDetail>(configurationDetail);
            AssertPropertiesMatch(configurationDetail, itemSummary);
            Assert.NotNull(roleDetail.SelectRoleArn);
            Assert.Equal("some-arn", roleDetail.RoleArnDetail.Value);
            Assert.Equal("some-service-principal", roleDetail.ServicePrincipal);
            Assert.True(roleDetail.CreateNewRole);
        }

        [Fact]
        public void CreateFrom_IamRoleTypeHint_NoServicePrincipal()
        {
            var itemSummary = CreateSampleIamRoleItemSummary()
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            var roleDetail = Assert.IsType<IamRoleConfigurationDetail>(configurationDetail);
            AssertPropertiesMatch(configurationDetail, itemSummary);
            Assert.Null(roleDetail.ServicePrincipal);
        }

        private OptionSettingItemSummaryBuilder CreateSampleIamRoleItemSummary()
        {
            return OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType("Object")
                .WithTypeHint(ConfigurationDetail.TypeHints.IamRole)
                .WithChild(OptionSettingItemSummaryBuilder.Create()
                    .WithId("CreateNew")
                    .WithType("Bool")
                    .WithValue(true))
                .WithChild(OptionSettingItemSummaryBuilder.Create()
                    .WithId("RoleArn")
                    .WithType("String")
                    .WithValue("some-arn"));
        }

        [Fact]
        public void CreateFrom_EcrRepositoryTypeHint()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType("String")
                .WithTypeHint(ConfigurationDetail.TypeHints.EcrRepository)
                .WithValue("some-repo")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            var detail = Assert.IsType<EcrRepositoryConfigurationDetail>(configurationDetail);
            AssertPropertiesMatch(configurationDetail, itemSummary);
            Assert.Equal("some-repo", detail.Value);
        }

        [Fact]
        public void CreateFrom_VpcTypeHint()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType("Object")
                .WithTypeHint(ConfigurationDetail.TypeHints.Vpc)
                .WithChild(OptionSettingItemSummaryBuilder.Create()
                    .WithId(VpcConfigurationDetail.ChildDetailIds.CreateNew)
                    .WithType("Bool")
                    .WithValue(false))
                .WithChild(OptionSettingItemSummaryBuilder.Create()
                    .WithId(VpcConfigurationDetail.ChildDetailIds.IsDefault)
                    .WithType("Bool")
                    .WithValue(false))
                .WithChild(OptionSettingItemSummaryBuilder.Create()
                    .WithId(VpcConfigurationDetail.ChildDetailIds.VpcId)
                    .WithType("String")
                    .WithValue("vpc-1234abcd"))
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            var vpcDetail = Assert.IsType<VpcConfigurationDetail>(configurationDetail);
            AssertPropertiesMatch(configurationDetail, itemSummary);
            Assert.NotNull(vpcDetail.VpcIdDetail);
            Assert.Equal("vpc-1234abcd", vpcDetail.VpcIdDetail.Value);
            Assert.Equal(VpcOption.Existing, vpcDetail.VpcOption);
        }

        [Fact]
        public void CreateFrom_InstanceTypeTypeHint()
        {
            var itemSummary = OptionSettingItemSummaryBuilder.Create()
                .UseSampleData()
                .WithType("String")
                .WithTypeHint(ConfigurationDetail.TypeHints.InstanceType)
                .WithValue("t3.micro")
                .Build();

            var configurationDetail = _sut.CreateFrom(itemSummary);
            var instanceTypeDetail = Assert.IsType<Ec2InstanceConfigurationDetail>(configurationDetail);
            Assert.Equal(itemSummary.Value, instanceTypeDetail.InstanceTypeId);
            Assert.NotNull(instanceTypeDetail.SelectInstanceType);
        }
    }
}
