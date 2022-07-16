using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.PluginServices.Activators;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Settings;
using Amazon.AWSToolkit.Shared;
using Amazon.AWSToolkit.Telemetry;

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
        private readonly IRegionProvider _regionProvider;
        private readonly ToolkitCredentialInitializer _toolkitCredentialInitializer;
        private readonly ToolkitSettingsWatcher _toolkitSettingsWatcher;
        private readonly ToolkitContext _toolkitContext;

        private readonly AWSViewMetaNode _rootViewMetaNode = new AWSViewMetaNode();
        AWSViewModel _rootViewModel;

        private readonly Dictionary<string, IPluginActivator> _pluginActivators =
            new Dictionary<string, IPluginActivator>();

        private ToolkitFactory(NavigatorControl navigator,
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider shellProvider,
            ToolkitCredentialInitializer toolkitCredentialInitializer,
            ToolkitSettingsWatcher toolkitSettingsWatcher)
        {
            _toolkitContext = toolkitContext ?? throw new ArgumentNullException(nameof(toolkitContext));
            _navigator = navigator;
            _shellProvider = shellProvider;
            _telemetryLogger = toolkitContext.TelemetryLogger ?? throw new ArgumentNullException(nameof(toolkitContext.TelemetryLogger));
            _regionProvider = toolkitContext.RegionProvider ?? throw new ArgumentNullException(nameof(toolkitContext.RegionProvider));
            _toolkitCredentialInitializer = toolkitCredentialInitializer;
            _toolkitSettingsWatcher = toolkitSettingsWatcher;

            if (ServicePointManager.DefaultConnectionLimit < 100)
            {
                ServicePointManager.DefaultConnectionLimit = 100;
            }
        }

        public static async Task InitializeToolkit(NavigatorControl navigator,
            ToolkitContext toolkitContext,
            IAWSToolkitShellProvider shellProvider,
            ToolkitCredentialInitializer toolkitCredentialInitializer,
            ToolkitSettingsWatcher toolkitSettingsWatcher,
            string additionalPluginPaths,
            Action initializeCompleteCallback)
        {
            if (INSTANCE != null)
                throw new ApplicationException("Toolkit has already been initialized");

            var pluginActivators = await PluginActivatorUtilities.LoadPluginActivators(
                typeof(ToolkitFactory).Assembly.Location,
                additionalPluginPaths);

            INSTANCE = new ToolkitFactory(navigator, toolkitContext, shellProvider,
                toolkitCredentialInitializer, toolkitSettingsWatcher);

            INSTANCE.InitializeAwsSdk();
            INSTANCE.InitializePluginActivators(pluginActivators, toolkitContext);
            INSTANCE._toolkitCredentialInitializer.Initialize();
            INSTANCE.ShellProvider.ExecuteOnUIThread((Action) (() =>
            {
                try
                {
                    INSTANCE._rootViewModel = new AWSViewModel(
                        INSTANCE._rootViewMetaNode,
                        toolkitContext);

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

                    toolkitContext.TelemetryLogger.RecordToolkitInit(new ToolkitInit()
                    {
                        Result = Result.Failed
                    });
                }

                LOGGER.Info("ToolkitFactory initialized");
            }));
        }

        private void InitializeAwsSdk()
        {
            SetAwsSdkUserAgent();
            ProxyUtilities.ApplyCurrentProxySettings();

            _toolkitSettingsWatcher.SettingsChanged += (s, e) => SetAwsSdkUserAgent();
        }

        private void SetAwsSdkUserAgent()
        {
            Amazon.Util.Internal.InternalSDKUtils.SetUserAgent(_shellProvider.HostInfo.Name, Constants.VERSION_NUMBER, $"ClientId/{ClientId.Instance}");
        }

        private void InitializePluginActivators(IList<IPluginActivator> pluginActivators, ToolkitContext toolkitContext)
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
                LOGGER.Debug($"... Initializing {plugin.PluginName}");
                plugin.Initialize(toolkitContext);

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

        /// <summary>
        /// Resolves Region-Partition mappings
        /// </summary>
        public IRegionProvider RegionProvider => this._regionProvider;

        public ToolkitContext ToolkitContext => this._toolkitContext;

        public AWSViewMetaNode RootViewMetaNode => this._rootViewMetaNode;

        public AWSViewModel RootViewModel => this._rootViewModel;

        public IAwsConnectionManager AwsConnectionManager => this._toolkitCredentialInitializer?.AwsConnectionManager;

        public ICredentialManager CredentialManager => this._toolkitCredentialInitializer?.CredentialManager;

        public ICredentialSettingsManager CredentialSettingsManager => this._toolkitCredentialInitializer?.CredentialSettingsManager;

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
