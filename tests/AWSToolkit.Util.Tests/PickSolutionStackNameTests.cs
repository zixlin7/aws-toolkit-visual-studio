using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class PickSolutionStackNameTests
    {
        [Fact]
        public void OlderThenNewer()
        {
            var stackNames = new string[] {
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "1.2.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames);
            Assert.Equal(DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0", item);
        }

        [Fact]
        public void NewerThenOlder()
        {
            var stackNames = new string[] {
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "1.2.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames);
            Assert.Equal(DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0", item);
        }
        [Fact]
        public void InvalidVersionString()
        {
            var stackNames = new string[] {
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "1.2.0 running IIS 10.0",
                "Foo Stack v3.0.0",
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "3.-.0 running IIS 10.0",
                DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0"
            };

            var item = DeploymentWizardHelper.PickDefaultSolutionStack(stackNames);
            Assert.Equal(DeploymentWizardHelper.DEFALT_SOLUTION_STACK_NAME_PREFIX + "2.0.0 running IIS 10.0", item);
        }
    }
}
