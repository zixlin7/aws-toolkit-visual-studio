using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace Amazon.AWSToolkit.PluginServices.Activators
{
    public static class PluginActivatorUtilities
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PluginActivatorUtilities));

        /// <summary>
        /// Finds and loads all of the toolkit's Plugin Activators
        /// </summary>
        /// <param name="toolkitPluginPath">full path to the toolkit assembly file</param>
        /// <param name="additionalPluginPaths">semicolon-delimited list of additional paths to search for plugins</param>
        /// <returns>list of loaded plugin activators</returns>
        public static async Task<IList<IPluginActivator>> LoadPluginActivators(
            string toolkitPluginPath,
            string additionalPluginPaths)
        {
            try
            {
                LOGGER.Debug("Loading Toolkit Plugins");

                var pluginPaths = new List<string>();
                // Search for plugins relative to the toolkit
                pluginPaths.Add(GetToolkitPluginDirectory(toolkitPluginPath));
                // Search for plugins in path(s) provided to the toolkit
                pluginPaths.AddRange(ParseAdditionalPluginPaths(additionalPluginPaths));

                // Find and load plugins
                return await GetPluginActivators(pluginPaths);
            }
            catch (Exception e)
            {
                LOGGER.Error("Error while loading Toolkit Plugins", e);
                return Enumerable.Empty<IPluginActivator>().ToList();
            }
            finally
            {
                LOGGER.Debug("Finished Loading Toolkit Plugins");
            }
        }

        public static string GetToolkitPluginDirectory(string toolkitPluginPath)
        {
            // If the toolkit is currently under development, reference the build output location.
            var deploymentPluginsFolderSuffixes = new List<string>()
            {
                @"bin\debug",
                @"bin\release",
                @"\out"
            };

            string toolkitLocation = Path.GetDirectoryName(toolkitPluginPath);

            if (toolkitLocation == null)
            {
                throw new Exception($"Unable to get directory name for {toolkitPluginPath}");
            }

            if (deploymentPluginsFolderSuffixes.Any(suffix =>
                toolkitLocation.EndsWith(suffix, StringComparison.CurrentCultureIgnoreCase)))
            {
                return Path.Combine(toolkitLocation, @"..\..\..\..\Deployment\Plugins");
            }

            return Path.Combine(toolkitLocation, "Plugins");
        }

        public static IList<string> ParseAdditionalPluginPaths(string additionalPluginPathsSetting)
        {
            var paths = new List<string>();

            if (string.IsNullOrEmpty(additionalPluginPathsSetting)) return paths;

            LOGGER.InfoFormat("Received additional probe paths '{0}'", additionalPluginPathsSetting);
            paths.AddRange(additionalPluginPathsSetting.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            return paths;
        }

        public static async Task<IList<IPluginActivator>> GetPluginActivators(IList<string> folderPaths)
        {
            var loadTasks = folderPaths
                .Select(GetPluginActivatorsFromFolder);

            var activatorLists = await Task.WhenAll(loadTasks);

            return activatorLists
                .SelectMany(plugins => plugins.ToList())
                .ToList();
        }

        public static async Task<IList<IPluginActivator>> GetPluginActivatorsFromFolder(string folderPath)
        {
            LOGGER.Info($"Searching for plugins in '{folderPath}'");

            var pluginActivators = new List<IPluginActivator>();

            if (!Directory.Exists(folderPath))
            {
                LOGGER.WarnFormat("Skipped folder '{0}' - folder does not exist", folderPath);
                return pluginActivators;
            }

            try
            {
                var getActivatorTasks = Directory.GetFiles(folderPath, "*.dll")
                    .Select(GetPluginActivatorsFromAssembly)
                    .ToList();

                var activatorLists = await Task.WhenAll(getActivatorTasks);

                pluginActivators.AddRange(
                    activatorLists.SelectMany(activators => activators.ToList())
                );
            }
            catch (Exception e)
            {
                LOGGER.WarnFormat($"Error while searching {folderPath}. Not all plugins may be loaded.", e);
            }

            return pluginActivators;
        }

        public static async Task<IList<IPluginActivator>> GetPluginActivatorsFromAssembly(string assemblyPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // This is broken.  There are duplicate assemblies in the <extension root> as well as <extension root>\Plugins
                    // folders.  This means the types in each duplicated assembly loaded twice are incompatible.  Need a modern plugin solution.
                    // See IDE-4975 and IDE-6946
                    return GetPluginActivatorsFromAssembly(Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath)));
                }
                catch (Exception e)
                {
                    LOGGER.Debug("Error loading assembly: " + assemblyPath, e);
                    LogTypeLoadException(e);
                }

                return new List<IPluginActivator>();
            });
        }

        public static IList<IPluginActivator> GetPluginActivatorsFromAssembly(Assembly assembly)
        {
            var activators = new List<IPluginActivator>();

            try
            {
                var assemblyName = assembly.GetName();

                assembly
                    .GetTypes()
                    .Where(type => type.GetInterface("IPluginActivator") != null)
                    .Where(type => !type.IsAbstract)
                    .ToList()
                    .ForEach(type =>
                    {
                        if (Activator.CreateInstance(type) is IPluginActivator plugin)
                        {
                            activators.Add(plugin);
                            LOGGER.InfoFormat("Loaded plugin {0} from {1}", plugin.PluginName, assemblyName);
                        }
                    });
            }
            catch (Exception e)
            {
                LOGGER.Debug($"Error loading assembly: {assembly.Location}", e);
                LogTypeLoadException(e);
            }

            return activators;
        }

        private static void LogTypeLoadException(Exception e)
        {
            if (e is ReflectionTypeLoadException typeLoadException)
            {
                var loaderExceptions = typeLoadException.LoaderExceptions;
                foreach (var le in loaderExceptions)
                {
                    LOGGER.Debug("...type load exception: " + le.Message);
                }
            }
        }
    }
}
