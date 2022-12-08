using Amazon.AWSToolkit.ElasticBeanstalk.Models;
using Amazon.AWSToolkit.ElasticBeanstalk.Utils;
using Amazon.ElasticBeanstalk.Model;

using Xunit;

namespace AWSToolkit.Tests.ElasticBeanstalk
{
    public class ModelExtensionMethodsTests
    {
        private readonly EnvironmentDescription _sampleEnvironmentDescription;

        public ModelExtensionMethodsTests()
        {
            _sampleEnvironmentDescription = new EnvironmentDescription()
            {
                EnvironmentId = "sample-id",
                EnvironmentName = "env-name",
                ApplicationName = "app-name",
                CNAME = "cname",
            };
        }

        [Fact]
        public void AsBeanstalkEnvironmentModel()
        {
            var expectedResult = new BeanstalkEnvironmentModel(
                id: _sampleEnvironmentDescription.EnvironmentId,
                name: _sampleEnvironmentDescription.EnvironmentName,
                applicationName: _sampleEnvironmentDescription.ApplicationName,
                cname: _sampleEnvironmentDescription.CNAME);

            Assert.Equal(expectedResult, _sampleEnvironmentDescription.AsBeanstalkEnvironmentModel());
        }
    }
}
