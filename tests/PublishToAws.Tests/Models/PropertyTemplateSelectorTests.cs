using System;
using System.Collections.Generic;
using System.Windows;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class PropertyTemplateSelectorTests
    {
        private readonly PropertyTemplateSelector _sut = new PropertyTemplateSelector()
        {
            ParentEditor = new DataTemplate(),
            BooleanEditor = new DataTemplate(),
            NumericEditor = new DataTemplate(),
            TextEditor = new DataTemplate(),
            UnsupportedTypeEditor = new DataTemplate(),
            EnumEditor = new DataTemplate(),
            IamRoleEditor = new DataTemplate(),
            VpcEditor = new DataTemplate(),
            Ec2InstanceTypeEditor = new DataTemplate(),
            EcrRepositoryEditor = new DataTemplate(),
            ListEditor = new DataTemplate()
        };

        [Fact]
        public void SelectTemplate_NullItem()
        {
            Assert.Null(_sut.SelectTemplate(null, null));
        }

        [Fact]
        public void SelectTemplate_NonConfigurationDetailItem()
        {
            Assert.Null(_sut.SelectTemplate("hello-world", null));
        }

        [Fact]
        public void SelectTemplate_StringDetail()
        {
            var detail = new ConfigurationDetail() {Type = DetailType.String};

            Assert.Equal(_sut.TextEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_ObjectDetail()
        {
            var detail = new ConfigurationDetail() {Type = DetailType.Blob};

            Assert.Equal(_sut.ParentEditor, _sut.SelectTemplate(detail, null));
        }

        [Theory]
        [InlineData(DetailType.Integer)]
        [InlineData(DetailType.Double)]
        public void SelectTemplate_NumericDetail(DetailType numericType)
        {
            var detail = new ConfigurationDetail() {Type = numericType};

            Assert.Equal(_sut.NumericEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_BoolDetail()
        {
            var detail = new ConfigurationDetail() {Type = DetailType.Boolean};

            Assert.Equal(_sut.BooleanEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_ListDetail()
        {
            var detail = new ListConfigurationDetail() { Type = DetailType.List };

            Assert.Equal(_sut.ListEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_UnsupportedDetail()
        {
            var detail = new ConfigurationDetail() {Type = DetailType.Unsupported};

            Assert.Equal(_sut.UnsupportedTypeEditor, _sut.SelectTemplate(detail, null));
        }

        [Theory]
        [InlineData(DetailType.String)]
        [InlineData(DetailType.Integer)]
        [InlineData(DetailType.Boolean)]
        public void SelectTemplate_EnumDetail(DetailType type)
        {
            var detail = new ConfigurationDetail()
            {
                Type = type,
                ValueMappings = new Dictionary<string, string>() {{"256", "256"}}
            };

            Assert.Equal(_sut.EnumEditor, _sut.SelectTemplate(detail, null));
        }

        public static IEnumerable<object[]> ValueMappingData = new List<object[]>
        {
            new object[] {null},
            new object[] {new Dictionary<string, string>()},
        };

        [Theory]
        [MemberData(nameof(ValueMappingData))]
        public void SelectTemplate_ShouldIgnoreEnumEditor(Dictionary<string, string> dict)
        {
            var detail = new ConfigurationDetail()
            {
                Type = DetailType.String,
                ValueMappings = dict
            };

            Assert.Equal(_sut.TextEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_IamRoleHintType()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithType(DetailType.Blob)
                .WithTypeHint(ConfigurationDetail.TypeHints.IamRole)
                .Build();

            Assert.Equal(_sut.IamRoleEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_EcrRepositoryHintType()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithType(DetailType.String)
                .WithTypeHint(ConfigurationDetail.TypeHints.EcrRepository)
                .Build();

            Assert.Equal(_sut.EcrRepositoryEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_VpcHintType()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithType(DetailType.Blob)
                .WithTypeHint(ConfigurationDetail.TypeHints.Vpc)
                .Build();

            Assert.Equal(_sut.VpcEditor, _sut.SelectTemplate(detail, null));
        }

        [Fact]
        public void SelectTemplate_InstanceTypeHintType()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithType(DetailType.String)
                .WithTypeHint(ConfigurationDetail.TypeHints.InstanceType)
                .Build();

            Assert.Equal(_sut.Ec2InstanceTypeEditor, _sut.SelectTemplate(detail, null));
        }
    }
}
