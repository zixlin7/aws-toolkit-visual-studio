using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Activators;

using Xunit;

[assembly: PluginActivatorType(typeof(AWSToolkit.Tests.PluginActivator.SamplePluginActivator))]

namespace AWSToolkit.Tests.PluginActivator
{
    public class SamplePluginActivator : IPluginActivator
    {
        public static readonly string SamplePluginName = "SamplePlugin";
        public string PluginName { get; } = SamplePluginName;

        public void Initialize(ToolkitContext toolkitContext)
        {

        }

        public void RegisterMetaNodes()
        {
        }

        public object QueryPluginService(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

    public class BasePluginActivatorUtilitiesTest
    {
        protected Dictionary<string, IPluginActivator> PluginActivators = new Dictionary<string, IPluginActivator>();
        protected readonly string AssemblyLocation;

        public BasePluginActivatorUtilitiesTest()
        {
            this.AssemblyLocation = this.GetType().Assembly.Location;
        }

        protected static void AssertContainsSamplePluginActivator(IList<IPluginActivator> activators)
        {
            Assert.NotNull(activators);
            Assert.True(activators.Count > 0, "No activators were found");
            Assert.Contains(activators, activator => activator.PluginName == SamplePluginActivator.SamplePluginName);
        }
    }

    public class GetToolkitPluginDirectory
    {
        [Theory]
        [InlineData(@"c:\somepath\bin\debug\plugin.dll")]
        [InlineData(@"c:\somepath\bin\release\plugin.dll")]
        [InlineData(@"c:\somepath\out\plugin.dll")]
        public void ReturnsDeploymentPlugins(string inputPath)
        {
            var expectedDirectory = Path.Combine(Path.GetDirectoryName(inputPath), @"..\..\..\..\Deployment\Plugins");

            var pluginDirectory = PluginActivatorUtilities.GetToolkitPluginDirectory(inputPath);
            Assert.Equal(expectedDirectory, pluginDirectory);
        }

        [Fact]
        public void AppendsPlugins()
        {
            var pluginDirectory = PluginActivatorUtilities.GetToolkitPluginDirectory(@"c:\somepath\plugin.dll");
            Assert.Equal(@"c:\somepath\Plugins", pluginDirectory);
        }
    }

    public class ParseAdditionalPluginPaths
    {
        [Fact]
        public void HandlesEmptyInput()
        {
            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(String.Empty);
            Assert.NotNull(paths);
            Assert.Equal(0, paths.Count);
        }

        [Fact]
        public void HandlesOnePath()
        {
            var somePath = @"c:\somepath1";
            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(somePath);
            Assert.NotNull(paths);
            Assert.Equal(1, paths.Count);
            Assert.True(paths.Contains(somePath));
        }

        [Fact]
        public void HandlesManyPaths()
        {
            var somePath1 = @"c:\somepath1";
            var somePath2 = @"c:\somepath2";
            var somePath3 = @"c:\somepath3";

            var inputPath = $"{somePath1};{somePath2};{somePath3}";

            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(inputPath);
            Assert.NotNull(paths);
            Assert.Equal(3, paths.Count);
            Assert.True(paths.Contains(somePath1));
            Assert.True(paths.Contains(somePath2));
            Assert.True(paths.Contains(somePath3));
        }
    }

    public class GetPluginActivatorsFromFolder : BasePluginActivatorUtilitiesTest
    {
        [Fact]
        public async Task MissingFolderReturnsEmpty()
        {
            var activators = await PluginActivatorUtilities.GetPluginActivatorsFromFolder(@"c:\RandomFolder");
            Assert.NotNull(activators);
            Assert.Equal(0, activators.Count);
        }

        [Fact]
        public async Task LoadsPluginsFromFolder()
        {
            var pluginActivators =
                await PluginActivatorUtilities.GetPluginActivatorsFromFolder(Path.GetDirectoryName(AssemblyLocation));
            AssertContainsSamplePluginActivator(pluginActivators);
        }
    }

    public class GetPluginActivatorsFromAssembly : BasePluginActivatorUtilitiesTest
    {
        [Fact]
        public async Task LoadsPluginActivatorFromPath()
        {
            var pluginActivators =
                await PluginActivatorUtilities.GetPluginActivatorsFromAssembly(this.GetType().Assembly.Location);
            AssertContainsSamplePluginActivator(pluginActivators);
            Assert.Equal(1, pluginActivators.Count);
        }

        [Fact]
        public void LoadsPluginActivatorFromAssembly()
        {
            var pluginActivators = PluginActivatorUtilities.GetPluginActivatorsFromAssembly(this.GetType().Assembly);
            AssertContainsSamplePluginActivator(pluginActivators);
            Assert.Equal(1, pluginActivators.Count);
        }
    }

    public class GetPluginActivators : BasePluginActivatorUtilitiesTest
    {
        [Fact]
        public async Task ReturnsActivators()
        {
            var paths = new List<string>()
            {
                Path.GetDirectoryName(this.AssemblyLocation)
            };

            var activators = await PluginActivatorUtilities.GetPluginActivators(paths);
            AssertContainsSamplePluginActivator(activators);
        }
    }

    public class LoadPluginActivators : BasePluginActivatorUtilitiesTest
    {
        [Fact]
        public async Task LoadsAndReturnsPluginActivators()
        {
            var activators =
                await PluginActivatorUtilities.LoadPluginActivators(this.AssemblyLocation, Path.GetDirectoryName(this.AssemblyLocation));
            AssertContainsSamplePluginActivator(activators);
        }
    }
}
