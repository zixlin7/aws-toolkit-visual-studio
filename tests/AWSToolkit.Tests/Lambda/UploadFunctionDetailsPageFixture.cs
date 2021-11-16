using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;
using Amazon.Lambda;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Amazon.AWSToolkit;

namespace AWSToolkit.Tests.Lambda
{
    public class UploadFunctionDetailsPageFixture : IDisposable
    {
        public static readonly IList<RuntimeOption> RuntimeOptions;

        public readonly TemporaryTestLocation TestLocation = new TemporaryTestLocation();
        public readonly Mock<IAWSToolkitShellProvider> ShellProvider = new Mock<IAWSToolkitShellProvider>();
        public readonly ToolkitContextFixture ToolkitContextFixture = new ToolkitContextFixture();
        public readonly UploadFunctionDetailsPage Page;

        static UploadFunctionDetailsPageFixture()
        {
            // Use reflection to grab all of the statically declared RuntimeOption instances
            RuntimeOptions = typeof(RuntimeOption).GetFields(BindingFlags.Static | BindingFlags.Public)
                .Where(f => f.FieldType == typeof(RuntimeOption))
                .Select(f => f.GetValue(null))
                .OfType<RuntimeOption>()
                .ToList();
        }

        public UploadFunctionDetailsPageFixture()
        {
            Page = new UploadFunctionDetailsPage(ShellProvider.Object, ToolkitContextFixture.ToolkitContext);
            Page.ViewModel.Frameworks.Add(Frameworks.NetCoreApp21);
            Page.ViewModel.Frameworks.Add(Frameworks.NetCoreApp31);
        }

        public void SetValidZipDeployValues()
        {
            var viewModel = Page.ViewModel;

            viewModel.PackageType = PackageType.Zip;
            viewModel.Runtime = RuntimeOption.NetCore_v3_1;
            viewModel.Architecture = LambdaArchitecture.X86;

            viewModel.SourceCodeLocation = TestLocation.TestFolder;
            viewModel.FunctionName = "some-lambda-function";
            viewModel.HandlerAssembly = "myAssembly";
            viewModel.HandlerType = "myNamespace.myClass";
            viewModel.HandlerMethod = "myFunction";

            viewModel.Connection.ConnectionIsValid = true;
            viewModel.Connection.IsValidating = false;
        }

        public void SetValidImageDeployValues()
        {
            var dockerfilePath = Path.Combine(TestLocation.TestFolder, "Dockerfile");
            File.WriteAllText(dockerfilePath, "I am a dockerfile");

            var viewModel = Page.ViewModel;

            viewModel.PackageType = PackageType.Image;
            viewModel.Architecture = LambdaArchitecture.X86;

            viewModel.Dockerfile = dockerfilePath;
            viewModel.FunctionName = "some-lambda-function";
            viewModel.ImageRepo = "myrepo";
            viewModel.ImageTag = "latest";

            viewModel.Connection.ConnectionIsValid = true;
            viewModel.Connection.IsValidating = false;
        }

        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }
}
