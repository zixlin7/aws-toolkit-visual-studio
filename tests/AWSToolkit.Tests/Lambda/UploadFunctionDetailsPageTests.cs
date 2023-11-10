using System;
using System.Linq;
using System.Windows;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.Lambda;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadFunctionDetailsPage_PropertyChanged : IDisposable
    {
        private readonly UploadFunctionDetailsPageFixture _fixture = new UploadFunctionDetailsPageFixture();

        public void Dispose()
        {
            _fixture.Dispose();
        }

        [StaFact]
        public void PackageTypeAffectsCanEditRuntime()
        {
            _fixture.Page.ViewModel.PackageType = PackageType.Zip;
            Assert.True(_fixture.Page.ViewModel.CanEditRuntime);

            _fixture.Page.ViewModel.PackageType = PackageType.Image;
            Assert.False(_fixture.Page.ViewModel.CanEditRuntime);
        }

        [StaFact]
        public void FrameworkAffectsRuntime()
        {
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp10, RuntimeOption.PROVIDED);
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp21, RuntimeOption.PROVIDED);
            AssertFrameworkSetsRuntime(Frameworks.NetCoreApp31, RuntimeOption.PROVIDED);
            AssertFrameworkSetsRuntime(Frameworks.Net50, RuntimeOption.PROVIDED);
            AssertFrameworkSetsRuntime(Frameworks.Net60, RuntimeOption.DotNet6);
        }

        [StaFact]
        public void ArchitectureAffectsRuntime()
        {
            AssertArchitectureAffectsRuntime(LambdaArchitecture.X86, RuntimeOption.PROVIDED);
            AssertArchitectureAffectsRuntime(LambdaArchitecture.Arm, RuntimeOption.PROVIDED_AL2);
        }

        [StaFact]
        public void RuntimeAffectsFramework()
        {
            AssertRuntimeAffectsFramework(RuntimeOption.DotNet6, Frameworks.Net60);
        }

        [StaFact]
        public void RuntimeAffectsConfigFrameworkSettingsVisibility()
        {
            var runtimesToShow = new RuntimeOption[]
            {
                RuntimeOption.DotNet6,
                RuntimeOption.PROVIDED,
                RuntimeOption.PROVIDED_AL2,
                RuntimeOption.PROVIDED_AL2023,
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
        public void DotNetHandlerComponentsAffectHandler_DotNet6()
        {
            _fixture.Page.ViewModel.Runtime = RuntimeOption.DotNet6;

            _fixture.Page.ViewModel.HandlerAssembly = "aaa";
            _fixture.Page.ViewModel.HandlerType = "ttt";
            _fixture.Page.ViewModel.HandlerMethod = "mmm";

            Assert.Equal("aaa::ttt::mmm", _fixture.Page.ViewModel.Handler);
        }

        [StaFact]
        public void HandlerAffectsDotNetHandlerComponents()
        {
            _fixture.Page.ViewModel.Handler = "aaa::ttt::mmm";
            Assert.Equal("aaa", _fixture.Page.ViewModel.HandlerAssembly);
            Assert.Equal("ttt", _fixture.Page.ViewModel.HandlerType);
            Assert.Equal("mmm", _fixture.Page.ViewModel.HandlerMethod);
        }

        private void AssertFrameworkSetsRuntime(string framework, RuntimeOption expectedRuntime)
        {
            _fixture.Page.ViewModel.Framework = framework;
            Assert.Equal(expectedRuntime, _fixture.Page.ViewModel.Runtime);
        }

        private void AssertRuntimeAffectsFramework(RuntimeOption runtime, string expectedFramework)
        {
            _fixture.Page.ViewModel.Runtime = runtime;
            Assert.Equal(expectedFramework, _fixture.Page.ViewModel.Framework);
        }

        private void AssertRuntimeAffectsConfigFrameworkSettingsVisibility(RuntimeOption runtime, bool expectedShow)
        {
            _fixture.Page.ViewModel.Runtime = runtime;
            Assert.Equal(expectedShow ? Visibility.Visible : Visibility.Collapsed,
                _fixture.Page.ViewModel.ConfigurationVisibility);
            Assert.Equal(expectedShow ? Visibility.Visible : Visibility.Collapsed,
                _fixture.Page.ViewModel.FrameworkVisibility);
            Assert.Equal(expectedShow, _fixture.Page.ViewModel.ShowSaveSettings);
        }

        private void AssertArchitectureAffectsRuntime(LambdaArchitecture architecture, RuntimeOption expectedRuntime)
        {
            _fixture.Page.ViewModel.Framework = Frameworks.NetCoreApp10;
            _fixture.Page.ViewModel.Architecture = architecture;
            _fixture.Page.ViewModel.Framework = Frameworks.Net50;
            Assert.Equal(expectedRuntime, _fixture.Page.ViewModel.Runtime);
        }

    }
}
