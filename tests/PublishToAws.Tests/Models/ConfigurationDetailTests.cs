using System.Collections;
using System.Collections.Generic;
using System.Linq;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Models
{
    public class ConfigurationDetailTests
    {
        private readonly ConfigurationDetail _sut = new ConfigurationDetail();

        public static IEnumerable<object[]> NoValueMappingData = new List<object[]>
        {
            new object[] { null },
            new object[] { new Dictionary<string, string>() },
        };

        [Fact]
        public void HasErrors()
        {
            _sut.ValidationMessage = "validation-message";
            Assert.True(_sut.HasErrors);
        }

        [Fact]
        public void HasErrors_NoValidationMessage()
        {
            _sut.ValidationMessage = string.Empty;
            Assert.False(_sut.HasErrors);
        }

        [Fact]
        public void GetErrors()
        {
            _sut.ValidationMessage = "validation-message";

            var errors = _sut.GetErrors(nameof(_sut.Value));
            Assert.Single(errors, _sut.ValidationMessage);
        }

        [Fact]
        public void GetErrors_NoValidationMessage()
        {
            _sut.ValidationMessage = string.Empty;

            Assert.Empty(_sut.GetErrors(nameof(_sut.Value)));
        }

        [Fact]
        public void GetErrors_NonValueProperty()
        {
            _sut.ValidationMessage = "validation-message";

            Assert.Empty(_sut.GetErrors(nameof(_sut.DefaultValue)));
        }

        [Fact]
        public void GetLeafId_NoParent()
        {
            _sut.Id = "child-id";

            Assert.Equal("child-id", _sut.GetLeafId());
        }

        [Fact]
        public void GetLeafId_WithParent()
        {
            _sut.Id = "child-id";
            _sut.Parent = new ConfigurationDetail {
                Id = "parent-id"
            };

            Assert.Equal("parent-id.child-id", _sut.GetLeafId());
        }

        [Fact]
        public void FullDisplayName_NoParent()
        {
            _sut.Name = "some-setting";

            Assert.Equal("some-setting", _sut.FullDisplayName);
        }

        [Fact]
        public void FullDisplayName_WithParent()
        {
            var detailTree = ConfigurationDetailBuilder.Create()
                .WithName("Load Balancers")
                .WithChild(ConfigurationDetailBuilder.Create().WithName("AutoScaling")
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("Capacity"))
                )
                .Build();

            
            var detail = detailTree.Children.First().Children.First();

            Assert.Equal("Load Balancers : AutoScaling : Capacity", detail.FullDisplayName);
        }

        [Fact]
        public void ShouldBeLeaf()
        {
            var leaf = ConfigurationDetailBuilder.Create()
                .WithSampleData()
                .Build();

            Assert.True(leaf.IsLeaf());
        }

        [Fact]
        public void ShouldNotBeLeaf()
        {
            var parent = ConfigurationDetailBuilder.Create()
                .WithSampleData()
                .WithChild(ConfigurationDetailBuilder.Create().WithSampleData())
                .Build();

            Assert.False(parent.IsLeaf());
        }

        [Fact]
        public void HasValueMappings()
        {
            _sut.ValueMappings = new Dictionary<string, string>()
            {
                {"key1", "key2"}
            };
            Assert.True(_sut.HasValueMappings());
        }

        [Theory]
        [MemberData(nameof(NoValueMappingData))]
        public void HasNoValueMappings(Dictionary<string, string> valueMapping)
        {
            _sut.ValueMappings = valueMapping;
            Assert.False(_sut.HasValueMappings());
        }

        [Fact]
        public void GetSelfAndDescendants_NoChildren()
        {
            var detail = new ConfigurationDetail();

            var details = detail.GetSelfAndDescendants().ToList();

            Assert.Single((IEnumerable) details);
            Assert.Contains(detail, details);
        }

        [Fact]
        public void GetSelfAndDescendants_WithChildren()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithSampleData()
                .WithChild(ConfigurationDetailBuilder.Create().WithSampleData())
                .WithChild(ConfigurationDetailBuilder.Create().WithSampleData())
                .Build();

            var details = detail.GetSelfAndDescendants().ToList();

            Assert.Equal(3, details.Count);
            Assert.Contains(detail, details);
            Assert.Contains(detail.Children[0], details);
            Assert.Contains(detail.Children[1], details);
        }

        [Fact]
        public void GetSelfAndDescendants_WithNestedChildren()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithName("root")
                .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 1")
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 1.a"))
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 1.b"))
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 1.b.i"))
                )
                .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 2"))
                .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 3")
                    .WithChild(ConfigurationDetailBuilder.Create().WithName("Child 3a"))
                )
                .Build();

            var details = detail.GetSelfAndDescendants().ToList();

            Assert.Equal(8, details.Count);
        }
    }
}
