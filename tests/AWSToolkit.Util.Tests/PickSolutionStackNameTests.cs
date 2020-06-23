using System.Linq;
using Xunit;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class PickSolutionStackNameTests
    {
        private readonly string _sampleDefaultPrefix = DeploymentWizardHelper.DefaultSolutionStackPrefixPrecedence.First();
        private readonly string _sampleLowerPriorityDefaultPrefix = DeploymentWizardHelper.DefaultSolutionStackPrefixPrecedence.Skip(1).First();

        [Fact]
        public void OlderThenNewer()
        {
            var stackNames = new string[] {
                _sampleDefaultPrefix + "1.2.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                _sampleDefaultPrefix + "2.0.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(_sampleDefaultPrefix + "2.0.0 running IIS 10.0", item);
        }

        [Fact]
        public void NewerThenOlder()
        {
            var stackNames = new string[] {
                _sampleDefaultPrefix + "2.0.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                _sampleDefaultPrefix + "1.2.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(_sampleDefaultPrefix + "2.0.0 running IIS 10.0", item);
        }

        [Fact]
        public void InvalidVersionString()
        {
            var stackNames = new string[] {
                _sampleDefaultPrefix + "1.2.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                _sampleDefaultPrefix + "3.-.0 running IIS 10.0",
                _sampleDefaultPrefix + "2.0.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(_sampleDefaultPrefix + "2.0.0 running IIS 10.0", item);
        }

        [Fact]
        public void NoDefaultPrefixesFound()
        {
            var stackNames = new string[] {
                "Foo Stack v1.0.0",
                "Foo Stack v2.0.0",
                "Foo Stack v3.0.0",
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(stackNames[0], item);
        }

        [Fact]
        public void PrefixPrecedence()
        {
            var stackNames = new string[] {
                _sampleDefaultPrefix + "2.0.0 running IIS 10.0",
                _sampleDefaultPrefix + "2.5.0 running IIS 10.0",
                _sampleLowerPriorityDefaultPrefix + "3.0.0 running IIS 10.0",
                _sampleLowerPriorityDefaultPrefix + "3.5.0 running IIS 10.0",
                "Foo Stack v4.0.0",
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(stackNames[1], item);
        }

        [Fact]
        public void BestPrefixAvailable()
        {
            var stackNames = new string[] {
                _sampleLowerPriorityDefaultPrefix + "3.0.0 running IIS 10.0",
                _sampleLowerPriorityDefaultPrefix + "3.5.0 running IIS 10.0",
                "Foo Stack v4.0.0",
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames.ToList());
            Assert.Equal(stackNames[1], item);
        }
    }
}
