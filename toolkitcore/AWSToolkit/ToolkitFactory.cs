using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.AWSToolkit.MobileAnalytics;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Activators;
using Amazon.AWSToolkit.Shared;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using log4net;

namespace Amazon.AWSToolkit
{
    public class ToolkitFactory
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ToolkitFactory));

        public delegate void ToolkitInitialized();

        private static event ToolkitInitialized OnToolkitInitialized;

        static ToolkitFactory INSTANCE;
        static readonly object INSTANCE_CREATE_LOCK = new object();

        readonly NavigatorControl _navigator;
        readonly IAWSToolkitShellProvider _shellProvider;
        private readonly ITelemetryLogger _telemetryLogger;

        private readonly AWSViewMetaNode _rootViewMetaNode = new AWSViewMetaNode();
        AWSViewModel _rootViewModel;

        private readonly Dictionary<string, IPluginActivator> _pluginActivators =
            new Dictionary<string, IPluginActivator>();

        private ToolkitFactory(NavigatorControl navigator,
            ITelemetryLogger telemetryLogger,
            IAWSToolkitShellProvider shellProvider)
        {
            this._navigator = navigator;
            this._shellProvider = shellProvider;
            this._telemetryLogger = telemetryLogger;

            if (ServicePointManager.DefaultConnectionLimit < 100)
            {
                ServicePointManager.DefaultConnectionLimit = 100;
            }
        }

        public static async Task InitializeToolkit(NavigatorControl navigator,
            ITelemetryLogger telemetryLogger,
            IAWSToolkitShellProvider shellProvider,
            string additionalPluginPaths,
            Action initializeCompleteCallback)
        {
            if (INSTANCE != null)
                throw new ApplicationException("Toolkit has already been initialized");

            Amazon.Util.Internal.InternalSDKUtils.SetUserAgent(shellProvider.HostInfo.Name, Constants.VERSION_NUMBER);
            ProxyUtilities.ApplyCurrentProxySettings();

            // TODO : set up region provider when ready to integrate
            // var regionProvider = new RegionProvider();
            // regionProvider.Initialize();

            var pluginActivators = await PluginActivatorUtilities.LoadPluginActivators(
                typeof(ToolkitFactory).Assembly.Location,
                additionalPluginPaths);

            INSTANCE = new ToolkitFactory(navigator, telemetryLogger, shellProvider);

            INSTANCE.InitializePluginActivators(pluginActivators);

            INSTANCE.ShellProvider.ExecuteOnUIThread((Action) (() =>
            {
                try
                {
                    INSTANCE._rootViewModel = new AWSViewModel(
                        INSTANCE._shellProvider,
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
                            "Unexpected error during initialization of AWS Toolkit. The toolkit may be unstable until Visual Studio is restarted.{0}{0}{1}",
                            Environment.NewLine,
                            e.Message
                        )
                    );

                    // We might not have initialized enough to send telemetry, but we should try.
                    ToolkitEvent evnt = new ToolkitEvent();
                    evnt.AddProperty(MetricKeys.ErrorInitializingAwsViewModel, 1);
                    SimpleMobileAnalytics.Instance.QueueEventToBeRecorded(evnt);
                }

                LOGGER.Info("ToolkitFactory initialized");
            }));
        }

        private void InitializePluginActivators(IList<IPluginActivator> pluginActivators)
        {
            this._pluginActivators.Clear();

            // Store the plugin activators
            pluginActivators
                .ToList()
                .ForEach(plugin => this._pluginActivators.Add(plugin.PluginName, plugin));

            // Register the plugins
            LOGGER.Debug("Registering Toolkit Plugins in the AWS Explorer...");
            foreach (var plugin in this._pluginActivators.Values)
            {
                LOGGER.Debug($"... registering {plugin.PluginName}");
                plugin.RegisterMetaNodes();
            }

            LOGGER.Debug("Finished Registering Toolkit Plugins in the AWS Explorer...");
        }

        public static void AddToolkitInitializedDelegate(ToolkitInitialized callback)
        {
            lock (INSTANCE_CREATE_LOCK)
            {
                if (_isShellInitializationComplete)
                {
                    LOGGER.Info("ToolkitFactory calling callback now since toolkit is initialized");
                    callback();
                }
                else
                {
                    LOGGER.Info("ToolkitFactory adding callback waiting for initialization");
                    OnToolkitInitialized += callback;
                }
            }
        }

        private static bool _isShellInitializationComplete;

        public static void SignalShellInitializationComplete()
        {
            LOGGER.Info("ToolkitFactory SignalShellInitializationComplete");
            lock (INSTANCE_CREATE_LOCK)
            {
                // If the shell has already been initialized then skip calling the callbacks again.
                if (_isShellInitializationComplete)
                    return;

                _isShellInitializationComplete = true;
            }

            OnToolkitInitialized?.Invoke();
        }


        public static ToolkitFactory Instance => INSTANCE;

        public NavigatorControl Navigator => this._navigator;

        public IAWSToolkitShellProvider ShellProvider => this._shellProvider;

        /// <summary>
        /// Entry point for toolkit code to emit metrics from.
        /// </summary>
        public ITelemetryLogger TelemetryLogger => this._telemetryLogger;

        public AWSViewMetaNode RootViewMetaNode => this._rootViewMetaNode;

        public AWSViewModel RootViewModel => this._rootViewModel;

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
                    var svc = (T) pa.QueryPluginService(typeof(T));
                    if (svc != null)
                        implementors.Add(svc);
                }
                catch (NullReferenceException)
                {
                }
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
    }
}
