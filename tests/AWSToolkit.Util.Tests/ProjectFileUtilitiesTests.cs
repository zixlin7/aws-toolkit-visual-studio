using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class ProjectFileUtilitiesTests
    {
        [Fact]
        public void IsLambdaProjectWithOneProjectType()
        {
            Assert.True(ProjectFileUtilities.IsProjectType("<Project><PropertyGroup><AWSProjectType>Lambda</AWSProjectType></PropertyGroup></Project>", ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID));
        }

        [Fact]
        public void IsLambdaProjectWithMultipleProjectTypes()
        {
            Assert.True(ProjectFileUtilities.IsProjectType("<Project><PropertyGroup><AWSProjectType>Feature1;Lambda;Feature2</AWSProjectType></PropertyGroup></Project>", ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID));
        }

        [Fact]
        public void IsNotLambdaProjectButHasAWSProjectType()
        {
            Assert.False(ProjectFileUtilities.IsProjectType("<Project><PropertyGroup><AWSProjectType>Feature1</AWSProjectType></PropertyGroup></Project>", ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID));
        }

        [Fact]
        public void IsNotLambdaProjectNoAWSProjectType()
        {
            Assert.False(ProjectFileUtilities.IsProjectType("<Project><PropertyGroup></PropertyGroup></Project>", ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID));
        }
    }
}
