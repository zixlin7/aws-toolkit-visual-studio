using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Lambda.Model;
using Amazon.AWSToolkit.Lambda.WizardPages.PageUI;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Tests.Common.Context;
using Amazon.AWSToolkit.Tests.Common.IO;

using Moq;

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
            Page.ViewModel.Frameworks.Add(Frameworks.Net60);
            Page.ViewModel.Frameworks.Add(Frameworks.Net80);
        }
        
        public void Dispose()
        {
            TestLocation.Dispose();
        }
    }
}
