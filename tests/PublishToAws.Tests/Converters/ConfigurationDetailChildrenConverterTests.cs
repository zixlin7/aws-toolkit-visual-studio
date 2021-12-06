using System.Collections.Generic;

using Amazon.AWSToolkit.Publish.Converters;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Tests.Publishing.Util;

using Xunit;

namespace Amazon.AWSToolkit.Tests.Publishing.Converters
{
    public class ConfigurationDetailChildrenConverterTests
    {
        private readonly ConfigurationDetailChildrenConverter _sut = new ConfigurationDetailChildrenConverter();

        [Fact]
        public void Convert_ConfigurationDetail()
        {
            var detail = ConfigurationDetailBuilder.Create()
                .WithSampleData()
                .WithChild(ConfigurationDetailBuilder.Create().WithSampleData())
                .WithChild(ConfigurationDetailBuilder.Create().WithSampleData())
                .Build();

            var children = Convert(detail);
            Assert.Equal(detail.Children, children);
        }

        [Fact]
        public void Convert_NonConfigurationDetail()
        {
            var children = Convert("some-other-datatype");
            Assert.Empty(children);
        }

        [Fact]
        public void Convert_IamRoleConfigurationDetail()
        {
            var detail = new IamRoleConfigurationDetail();
            detail.AddChild(ConfigurationDetailBuilder.Create().WithSampleData().Build());
            detail.AddChild(ConfigurationDetailBuilder.Create().WithSampleData().Build());

            var children = Convert(detail);
            Assert.Empty(children);
        }

        public IEnumerable<ConfigurationDetail> Convert(object value)
        {
            return _sut.Convert(value, null, null, null) as IEnumerable<ConfigurationDetail>;
        }
    }
}
