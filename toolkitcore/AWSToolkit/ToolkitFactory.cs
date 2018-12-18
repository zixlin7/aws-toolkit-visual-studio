using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Globalization;

using Amazon.Util;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Shared;

using log4net;

namespace Amazon.AWSToolkit
{
    public class ToolkitFactory
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ToolkitFactory));

        static ToolkitFactory INSTANCE;

        readonly NavigatorControl _navigator;
        readonly IAWSToolkitShellProvider _shellProvider;

        AWSViewMetaNode _rootViewMetaNode;
        AWSViewModel _rootViewModel;

        Dictionary<string, IPluginActivator> _pluginActivators;

        private ToolkitFactory(NavigatorControl navigator, IAWSToolkitShellProvider shellProvider)
        {
            this._navigator = navigator;
            this._shellProvider = shellProvider;

            if (ServicePointManager.DefaultConnectionLimit < 100)
            {
                ServicePointManager.DefaultConnectionLimit = 100;
            }
        }

        public static void InitializeToolkit(
            NavigatorControl navigator,
            IAWSToolkitShellProvider shellProvider,
            string additionalPluginPaths,
            Action initializeCompleteCallback
        )
        {
            if (INSTANCE != null)
                throw new ApplicationException("Toolkit has already been initialized");

            Amazon.Util.Internal.InternalSDKUtils.SetUserAgent(shellProvider.ShellName, Constants.VERSION_NUMBER);
            ProxyUtilities.ApplyCurrentProxySettings();

            INSTANCE = new ToolkitFactory(navigator, shellProvider);

            Task.Run(() =>
            {

                INSTANCE.loadPluginActivators(additionalPluginPaths);
                INSTANCE.registerMetaNodes();


                INSTANCE.ShellProvider.ShellDispatcher.BeginInvoke((Action) (() =>
                {
                    try
                    {
                        INSTANCE._rootViewModel = new AWSViewModel(
                            INSTANCE._shellProvider.ShellDispatcher,
                            INSTANCE._rootViewMetaNode);
                        INSTANCE._navigator.Initialize(INSTANCE._rootViewModel);

                        if (initializeCompleteCallback != null)
                        {
                            initializeCompleteCallback();
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.Fatal("Unhandled exception during AWS Toolkit startup", e);
                        ToolkitFactory.Instance.ShellProvider.ShowError(
                            string.Format(
                                "Unexpected error during initialization of AWS Toolkit. Toolkit is likely to be unstable until Visual Studio is restarted.{0}{0}{1}",
                                Environment.NewLine,
                                e.Message
                            )
                        );
                    }
                }));
            });

            if (Application.Current != null)
                Application.Current.DispatcherUnhandledException += onDispatcherUnhandledException;
        }

        static void onDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LOGGER.Fatal("Unhandled exception", e.Exception);
        }

        public static ToolkitFactory Instance
        {
            get { return INSTANCE; }
        }

        public NavigatorControl Navigator
        {
            get { return this._navigator; }
        }

        public IAWSToolkitShellProvider ShellProvider
        {
            get { return this._shellProvider; }
        }

        public AWSViewMetaNode RootViewMetaNode
        {
            get { return this._rootViewMetaNode; }
        }

        public AWSViewModel RootViewModel
        {
            get { return this._rootViewModel; }
        }

        public object QueryPluginService(Type serviceType)
        {
            foreach (IPluginActivator pa in this._pluginActivators.Values)
            {
                object svc = pa.QueryPluginService(serviceType);
                if (svc != null)
                    return svc;
            }

            return null;
        }

        /// <summary>
        /// Returns the set of plugins that implement service interface T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> QueryPluginServiceImplementors<T>()
        {
            var implementors = new List<T>();
            foreach (IPluginActivator pa in this._pluginActivators.Values)
            {
                try
                {
                    var svc = (T)pa.QueryPluginService(typeof(T));
                    if (svc != null)
                        implementors.Add(svc);
                }
                catch (NullReferenceException) { }
            }

            return implementors;
        }

        /// <summary>
        /// Checks for existence of registered accounts and if none and the dialog has not
        /// been run previously, presents a 'welcome and setup' dialog in the host shell to 
        /// get them started
        /// </summary>
        public void RunFirstTimeSetup()
        {
            if (INSTANCE == null)
            {
                LOGGER.ErrorFormat("RunFirstTimeSetup called before toolkit initialized");
                return;
            }

            // TBD
        }

        void loadPluginActivators(string additionalPluginPaths)
        {
            this._pluginActivators = new Dictionary<string, IPluginActivator>();

            // by default infer a plugin location based on the running assembly, then consider provider-specific 
            // search paths
            string pluginDirectory;
            string toolkitLocation = Path.GetDirectoryName(this.GetType().Assembly.Location);
            if (toolkitLocation.EndsWith(@"bin\debug", StringComparison.CurrentCultureIgnoreCase) 
                    || toolkitLocation.EndsWith(@"bin\release", StringComparison.CurrentCultureIgnoreCase) 
                    || toolkitLocation.EndsWith(@"\out", StringComparison.CurrentCultureIgnoreCase))
                pluginDirectory = Path.Combine(toolkitLocation, @"..\..\..\..\Deployment\Plugins");
            else
                pluginDirectory = Path.Combine(toolkitLocation, "Plugins");

            LOGGER.InfoFormat("Starting default probe for plugins in folder '{0}'", pluginDirectory);
            loadPlugins(pluginDirectory);

            if (!string.IsNullOrEmpty(additionalPluginPaths))
            {
                LOGGER.InfoFormat("Received additional probe paths '{0}'", additionalPluginPaths);
                string[] additionalPaths = additionalPluginPaths.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string additionalPath in additionalPaths)
                {
                    LOGGER.InfoFormat("Starting custom probe for plugins in folder '{0}'", additionalPath);
                    loadPlugins(additionalPath);
                }
            }
        }

        void loadPlugins(string folderPath)
        {
            try
            {
                foreach (var assemblyPath in Directory.GetFiles(folderPath, "*.dll"))
                {
                    try
                    {
                        var assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyPath));
                        var query = from type in assembly.GetTypes()
                                    where type.GetInterface("IPluginActivator") != null
                                    select type;

                        foreach (var pluginActivatorType in query)
                        {
                            if (!pluginActivatorType.IsAbstract)
                            {
                                var plugin = Activator.CreateInstance(pluginActivatorType) as IPluginActivator;
                                this._pluginActivators[plugin.PluginName] = plugin;
                                LOGGER.InfoFormat("Loaded plugin {0} from {1}", plugin.PluginName, assembly.GetName());
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.Debug("Error loading assembly: " + assemblyPath, e);
                        var typeLoadException = e as ReflectionTypeLoadException;
                        if (typeLoadException != null)
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
            catch (DirectoryNotFoundException)
            {
                LOGGER.WarnFormat("Skipped folder '{0}' - folder does not exist", folderPath);
            }
        }

        void registerMetaNodes()
        {
            this._rootViewMetaNode = new AWSViewMetaNode();
            foreach (var plugin in this._pluginActivators.Values)
            {
                plugin.RegisterMetaNodes();
            }
        }

    }
}
