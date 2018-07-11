using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
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


        [Fact]
        public void AddLambdaProjectType()
        {
            var updatedContent = ProjectFileUtilities.SetAWSProjectType("<Project>\n<PropertyGroup></PropertyGroup></Project>", ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID);

            var xmlDoc = XDocument.Parse(updatedContent);
            var projectTypeElement = xmlDoc.XPathSelectElement("//PropertyGroup/AWSProjectType");

            Assert.NotNull(projectTypeElement);
            Assert.Equal(ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID, projectTypeElement.Value);

            // Make sure the project wasn't formatted as a single lone of text
            Assert.Contains("\n", updatedContent);
        }

        [Fact]
        public void ProjectWithLambdaTypeDoesntChange()
        {
            var originalContent = "<Project><PropertyGroup><AWSProjectType>Lambda</AWSProjectType></PropertyGroup></Project>";
            var updatedContent = ProjectFileUtilities.SetAWSProjectType(originalContent, ProjectFileUtilities.LAMBDA_PROJECT_TYPE_ID);

            Assert.Equal(originalContent, updatedContent);
        }
    }
}
