using System;
using System.Collections.Generic;
using System.IO;

using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.ViewModel;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Lambda;

using Moq;

using Xunit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadFunctionViewModelTestFixture : IDisposable
    {
        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        public readonly string SampleDockerfilePath;

        public readonly Mock<IAWSToolkitShellProvider> ShellProvider = new Mock<IAWSToolkitShellProvider>();
        public readonly ToolkitContextFixture ToolkitContextFixture = new ToolkitContextFixture();

        public UploadFunctionViewModelTestFixture()
        {
            SampleDockerfilePath = Path.Combine(TestLocation.TestFolder, "Dockerfile");
            File.WriteAllText(SampleDockerfilePath, "I am a dockerfile");
        }

        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }

    public class UploadFunctionViewModel_IsValidConfigurationTests : IClassFixture<UploadFunctionViewModelTestFixture>
    {
        private readonly UploadFunctionViewModelTestFixture _fixture;
        public readonly UploadFunctionViewModel _sut;


        public UploadFunctionViewModel_IsValidConfigurationTests(UploadFunctionViewModelTestFixture fixture)
        {
            _fixture = fixture;
            _sut = new UploadFunctionViewModel(_fixture.ShellProvider.Object, _fixture.ToolkitContextFixture.ToolkitContext)
            {
                PackageType = Amazon.Lambda.PackageType.Zip
            };
        }

        [Fact]
        public void ValidImageDeploy()
        {
            SetValidImageDeployValues(_sut);
            Assert.True(_sut.IsValidConfiguration());
        }

        [Fact]
        public void ValidZipDeploy()
        {
            SetValidZipDeployValues(_sut);

            Assert.True(_sut.IsValidConfiguration());
        }

        [Fact]
        public void ValidZipDeploy_ExecutableProject()
        {
            SetValidZipDeployValues(_sut);
            ClearHandlerAndComponents(_sut);
            _sut.Runtime = RuntimeOption.DotNet6;
            _sut.Handler = "some-assembly";
            _sut.ProjectIsExecutable = true;

            Assert.True(_sut.IsValidConfiguration());
        }

        [Fact]
        public void SourceCodeLocation_NonExistent()
        {
            SetValidZipDeployValues(_sut);

            _sut.SourceCodeLocation =
                Path.Combine(_fixture.TestLocation.TestFolder, "bad-folder");
            Assert.False(_sut.IsValidConfiguration());

            _sut.SourceCodeLocation =
                Path.Combine(_fixture.TestLocation.TestFolder, "bad-file.zip");
            Assert.False(_sut.IsValidConfiguration());
        }

        [Fact]
        public void FunctionName_Empty()
        {
            SetValidZipDeployValues(_sut);

            _sut.FunctionName = null;
            Assert.False(_sut.IsValidConfiguration());

            _sut.FunctionName = string.Empty;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Fact]
        public void Runtime_Null()
        {
            SetValidZipDeployValues(_sut);

            _sut.Runtime = null;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Fact]
        public void Image_Architecture_Null()
        {
            SetValidImageDeployValues(_sut);

            _sut.Architecture = null;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Fact]
        public void Zip_Architecture_Null()
        {
            SetValidZipDeployValues(_sut);

            _sut.Architecture = null;
            Assert.False(_sut.IsValidConfiguration());
        }

        public static IEnumerable<object[]> EmptyHandlerComponentVariations = new []
        {
            new object[] {null, "x", "x"},
            new object[] {"", "x", "x"},
            new object[] {"x", null, "x"},
            new object[] {"x", "", "x"},
            new object[] {"x", "x", null},
            new object[] {"x", "x", "" },
            new object[] {"", "", "" },
            new object[] {null, null, null },
        };

        [Theory]
        [MemberData(nameof(EmptyHandlerComponentVariations))]
        public void EmptyHandlerComponents_Required(string assembly, string type, string method)
        {
            SetValidZipDeployValues(_sut);

            _sut.HandlerAssembly = assembly;
            _sut.HandlerType = type;
            _sut.HandlerMethod = method;
            _sut.Handler = "some-handler";

            _sut.Runtime = RuntimeOption.DotNet6;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Theory]
        [MemberData(nameof(EmptyHandlerComponentVariations))]
        public void EmptyHandlerComponents_NotRequired(string assembly, string type, string method)
        {
            SetValidZipDeployValues(_sut);

            _sut.HandlerAssembly = assembly;
            _sut.HandlerType = type;
            _sut.HandlerMethod = method;
            _sut.Handler = "some-handler";

            _sut.ProjectIsExecutable = true;

            _sut.Runtime = RuntimeOption.DotNet6;
            Assert.True(_sut.IsValidConfiguration());

            _sut.Runtime = RuntimeOption.PROVIDED_AL2;
            Assert.True(_sut.IsValidConfiguration());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Handler_Empty(string handler)
        {
            SetValidZipDeployValues(_sut);
            _sut.Handler = handler;

            // Handler can not be empty for non-Custom runtimes
            _sut.Runtime = RuntimeOption.DotNet6;
            Assert.False(_sut.IsValidConfiguration());

            // Handler can be empty for Custom runtimes
            _sut.Runtime = RuntimeOption.PROVIDED;
            Assert.True(_sut.IsValidConfiguration());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void Dockerfile_Empty(string dockerfilePath)
        {
            SetValidImageDeployValues(_sut);
            _sut.Dockerfile = dockerfilePath;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Fact]
        public void Dockerfile_NonExistent()
        {
            SetValidImageDeployValues(_sut);
            _sut.Dockerfile = Path.Combine(_fixture.TestLocation.TestFolder, "bad-file");
            Assert.False(_sut.IsValidConfiguration());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ImageRepo_Empty(string imageRepo)
        {
            SetValidImageDeployValues(_sut);
            _sut.ImageRepo = imageRepo;
            Assert.False(_sut.IsValidConfiguration());
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        public void ImageTag_Empty(string imageTag)
        {
            SetValidImageDeployValues(_sut);
            _sut.ImageTag = imageTag;
            Assert.False(_sut.IsValidConfiguration());
        }

        private void SetValidZipDeployValues(UploadFunctionViewModel viewModel)
        {
            viewModel.PackageType = PackageType.Zip;
            viewModel.Runtime = RuntimeOption.DotNet6;
            viewModel.Architecture = LambdaArchitecture.X86;

            viewModel.SourceCodeLocation = _fixture.TestLocation.TestFolder;
            viewModel.FunctionName = "some-lambda-function";
            viewModel.HandlerAssembly = "myAssembly";
            viewModel.HandlerType = "myNamespace.myClass";
            viewModel.HandlerMethod = "myFunction";
            viewModel.Handler = viewModel.CreateDotNetHandler();

            viewModel.Connection.ConnectionIsValid = true;
            viewModel.Connection.IsValidating = false;
        }

        private void SetValidImageDeployValues(UploadFunctionViewModel viewModel)
        {
            viewModel.PackageType = PackageType.Image;
            viewModel.Architecture = LambdaArchitecture.X86;

            viewModel.Dockerfile = _fixture.SampleDockerfilePath;
            viewModel.FunctionName = "some-lambda-function";
            viewModel.ImageRepo = "myrepo";
            viewModel.ImageTag = "latest";

            viewModel.Connection.ConnectionIsValid = true;
            viewModel.Connection.IsValidating = false;
        }

        private static void ClearHandlerAndComponents(UploadFunctionViewModel viewModel)
        {
            viewModel.HandlerAssembly = string.Empty;
            viewModel.HandlerType = string.Empty;
            viewModel.HandlerMethod = string.Empty;
            viewModel.Handler = viewModel.CreateDotNetHandler();
        }
    }
}
