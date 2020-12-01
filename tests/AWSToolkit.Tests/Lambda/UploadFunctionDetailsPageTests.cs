using Amazon.AWSToolkit.Lambda.Model;
using Amazon.Lambda;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadFunctionDetailsPageTestsBase : IDisposable
    {
        protected readonly UploadFunctionDetailsPageFixture Fixture = new UploadFunctionDetailsPageFixture();

        public void Dispose()
        {
            Fixture.Dispose();
        }
    }

    public class UploadFunctionDetailsPage_AllRequiredFieldsAreSet : UploadFunctionDetailsPageTestsBase
    {
        [StaFact]
        public void ValidImageDeploy()
        {
            Fixture.SetValidImageDeployValues();
            Assert.True(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaFact]
        public void ValidZipDeploy()
        {
            Fixture.SetValidZipDeployValues();
            Assert.True(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaFact]
        public void SourceCodeLocation_NonExistent()
        {
            Fixture.SetValidZipDeployValues();

            Fixture.Page.ViewModel.SourceCodeLocation =
                Path.Combine(Fixture.TestLocation.TestFolder, "bad-folder");
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);

            Fixture.Page.ViewModel.SourceCodeLocation =
                Path.Combine(Fixture.TestLocation.TestFolder, "bad-file.zip");
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaFact]
        public void FunctionName_Empty()
        {
            Fixture.SetValidZipDeployValues();

            Fixture.Page.ViewModel.FunctionName = null;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);

            Fixture.Page.ViewModel.FunctionName = string.Empty;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaFact]
        public void Runtime_Null()
        {
            Fixture.SetValidZipDeployValues();

            Fixture.Page.ViewModel.Runtime = null;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaTheory]
        [InlineData(null, "x", "x")]
        [InlineData("", "x", "x")]
        [InlineData("x", null, "x")]
        [InlineData("x", "", "x")]
        [InlineData("x", "x", null)]
        [InlineData("x", "x", "")]
        public void HandlerComponents_Empty(string assembly, string type, string method)
        {
            Fixture.SetValidZipDeployValues();

            Fixture.Page.ViewModel.HandlerAssembly = assembly;
            Fixture.Page.ViewModel.HandlerType = type;
            Fixture.Page.ViewModel.HandlerMethod = method;

            Fixture.Page.ViewModel.Runtime = RuntimeOption.NetCore_v2_1;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);

            Fixture.Page.ViewModel.Runtime = RuntimeOption.PROVIDED;
            Assert.True(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaTheory]
        [InlineData(null)]
        [InlineData("")]
        public void Handler_Empty(string handler)
        {
            Fixture.SetValidZipDeployValues();
            Fixture.Page.ViewModel.Handler = handler;

            // Handler can not be empty for non-Custom runtimes
            Fixture.Page.ViewModel.Runtime = RuntimeOption.NetCore_v2_1;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);

            // Handler can be empty for Custom runtimes
            Fixture.Page.ViewModel.Runtime = RuntimeOption.PROVIDED;
            Assert.True(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaTheory]
        [InlineData(null)]
        [InlineData("")]
        public void Dockerfile_Empty(string dockerfilePath)
        {
            Fixture.SetValidImageDeployValues();
            Fixture.Page.ViewModel.Dockerfile = dockerfilePath;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaFact]
        public void Dockerfile_NonExistent()
        {
            Fixture.SetValidImageDeployValues();
            Fixture.Page.ViewModel.Dockerfile = Path.Combine(Fixture.TestLocation.TestFolder, "bad-file");
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaTheory]
        [InlineData(null)]
        [InlineData("")]
        public void ImageRepo_Empty(string imageRepo)
        {
            Fixture.SetValidImageDeployValues();
            Fixture.Page.ViewModel.ImageRepo = imageRepo;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }

        [StaTheory]
        [InlineData(null)]
        [InlineData("")]
        public void ImageTag_Empty(string imageTag)
        {
            Fixture.SetValidImageDeployValues();
            Fixture.Page.ViewModel.ImageTag = imageTag;
            Assert.False(Fixture.Page.AllRequiredFieldsAreSet);
        }
    }

    public class UploadFunctionDetailsPage_PropertyChanged : UploadFunctionDetailsPageTestsBase
    {
        [StaFact]
        public void PackageTypeAffectsCanEditRuntime()
        {
            Fixture.Page.ViewModel.PackageType = PackageType.Zip;
            Assert.True(Fixture.Page.ViewModel.CanEditRuntime);

            Fixture.Page.ViewModel.PackageType = PackageType.Image;
            Assert.False(Fixture.Page.ViewModel.CanEditRuntime);
        }

        [StaFact]
        public void FrameworkAffectsRuntime()
        {
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp10, RuntimeOption.PROVIDED);
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp21, RuntimeOption.NetCore_v2_1);
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp31, RuntimeOption.NetCore_v3_1);
        }

        [StaFact]
        public void RuntimeAffectsFramework()
        {
            AssertRuntimeAffectsFramework(RuntimeOption.NetCore_v2_1, Frameworks.NetCoreApp21);
            AssertRuntimeAffectsFramework(RuntimeOption.NetCore_v3_1, Frameworks.NetCoreApp31);
        }

        /// <summary>
        /// Either the DotNet Handler or the normal Handler are shown depending on Runtime.
        /// DotNet handler has the Assembly/Type/Method components.
        /// </summary>
        [StaFact]
        public void RuntimeAffectsHandlerVisibility()
        {
            var runtimesShowingDotNetHandler = new RuntimeOption[]
            {
                RuntimeOption.NetCore_v2_1,
                RuntimeOption.NetCore_v3_1,
            };

            UploadFunctionDetailsPageFixture.RuntimeOptions
                .ToList()
                .ForEach(runtime =>
                {
                    AssertRuntimeAffectsHandlerVisibility(runtime, runtimesShowingDotNetHandler.Contains(runtime));
                });
        }

        [StaFact]
        public void RuntimeAffectsConfigFrameworkSettingsVisibility()
        {
            var runtimesToShow = new RuntimeOption[]
            {
                RuntimeOption.NetCore_v2_1,
                RuntimeOption.NetCore_v3_1,
                RuntimeOption.PROVIDED,
            };

            UploadFunctionDetailsPageFixture.RuntimeOptions
                .ToList()
                .ForEach(runtime =>
                {
                    AssertRuntimeAffectsConfigFrameworkSettingsVisibility(runtime,
                        runtimesToShow.Contains(runtime));
                });
        }

        [StaFact]
        public void DotNetHandlerComponentsAffectHandler()
        {
            Fixture.Page.ViewModel.Runtime = RuntimeOption.NetCore_v2_1;

            Fixture.Page.ViewModel.HandlerAssembly = "aaa";
            Fixture.Page.ViewModel.HandlerType = "ttt";
            Fixture.Page.ViewModel.HandlerMethod = "mmm";

            Assert.Equal("aaa::ttt::mmm", Fixture.Page.ViewModel.Handler);
        }

        [StaFact]
        public void HandlerAffectsDotNetHandlerComponents()
        {
            Fixture.Page.ViewModel.Handler = "aaa::ttt::mmm";
            Assert.Equal("aaa", Fixture.Page.ViewModel.HandlerAssembly);
            Assert.Equal("ttt", Fixture.Page.ViewModel.HandlerType);
            Assert.Equal("mmm", Fixture.Page.ViewModel.HandlerMethod);
        }

        private void AssertFrameworkSetsRuntime(string framework, RuntimeOption expectedRuntime)
        {
            Fixture.Page.ViewModel.Framework = framework;
            Assert.Equal(expectedRuntime, Fixture.Page.ViewModel.Runtime);
        }

        private void AssertRuntimeAffectsFramework(RuntimeOption runtime, string expectedFramework)
        {
            Fixture.Page.ViewModel.Runtime = runtime;
            Assert.Equal(expectedFramework, Fixture.Page.ViewModel.Framework);
        }

        private void AssertRuntimeAffectsHandlerVisibility(RuntimeOption runtime, bool expectedDotNetHandlerVisibility)
        {
            Fixture.Page.ViewModel.Runtime = runtime;
            Assert.Equal(expectedDotNetHandlerVisibility ? Visibility.Visible : Visibility.Collapsed,
                Fixture.Page.ViewModel.DotNetHandlerVisibility);
            Assert.Equal(!expectedDotNetHandlerVisibility ? Visibility.Visible : Visibility.Collapsed,
                Fixture.Page.ViewModel.HandlerVisibility);
        }

        private void AssertRuntimeAffectsConfigFrameworkSettingsVisibility(RuntimeOption runtime, bool expectedShow)
        {
            Fixture.Page.ViewModel.Runtime = runtime;
            Assert.Equal(expectedShow ? Visibility.Visible : Visibility.Collapsed,
                Fixture.Page.ViewModel.ConfigurationVisibility);
            Assert.Equal(expectedShow ? Visibility.Visible : Visibility.Collapsed,
                Fixture.Page.ViewModel.FrameworkVisibility);
            Assert.Equal(expectedShow, Fixture.Page.ViewModel.ShowSaveSettings);
        }
    }
}