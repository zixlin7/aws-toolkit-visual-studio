using System.IO;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Templating;
using Xunit;

namespace AWSToolkit.Tests.CommonUI
{
    public class CloudFormationTemplateWrapperTests
    {
        private readonly string _simpleYaml;
        private readonly string _simpleYamlNoParameters;
        private readonly string _shortFormYaml;

        public CloudFormationTemplateWrapperTests()
        {
            _simpleYaml = LoadResource("simple.yaml");
            _simpleYamlNoParameters = LoadResource("simple-no-parameters.yaml");
            _shortFormYaml = LoadResource("short-form.yaml");
        }

        [Fact]
        public void LoadAndParse_SimpleYaml()
        {
            var templateWrapper = CloudFormationTemplateWrapper.FromString(_simpleYaml);
            templateWrapper.LoadAndParse();
            Assert.Equal("Simple example CloudFormation Template in yaml", templateWrapper.TemplateDescription);
            Assert.Null(templateWrapper.TemplateTransformation);

            Assert.NotNull(templateWrapper.Parameters);
            Assert.Equal(3, templateWrapper.Parameters.Count);

            var stage = templateWrapper.Parameters["Stage"];
            Assert.Equal("dev", stage.DefaultValue);
            Assert.Equal("String", stage.Type);
            Assert.Equal("Example parameter 1", stage.Description);
            Assert.Equal(2, stage.AllowedValues.Length);
            Assert.Contains("dev", stage.AllowedValues);
            Assert.Contains("prod", stage.AllowedValues);
            Assert.Equal(3, stage.MinLength);
            Assert.Equal(10, stage.MaxLength);
            Assert.False(stage.NoEcho);

            var owner = templateWrapper.Parameters["Owner"];
            Assert.True(owner.NoEcho);

            var quantity = templateWrapper.Parameters["Quantity"];
            Assert.Equal("5", quantity.DefaultValue);
            Assert.Equal("Number", quantity.Type);
            Assert.Equal("How many things", quantity.Description);
            Assert.Equal(2, quantity.MinValue);
            Assert.Equal(100, quantity.MaxValue);
            Assert.False(quantity.NoEcho);
        }

        [Fact]
        public void LoadAndParse_SimpleYamlNoParameters()
        {
            var templateWrapper = CloudFormationTemplateWrapper.FromString(_simpleYamlNoParameters);
            templateWrapper.LoadAndParse();
            Assert.Equal("Simple example CloudFormation Template in yaml, no Parameters section", templateWrapper.TemplateDescription);
            Assert.Null(templateWrapper.TemplateTransformation);

            Assert.NotNull(templateWrapper.Parameters);
            Assert.Empty(templateWrapper.Parameters);
        }

        [Fact]
        public void LoadAndParse()
        {
            var templateWrapper = CloudFormationTemplateWrapper.FromString(_shortFormYaml);
            templateWrapper.LoadAndParse();
            Assert.Equal("Example CloudFormation template that contains short form notation", templateWrapper.TemplateDescription);
            Assert.Null(templateWrapper.TemplateTransformation);

            Assert.NotNull(templateWrapper.Parameters);
            Assert.Equal(2, templateWrapper.Parameters.Count);

            var stage = templateWrapper.Parameters["Stage"];
            Assert.Equal("dev", stage.DefaultValue);
            Assert.Equal("String", stage.Type);
            Assert.Equal("Example parameter 1", stage.Description);
            Assert.Equal(2, stage.AllowedValues.Length);
            Assert.Contains("dev", stage.AllowedValues);
            Assert.Contains("prod", stage.AllowedValues);
            Assert.Equal(3, stage.MinLength);
            Assert.Equal(10, stage.MaxLength);
            Assert.False(stage.NoEcho);

            var prefix = templateWrapper.Parameters["ResourcePrefix"];
            Assert.Equal("foo", prefix.DefaultValue);
            Assert.Equal("String", prefix.Type);
            Assert.Equal("Prefix applied to created resources", prefix.Description);
            Assert.False(prefix.NoEcho);
        }

        [Fact]
        public void ContainsUserVisibleParameters()
        {
            var templateWrapper = CloudFormationTemplateWrapper.FromString(_simpleYaml);
            templateWrapper.LoadAndParse();
            Assert.True(templateWrapper.ContainsUserVisibleParameters);

            templateWrapper = CloudFormationTemplateWrapper.FromString(_simpleYamlNoParameters);
            templateWrapper.LoadAndParse();
            Assert.False(templateWrapper.ContainsUserVisibleParameters);
        }

        private string LoadResource(string resourceFilename)
        {
            return File.ReadAllText($"./CommonUI/Resources/{resourceFilename}");
        }
    }
}
