using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.AWSToolkit;
using Amazon.AWSToolkit.PluginServices.Activators;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AWSToolkit.Tests.PluginActivator
{
    public class SamplePluginActivator : IPluginActivator
    {
        public static readonly string SamplePluginName = "SamplePlugin";
        public string PluginName { get; } = SamplePluginName;

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
            Assert.IsNotNull(activators);
            Assert.IsTrue(activators.Count > 0, "No activators were found");
            Assert.IsTrue(activators.Any(activator => activator.PluginName == SamplePluginActivator.SamplePluginName));
        }
    }

    [TestClass]
    public class GetToolkitPluginDirectory
    {
        [TestMethod]
        [DataRow(@"c:\somepath\bin\debug\plugin.dll")]
        [DataRow(@"c:\somepath\bin\release\plugin.dll")]
        [DataRow(@"c:\somepath\out\plugin.dll")]
        public void ReturnsDeploymentPlugins(string inputPath)
        {
            var expectedDirectory = Path.Combine(Path.GetDirectoryName(inputPath), @"..\..\..\..\Deployment\Plugins");

            var pluginDirectory = PluginActivatorUtilities.GetToolkitPluginDirectory(inputPath);
            Assert.AreEqual(expectedDirectory, pluginDirectory);
        }

        [TestMethod]
        public void AppendsPlugins()
        {
            var pluginDirectory = PluginActivatorUtilities.GetToolkitPluginDirectory(@"c:\somepath\plugin.dll");
            Assert.AreEqual(@"c:\somepath\Plugins", pluginDirectory);
        }
    }

    [TestClass]
    public class ParseAdditionalPluginPaths
    {
        [TestMethod]
        public void HandlesEmptyInput()
        {
            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(String.Empty);
            Assert.IsNotNull(paths);
            Assert.AreEqual(0, paths.Count);
        }

        [TestMethod]
        public void HandlesOnePath()
        {
            var somePath = @"c:\somepath1";
            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(somePath);
            Assert.IsNotNull(paths);
            Assert.AreEqual(1, paths.Count);
            Assert.IsTrue(paths.Contains(somePath));
        }

        [TestMethod]
        public void HandlesManyPaths()
        {
            var somePath1 = @"c:\somepath1";
            var somePath2 = @"c:\somepath2";
            var somePath3 = @"c:\somepath3";

            var inputPath = $"{somePath1};{somePath2};{somePath3}";

            var paths = PluginActivatorUtilities.ParseAdditionalPluginPaths(inputPath);
            Assert.IsNotNull(paths);
            Assert.AreEqual(3, paths.Count);
            Assert.IsTrue(paths.Contains(somePath1));
            Assert.IsTrue(paths.Contains(somePath2));
            Assert.IsTrue(paths.Contains(somePath3));
        }
    }

    [TestClass]
    public class GetPluginActivatorsFromFolder : BasePluginActivatorUtilitiesTest
    {
        [TestMethod]
        public async Task MissingFolderReturnsEmpty()
        {
            var activators = await PluginActivatorUtilities.GetPluginActivatorsFromFolder(@"c:\RandomFolder");
            Assert.IsNotNull(activators);
            Assert.AreEqual(0, activators.Count);
        }

        [TestMethod]
        public async Task LoadsPluginsFromFolder()
        {
            var pluginActivators =
                await PluginActivatorUtilities.GetPluginActivatorsFromFolder(Path.GetDirectoryName(AssemblyLocation));
            AssertContainsSamplePluginActivator(pluginActivators);
        }
    }

    [TestClass]
    public class GetPluginActivatorsFromAssembly : BasePluginActivatorUtilitiesTest
    {
        [TestMethod]
        public async Task LoadsPluginActivatorFromPath()
        {
            var pluginActivators =
                await PluginActivatorUtilities.GetPluginActivatorsFromAssembly(this.GetType().Assembly.Location);
            AssertContainsSamplePluginActivator(pluginActivators);
            Assert.AreEqual(1, pluginActivators.Count);
        }

        [TestMethod]
        public void LoadsPluginActivatorFromAssembly()
        {
            var pluginActivators = PluginActivatorUtilities.GetPluginActivatorsFromAssembly(this.GetType().Assembly);
            AssertContainsSamplePluginActivator(pluginActivators);
            Assert.AreEqual(1, pluginActivators.Count);
        }
    }

    [TestClass]
    public class GetPluginActivators : BasePluginActivatorUtilitiesTest
    {
        [TestMethod]
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

    [TestClass]
    public class LoadPluginActivators : BasePluginActivatorUtilitiesTest
    {
        [TestMethod]
        public async Task LoadsAndReturnsPluginActivators()
        {
            var activators =
                await PluginActivatorUtilities.LoadPluginActivators(this.AssemblyLocation, Path.GetDirectoryName(this.AssemblyLocation));
            AssertContainsSamplePluginActivator(activators);
        }
    }
}