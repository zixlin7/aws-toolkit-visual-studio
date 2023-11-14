using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using System.Reflection;
using System.Windows.Interop;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.Samples.VisualStudio.IDE.OptionsPage;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using Amazon.AWSToolkit.Shared;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.ECS;

using Amazon.AWSToolkit.VisualStudio.ToolWindow;
using Amazon.AWSToolkit.VisualStudio.HostedEditor;

using Amazon.AWSToolkit.VisualStudio.Registration;

using Amazon.AWSToolkit.VisualStudio.BuildProcessors;
using Amazon.AWSToolkit.VisualStudio.DeploymentProcessors;
using Amazon.AWSToolkit.VisualStudio.Loggers;
using Amazon.AWSToolkit.VisualStudio.Services;

using Amazon.AWSToolkit.PluginServices.Deployment;

using log4net;

using Microsoft;
using Microsoft.VisualStudio.Project;
using Window = System.Windows.Window;
using Amazon.AWSToolkit.Persistence.Deployment;
using Amazon.AWSToolkit.VisualStudio.Editors.CloudFormation;

using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.VisualStudio.FirstRun.Controller;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.AwsServices;
using Amazon.AWSToolkit.Clients;
using Amazon.AWSToolkit.Settings;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.VisualStudio.Lambda;
using Amazon.AWSToolkit.VisualStudio.Telemetry;
using Amazon.AWSToolkit.VisualStudio.Utilities;
using Amazon.AWSToolkit.VisualStudio.Utilities.VsAppId;
using Amazon.AWSToolkit.CodeArtifact.Controller;
using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AWSToolkit.Themes;
using Amazon.AWSToolkit.Urls;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.VisualStudio.Commands.Lambda;
using Amazon.AWSToolkit.VisualStudio.Commands.Publishing;
using Amazon.AWSToolkit.VisualStudio.Commands.Toolkit;
using Amazon.AWSToolkit.VisualStudio.Images;
using Amazon.AWSToolkit.VisualStudio.Utilities.DTE;
using Amazon.AwsToolkit.VsSdk.Common;

using AwsToolkit.VsSdk.Common.CommonUI.Converters;

using Microsoft.VisualStudio.PlatformUI;

using Task = System.Threading.Tasks.Task;
using VsImages = Amazon.AWSToolkit.CommonUI.VsImages;
using Amazon.AWSToolkit.VisualStudio.SupportedVersion;
using Amazon.AWSToolkit.VisualStudio.Notification;

using Microsoft.VisualStudio.Threading;

using Debugger = System.Diagnostics.Debugger;
using OutputWindow = Amazon.AwsToolkit.VsSdk.Common.OutputWindow.OutputWindow;
using Amazon.AWSToolkit.CodeCommitTeamExplorer.CodeCommit.Controllers;
using Amazon.AWSToolkit.CommonUI.Behaviors;
using Amazon.AwsToolkit.SourceControl.CodeContainerProviders;
using Amazon.AWSToolkit.VisualStudio.GettingStarted;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Behaviors;
using Amazon.AwsToolkit.VsSdk.Common.Services;

using AwsToolkit.VsSdk.Common.Settings.CodeWhisperer;
using AwsToolkit.VsSdk.Common.Settings;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServer.Client;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(AWSNavigatorToolWindow),
                       Style = VsDockStyle.Tabbed,
                       Orientation = ToolWindowOrientation.Left,
                       Transient = false,
                       Window = ToolWindowGuids80.ServerExplorer)]
    [CustomProvideEditorFactory(typeof(HostedEditorFactory), 114)]
    [ProvideEditorExtension(typeof(HostedEditorFactory), ".hostedControl", 50, 
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}", 
              TemplateDir = "Templates", 
              NameResourceID = 105,
              DefaultName = "Amazon.AWSToolkit.VisualStudio")]
    [ProvideKeyBindingTable(GuidList.HostedEditorFactoryGuidString, 102)]
    [ProvideEditorLogicalView(typeof(HostedEditorFactory), EnvDTEConstants.vsViewKindTextView)]
    [Guid(GuidList.AwsToolkitPackageGuidString)]
    [AWSCommandLineRegistration(CommandLineToken = awsToolkitPluginsParam, DemandLoad = false, Arguments = 1)]
    [ProvideService(typeof(SAWSToolkitShellProvider))]
    [CustomProvideEditorFactory(typeof(TemplateEditorFactory), 113)]
    [ProvideEditorLogicalView(typeof(TemplateEditorFactory), EnvDTEConstants.vsViewKindTextView)]
    [ProvideEditorExtension(typeof(TemplateEditorFactory), ".template", 10000, NameResourceID = 113)]
    // need to force load when VS starts for CFN editor project stuff
    [ProvideProjectFactory(typeof(CloudFormationTemplateProjectFactory),
                           null,
                           "CloudFormation Template Project Files (*.cfproj);*.cfproj",
                           "cfproj",
                           "cfproj",
                           ".\\NullPath",
                           LanguageVsTemplate = "AWS")]
    [ProvideOptionPage(typeof(GeneralOptionsPage), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.General, 150, 160, true)]
    [ProvideProfile(typeof(GeneralOptionsPage), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.General, 150, 160, true, DescriptionResourceID = 150)]
    [ProvideOptionPage(typeof(ProxyOptionsPage), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.Proxy, 150, 170, true)]
    [ProvideProfile(typeof(ProxyOptionsPage), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.Proxy, 150, 170, true, DescriptionResourceID = 150)]
#if VS2022_OR_LATER
    // Show CodeWhisperer Settings in VS "Tools > Options" dialog
    [ProvideOptionPage(typeof(CodeWhispererSettingsProvider), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.CodeWhisperer, 0, 0, true)]
    [ProvideProfile(typeof(CodeWhispererSettingsProvider), SettingsText.CategoryNames.AwsToolkit, SettingsText.PageNames.CodeWhisperer, 0, 0, true)]
#endif
    [ProvideToolWindow(typeof(Amazon.AWSToolkit.VisualStudio.LogGroupsToolWindow), Style = VsDockStyle.Tabbed,
        Orientation = ToolWindowOrientation.Right,
        Transient = true,
        Window = ToolWindowGuids80.SolutionExplorer)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string, PackageAutoLoadFlags.BackgroundLoad)]
    // This attribute is required to load imagemanifests to be available for ProvideCodeContainerProvider so icons appear in Git Clone dialog
    [ProvideBindingPath(SubPath = "BindingPathAssemblies")]
    // TODO IDE-8906
    //[ProvideCodeContainerProvider(
    //    registeredName: nameof(CodeCommitCodeContainerProvider),
    //    displayNamePackageGuid: GuidList.AwsToolkitPackageGuidString,
    //    imageMonikerGuid: GuidList.VsImageCatalog.ImageCatalogGuidString,
    //    imageMonikerId: 1027, // TODO IDE-8851
    //    displayNameResourceId: "#200", // See AWSToolkitPackage.resx
    //    displayDetailResourceId: "#201", // See AWSToolkitPackage.resx
    //    typeof(CodeCommitCodeContainerProvider))]
    [ProvideCodeContainerProvider(
        registeredName: nameof(CodeCatalystCodeContainerProvider),
        displayNamePackageGuid: GuidList.AwsToolkitPackageGuidString,
        imageMonikerGuid: GuidList.MonikersGuidString,
        imageMonikerId: 0, // See Monikers.imagemanifest
        displayNameResourceId: "#210", // See AWSToolkitPackage.resx
        displayDetailResourceId: "#211", // See AWSToolkitPackage.resx
        typeof(CodeCatalystCodeContainerProvider))]

    public sealed class AWSToolkitPackage : ProjectAsyncPackage, 
                                            IVsInstalledProduct, 
                                            IAWSToolkitShellThemeService,
                                            IRegisterDataConnectionService, 
                                            IVsShellPropertyEvents, 
                                            IVsBroadcastMessageEvents,
											IVsPackage,
                                            IVsPackageExtensionProvider
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSToolkitPackage));

        // registered VS command line param, /awsToolkitPlugins c:\path1[;c:\path2;c:\path3...]
        internal const string awsToolkitPluginsParam = "awsToolkitPlugins";

        private ToolkitSettingsWatcher _toolkitSettingsWatcher;
        private ToolkitCredentialInitializer _toolkitCredentialInitializer;
        private ToolkitContext _toolkitContext;
        private TelemetryManager _telemetryManager;
        private TelemetryInfoBarManager _telemetryInfoBarManager;
        private SupportedVersionBarManager _supportedVersionBarManager;
        private NotificationInfoBarManager _notificationInfoBarManager;
        private ProductEnvironment _productEnvironment;
        private DateTime _startInitializeOn;
        private IPublishSettingsRepository _publishSettingsRepository;
        private ISettingsRepository<LoggingSettings> _loggingSettingsRepository;

        private OutputWindow _toolkitOutputWindow;
        private MetricsOutputWindow _metricsOutputWindow;
        private DocumentEventWatcher _documentEventWatcher;

        internal AWSToolkitShellProviderService ToolkitShellProviderService { get; private set; }
        internal AWSLegacyDeploymentPersistenceService LegacyDeploymentPersistenceService { get; private set; }

        internal ToolkitContext ToolkitContext => _toolkitContext;

        private IAWSCloudFormation _cloudformationPlugin;
        private IAWSElasticBeanstalk _beanstalkPlugin;

        internal readonly NavigatorVsUIHierarchy _navigatorVsUIHierarchy;

        // allows us to filter out undesirable solution elements from publishing enablement
        readonly Guid _guidSolutionFolderProject = new Guid(0x2150e333, 0x8fdc, 0x42a3, 0x94, 0x74, 0x1a, 0x39, 0x56, 0xd4, 0x6d, 0xe8);

        private const string ToolkitOutputWindowName = "Amazon Web Services";
        private readonly Guid ToolkitOutputWindowPaneId = new Guid(0xb1c55205, 0xa332, 0x4dcb, 0xbf, 0xa0, 0x86, 0x62, 0x7f, 0x6d, 0x4, 0x86 );

        uint _vsShellPropertyChangeEventSinkCookie;

        // This key will be used to tag persisted last-run deployment data in the .suo file.
        // This is doc'd as not being able to contain periods....what the docs don't say is
        // that it also must be less than 31chars to avoid ctor exceptions when calling 
        // AddOptionKey :-(
        private const string DeploymentsPersistenceTag = "awsDeployment";

        // this was the key used when the only deployment service was CloudFormation; we'll
        // read it and migrate data but not output to it
        private const string DeprecatedDeploymentsPersistenceTag = "awscfDeployment";

        // this will track all deployments against projects in a solution,
        // persisting last-run data that will be emitted to the .suo file
        // regardless of the target service
        readonly ProjectDeploymentsPersistenceManager _projectDeployments 
            = new ProjectDeploymentsPersistenceManager(new ProjectDeploymentsPersistenceManager.PersistableProjectInfoFactory(PersistableProjectInfoCreator));

        /// <summary>
        /// Set true whilst threads performing a build/deployment sequence are active,
        /// guarding against a 2nd invocation
        /// </summary>
        bool _performingDeployment = false;

        /// <summary>
        /// Allows for prelim check that msdeploy.exe is installed before we attempt any deployment
        /// </summary>
        bool? _msdeployInstallVerified = null;

        private LambdaTesterEventListener _lambdaTesterEventListener;


        /// <summary>
        /// UI dispatcher
        /// </summary>
        internal Dispatcher ShellDispatcher { get; }

        public override string ProductUserContext => null;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public AWSToolkitPackage()
        {
            _startInitializeOn = DateTime.Now;

            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            _loggingSettingsRepository = new FileSettingsRepository<LoggingSettings>();

            this.JoinableTaskFactory.Run(async () =>
            {
               await Utility.ConfigureLog4NetAsync(_loggingSettingsRepository);
            });
            AppDomain.CurrentDomain.AssemblyResolve += Utility.AssemblyResolveEventHandler;

            ToolkitSettings.Initialize();

            _navigatorVsUIHierarchy = new NavigatorVsUIHierarchy(this);
            ShellDispatcher = Dispatcher.CurrentDispatcher;

            var serviceContainer = this as IServiceContainer;
            var callback = new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SAWSToolkitShellProvider), callback, true);

            // .suo persistence keys must be registered during ctor according to docs
            AddOptionKey(DeploymentsPersistenceTag);           // this one used to store all cf/beanstalk deployment data going forward
            AddOptionKey(DeprecatedDeploymentsPersistenceTag); // so we can migrate prior version

            try
            {
                this.JoinableTaskFactory.Run(async () =>
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var vsShell = (IVsShell)GetGlobalService(typeof(SVsShell));
                    uint cookie;
                    vsShell.AdviseBroadcastMessages(this, out cookie);
                });
            }
            catch (Exception e)
            {
                LOGGER.Warn("Failed to register for broadcast messages, theme change will not be detected", e);
            }
        }

        public ILog Logger => LOGGER;

        // works around GetService being protected, and we want access to it from our
        // own service handlers
        internal object GetVSShellService(Type serviceType)
        {
            return GetService(serviceType);
        }

        internal async Task<object> GetVSShellServiceAsync(Type serviceType)
        {
            return await GetServiceAsync(serviceType);
        }

        internal void OutputToConsole(string message, bool forceVisible)
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    if (forceVisible)
                    {
                        _toolkitOutputWindow?.Show();
                    }

                    _toolkitOutputWindow?.WriteText(message);
                }
                catch (Exception e)
                {
                    LOGGER.ErrorFormat("Caught exception calling IVsOutputWindow.OutputStringThreadSafe.");
                    LOGGER.ErrorFormat("Exception message: '{0}', original message content '{1}'", e.Message, message);
                }
            });
        }

        internal ProjectDeploymentsPersistenceManager LegacyDeploymentsPersistenceManager => _projectDeployments;

        internal IAWSCloudFormation AWSCloudFormationPlugin
        {
            get
            {
                try
                {
                    if (_cloudformationPlugin == null)
                    {
                        _cloudformationPlugin = ToolkitShellProviderService.QueryAWSToolkitPluginService(typeof(IAWSCloudFormation))
                            as IAWSCloudFormation;
                    }
                }
                catch (Exception) { }
                return _cloudformationPlugin;
            }
        }

        internal bool CloudFormationPluginAvailable => AWSCloudFormationPlugin != null;

        private IAWSECS _awsECSPlugin;
        internal IAWSECS AWSECSPlugin
        {
            get
            {
                try
                {
                    if (_awsECSPlugin == null)
                    {
                        _awsECSPlugin = ToolkitShellProviderService.QueryAWSToolkitPluginService(typeof(IAWSECS))
                            as IAWSECS;
                    }
                }
                catch (Exception) { }

                if (_awsECSPlugin == null || !_awsECSPlugin.SupportedInThisVersionOfVS())
                    return null;

                return _awsECSPlugin;
            }
        }

        internal IAWSElasticBeanstalk AWSBeanstalkPlugin
        {
            get
            {
                try
                {
                    if (_beanstalkPlugin == null)
                    {
                        _beanstalkPlugin = ToolkitShellProviderService.QueryAWSToolkitPluginService(typeof(IAWSElasticBeanstalk))
                            as IAWSElasticBeanstalk;
                    }
                }
                catch (Exception) { }
                return _beanstalkPlugin;
            }
        }

        internal bool BeanstalkPluginAvailable => AWSBeanstalkPlugin != null;

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SAWSToolkitShellProvider) == serviceType)
            {
                return ToolkitShellProviderService;
            }

            if (typeof(SAWSLegacyDeploymentPersistence) == serviceType)
            {
                InstantiateLegacyDeploymentPersistenceService();
                return LegacyDeploymentPersistenceService;
            }

            return null;
        }

        void InstantiateToolkitShellProviderService(IToolkitHostInfo hostVersion, ProductEnvironment productEnvironment)
        {
            lock (this)
            {
                if (ToolkitShellProviderService == null)
                {
                    LOGGER.Debug("Creating SAWSToolkitShellProvider service");
                    ToolkitShellProviderService = new AWSToolkitShellProviderService(this, hostVersion, productEnvironment);
                }
            }
        }

        void InstantiateLegacyDeploymentPersistenceService()
        {
            lock (this)
            {
                if (LegacyDeploymentPersistenceService == null)
                {
                    LOGGER.Debug("Creating SAWSLegacyDeploymentPersistence service");
                    LegacyDeploymentPersistenceService = new AWSLegacyDeploymentPersistenceService(this);
                }
            }
        }

        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        {
            if (toolWindowType.Equals(Guid.Parse(GuidList.AWSExplorerToolWindowGuidString)))
            {
                return this;
            }

            return null;
        }

        protected override string GetToolWindowTitle(Type toolWindowType, int id)
        {
            if (toolWindowType == typeof(AWSNavigatorToolWindow))
            {
                return Resources.AWSExplorerToolWindowTitle;
            }

            return base.GetToolWindowTitle(toolWindowType, id);
        }

        protected override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
            if (toolWindowType == typeof(AWSNavigatorToolWindow))
            {
                return Task.FromResult<object>(new AWSNavigatorToolWindowState());
            }

            return base.InitializeToolWindowAsync(toolWindowType, id, cancellationToken);
        }

        internal void CodeArtifactSelectProfile(object sender, EventArgs e)
        {
            new CommandInstantiator<SelectProfileController>().Execute(ToolkitFactory.Instance.RootViewModel);
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override async System.Threading.Tasks.Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            LOGGER.Info("AWSToolkitPackage InitializeAsync started");
            try
            {
                Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));

                // ranu build of package deploys us outside of build folder hierarchy, so toolkit's
                // default plugin load fails - use a command line switch so dev's can inform toolkit
                // of where to go look
                var additionalPluginFolders = string.Empty;

                string dteVersion = null;

                _productEnvironment = await CreateProductEnvironmentAsync();
                _metricsOutputWindow = await CreateMetricsOutputWindowAsync();
                _telemetryManager = CreateTelemetryManager(_productEnvironment);
                // Give RegionProvider a chance to load data before it is needed later in Toolkit initialization
                var regionProviderTask = CreateRegionProviderAsync(_telemetryManager?.TelemetryLogger);

                NavigatorControl navigator = null;
                await this.JoinableTaskFactory.RunAsync(async delegate
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    await base.InitializeAsync(cancellationToken, progress);

#if DEBUG
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
                    var vsCmdLine = await GetServiceAsync(typeof(SVsAppCommandLine)) as IVsAppCommandLine;
                    if (vsCmdLine != null)
                    {
                        int optPresent;
                        string optValue;
                        if (vsCmdLine.GetOption(awsToolkitPluginsParam, out optPresent, out optValue) == VSConstants.S_OK && optPresent != 0)
                            additionalPluginFolders = optValue as string;
                    }
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
#endif

                    var dte = (DTE2)await GetServiceAsync(typeof(EnvDTE.DTE));
                    Assumes.Present(dte);
                    dteVersion = dte.Version;

                    RegisterProjectFactory(new CloudFormationTemplateProjectFactory(this));
                    RegisterEditorFactory(new TemplateEditorFactory(this));

                    //Create Editor Factory. Note that the base Package class will call Dispose on it.
                    RegisterEditorFactory(new HostedEditorFactory(this));

                    // Add our command handlers for menu (commands must exist in the .vsct file)
                    var mcs = await GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
                    if (null != mcs)
                    {
                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidCodeArtifactSelectProfile, CodeArtifactSelectProfile, null);
                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidPublishToElasticBeanstalk, PublishToAWS, PublishMenuCommand_BeforeQueryStatus);
                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdIdRepublishToAWS, RepublishToAWS, RepublishMenuCommand_BeforeQueryStatus);

                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidDeployTemplateSolutionExplorer, DeployTemplateSolutionExplorer, TemplateCommandSolutionExplorer_BeforeQueryStatus);
                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidDeployTemplateActiveDocument, DeployTemplateActiveDocument, TemplateCommandActiveDocument_BeforeQueryStatus);

                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidFormatTemplateSolutionExplorer, FormatTemplateSolutionExplorer, TemplateCommandSolutionExplorer_BeforeQueryStatus);
                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidFormatTemplateActiveDocument, FormatTemplateActiveDocument, TemplateCommandActiveDocument_BeforeQueryStatus);

                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidAddCloudFormationTemplate, AddCloudFormationTemplate, AddCloudFormationTemplate_BeforeQueryStatus);

                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidTeamExplorerConnect, AddTeamExplorerConnection, null);

                        SetupMenuCommand(mcs, GuidList.CommandSetGuid, PkgCmdIDList.cmdidPublishContainerToAWS, PublishContainerToAWS, PublishContainerToAWS_BeforeQueryStatus);

                        var shellService = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
                        if (shellService != null)
                        {
                            ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out _vsShellPropertyChangeEventSinkCookie));
                        }
                    }
                });

                _toolkitOutputWindow = await CreateToolkitOutputWindowAsync();
                OutputProductDetails(_productEnvironment);

                var hostInfo = DteVersion.AsHostInfo(dteVersion);
                ThemeUtil.Initialize(dteVersion);
                ToolkitThemes.Initialize(new ToolkitThemeProvider());
                _publishSettingsRepository = new FilePublishSettingsRepository();
                _toolkitSettingsWatcher = new ToolkitSettingsWatcher();

                S3FileFetcher.Initialize(_telemetryManager?.TelemetryLogger);

                // shell provider is used all the time, so pre-load. Leave legacy deployment
                // service until a plugin asks for it.
                InstantiateToolkitShellProviderService(hostInfo, _productEnvironment);

                // Enable UIs to access VS-provided images
                await InitializeImageProviderAsync();
                await InitializeVsImageAsync();

                // Ensure AWS SSO related assemblies are loaded before starting the credentials system.
                // For some reason, these DLLs do not get loaded properly before an SSO related profile needs to use the callback handler.
                LoadSsoAssemblies();

                var regionProvider = await regionProviderTask;

                _toolkitCredentialInitializer = new ToolkitCredentialInitializer(_telemetryManager.TelemetryLogger, regionProvider, ToolkitShellProviderService);
                _toolkitCredentialInitializer.AwsConnectionManager.ConnectionStateChanged += AwsConnectionManager_ConnectionStateChanged;

                var serviceClientManager = new AwsServiceClientManager(
                    _toolkitCredentialInitializer.CredentialManager,
                    regionProvider
                );

                _toolkitContext = new ToolkitContext()
                {
                    TelemetryLogger = _telemetryManager.TelemetryLogger,
                    ServiceClientManager = serviceClientManager,
                    RegionProvider = regionProvider,
                    ConnectionManager = _toolkitCredentialInitializer.AwsConnectionManager,
                    CredentialManager = _toolkitCredentialInitializer.CredentialManager,
                    CredentialSettingsManager = _toolkitCredentialInitializer.CredentialSettingsManager,
                    ToolkitHost = ToolkitShellProviderService,
                    ToolkitHostInfo = hostInfo
                };

                _documentEventWatcher = InitializeDocumentWatcher(_toolkitContext);

                navigator = await CreateNavigatorControlAsync(_toolkitContext);

                await InitializeAwsToolkitMenuCommandsAsync();

                await ViewAwsExplorerCommand.InitializeAsync(
                    _toolkitContext,
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidAWSNavigator,
                    this);

                await DeployLambdaCommand.InitializeAsync(
                    ToolkitShellProviderService,
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidDeployToLambdaServerlessTemplate,
                    this);
                await DeployLambdaCommand.InitializeAsync(
                    ToolkitShellProviderService,
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidDeployToLambdaSolutionExplorer,
                    this);

                await AddServerlessTemplateCommand.InitializeAsync(
                    ToolkitShellProviderService,
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidAddAWSServerlessTemplate,
                    this);

                await InitializePublishToAwsAsync(hostInfo);

                await UiClickMetric.InitializeAsync(_toolkitContext);

                await ToolkitFactory.InitializeToolkit(
                    navigator,
                    _toolkitContext,
                    ToolkitShellProviderService as IAWSToolkitShellProvider,
                    _toolkitCredentialInitializer,
                    _toolkitSettingsWatcher,
                    additionalPluginFolders,
                    () =>
                    {
                        // Event listener uses IAWSLambda, requires plugins to be loaded first
                        InitializeLambdaTesterEventListener();
                        RecordToolkitInitializedMetrics();
                        _toolkitInitialized = true;
                        ShowFirstRun();
                    });

                // TODO : IDE-11469 : phase out SignalShellInitializationComplete by replacing reliance on static globals with MEF & IToolkitContextProvider
                ToolkitFactory.SignalShellInitializationComplete();
                await InitializeToolkitContextProviderAsync();
                await InitializePluginCommandsAsync();
                RunLogCleanupAsync().LogExceptionAndForget();
            }
            finally
            {
                LOGGER.Info("AWSToolkitPackage InitializeAsync complete");
            }
        }

        private async Task InitializePluginCommandsAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            componentModel
                .GetExtensions<IPluginCommand>()?
                .ToList()
                .ForEach(pluginCommand => pluginCommand.RegisterAsync(this));
        }

        /// <summary>
        /// Makes the ToolkitContext object available to MEF components (through IToolkitContextProvider)
        /// </summary>
        private async Task InitializeToolkitContextProviderAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);
            var componentModel = await this.GetServiceAsync<SComponentModel, IComponentModel>();

            var toolkitContextProvider = componentModel.GetService<IToolkitContextProvider>() as ToolkitContextProvider;
            toolkitContextProvider?.Initialize(_toolkitContext);
        }

        private async Task RunLogCleanupAsync()
        {
            await TaskScheduler.Default;
            await Utility.CleanupLogFilesAsync(_loggingSettingsRepository);
        }

        private async Task<RegionProvider> CreateRegionProviderAsync(ITelemetryLogger telemetryLogger)
        {
            await TaskScheduler.Default;

            var regionProvider = new RegionProvider(telemetryLogger);
            await regionProvider.InitializeAsync();

            return regionProvider;
        }

        private void LoadSsoAssemblies()
        {
            try
            {
                // Visual Studio knows where to locate the AWS SDK DLLs when a symbol needs to be resolved, 
                // due to the declarations in AssemblyInfoAwsSdkReferences.cs. The SDK is more dynamic about
                // using SSO related DLLs when handling SSO based credentials and providing a callback.
                // We need the Toolkit to pre-load these assemblies directly, so that the credentials engine
                // is successful in working with SSO based profiles.
                Assembly.Load(Amazon.Runtime.Internal.ServiceClientHelpers.SSO_ASSEMBLY_NAME);
                Assembly.Load(Amazon.Runtime.Internal.ServiceClientHelpers.SSO_OIDC_ASSEMBLY_NAME);
            }
            catch (Exception ex)
            {
                // Log the exception to keep the rest of the extension and IDE stable.
                // This way, only users leveraging SSO credentials are affected rather than all users.
                LOGGER.Error("Unable to load AWS SSO assemblies. The Toolkit might not work with SSO connections.", ex);
                if (Debugger.IsAttached)
                {
                    Debug.Assert(false, $"Failure loading AWS SSO related DLLs: {ex.Message}");
                }
            }
        }

        private void RecordToolkitInitializedMetrics()
        {
            ToolkitFactory.Instance?.TelemetryLogger.RecordSessionStart(new SessionStart());

            var startupMs = (DateTime.Now - _startInitializeOn).TotalMilliseconds;

            ToolkitFactory.Instance?.TelemetryLogger.RecordToolkitInit(new ToolkitInit()
            {
                Result = Result.Succeeded,
                Duration = startupMs
            });
        }

        private void AwsConnectionManager_ConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            if (sender is IAwsConnectionManager connectionManager)
            {
                // Update the Telemetry system to use the new AccountId
                _telemetryManager.SetAccountId(connectionManager.ActiveAccountId);
            }
        }

        private void OutputProductDetails(ProductEnvironment productEnvironment)
        {
            var builder = new StringBuilder();
            builder.AppendLine("AWS Toolkit for Visual Studio");
            builder.AppendLine($"Toolkit Version: {productEnvironment.AwsProductVersion}");
            builder.AppendLine(
                $"Visual Studio: {productEnvironment.ParentProduct}, Version: {productEnvironment.ParentProductVersion}");
            builder.AppendLine();

            OutputToConsole(builder.ToString(), false);
        }

        private async Task<OutputWindow> CreateToolkitOutputWindowAsync()
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

                var outputWindowManager = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                Assumes.Present(outputWindowManager);

                var outputWindow = new OutputWindow(ToolkitOutputWindowPaneId, ToolkitOutputWindowName, outputWindowManager);

                DeferInitializationAsync(outputWindow).LogExceptionAndForget();

                return outputWindow;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to set up AWS Toolkit Output window", e);
                return null;
            }
        }

        private async Task<MetricsOutputWindow> CreateMetricsOutputWindowAsync()
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

                var outputWindowManager = await GetServiceAsync(typeof(SVsOutputWindow)) as IVsOutputWindow;
                Assumes.Present(outputWindowManager);

                var outputWindow = new MetricsOutputWindow(outputWindowManager);

                DeferMetricsOutputWindowInitializationAsync(outputWindow).LogExceptionAndForget();

                return outputWindow;
            }
            catch (Exception e)
            {
                Logger.Error("Unable to set up Metrics Output window", e);
                return null;
            }
        }

        private async Task DeferMetricsOutputWindowInitializationAsync(MetricsOutputWindow metricsOutputWindow)
        {
            // Access ToolkitSettings on a non-UI thread
            await TaskScheduler.Default;
            if (ToolkitSettings.Instance.ShowMetricsOutputWindow)
            {
                await DeferInitializationAsync(metricsOutputWindow);
            }
        }

        private async Task DeferInitializationAsync(OutputWindow outputWindow)
        {
            // The first output window instantiated by an extension is more expensive for some reason.
            // Delay Pane creation to avoid contention with the UI thread while the Toolkit is initializing.
            await TaskScheduler.Default;
            await Task.Delay(2000);
            await outputWindow.InitializeAsync();
        }

        private TelemetryManager CreateTelemetryManager(ProductEnvironment productEnvironment)
        {
            var telemetryManager = new TelemetryManager(productEnvironment, _metricsOutputWindow);

            // Get telemetry started in a background thread (don't block on obtaining credentials)
            ThreadPool.QueueUserWorkItem(state => { telemetryManager.Initialize(); });

            return telemetryManager;
        }

        private async Task<ProductEnvironment> CreateProductEnvironmentAsync()
        {
            await this.JoinableTaskFactory.SwitchToMainThreadAsync();
            try
            {
                var dte = (DTE2) await GetServiceAsync(typeof(EnvDTE.DTE));
                var vsAppId = (IVsAppId) await GetServiceAsync(typeof(IVsAppId));

                return ToolkitProductEnvironment.CreateProductEnvironment(vsAppId, dte);
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to create ProductEnvironment, using default values", e);
                return ProductEnvironment.Default;
            }
        }

        /// <summary>
        /// Initialize the system that allows Toolkit UIs to access
        /// images served up through Visual Studio.
        /// </summary>
        private async Task InitializeImageProviderAsync()
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                VsImages.Initialize(new VsImageProvider(this));
                CustomImages.Initialize(new VsImageProvider(this));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set up Image provider - portions of the Toolkit may not have icons", e);
            }
        }


        /// <summary>
        /// Initialize the system that watches for document events <see cref="DocumentEventWatcher"/>
        /// </summary>
        private DocumentEventWatcher InitializeDocumentWatcher(ToolkitContext toolkitContext)
        {
            try
            {
                return new DocumentEventWatcher(toolkitContext);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to initialize documents event watcher", e);
                return null;
            }
        }

        /// <summary>
        /// Initialize the toolkit control that
        /// renders the images served by Visual Studio
        /// </summary>
        private async Task InitializeVsImageAsync()
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                VsImage.Initialize(new ImageThemeConverter(new BrushToColorConverter()));
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set up VS image control - portions of the Toolkit may not have icons", e);
            }
        }

        private async Task<NavigatorControl> CreateNavigatorControlAsync(ToolkitContext toolkitContext)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                var navigator = new NavigatorControl(toolkitContext);

                return navigator;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to set up the AWS Explorer. The Toolkit is in a bad state.", e);
                return null;
            }
        }

        /// <summary>
        /// Set up the commands that appear in the "AWS Toolkit" menu
        /// </summary>
        private async Task InitializeAwsToolkitMenuCommandsAsync()
        {
            var tasks = new List<Task>
            {
                ViewUserGuideCommand.InitializeAsync(
                    ToolkitShellProviderService, _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidViewUserGuide,
                    this),

                ViewUrlCommand.InitializeAsync(
                    AwsUrls.DotNetOnAws, ToolkitShellProviderService, _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidLinkToDotNetOnAws,
                    this),

                ViewUrlCommand.InitializeAsync(
                    AwsUrls.DotNetOnAwsCommunity, ToolkitShellProviderService, _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidLinkToDotNetOnAwsCommunity,
                    this),

                ViewGettingStartedCommand.InitializeAsync(
                    this, _toolkitContext, _toolkitSettingsWatcher,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidViewGettingStarted,
                    this),

                CreateIssueCommand.InitializeAsync(
                    ToolkitShellProviderService, _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidCreateIssue,
                    this),

                ViewFeedbackPanelCommand.InitializeAsync(
                    _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidSubmitFeedback,
                    this),

                ViewToolkitLogsCommand.InitializeAsync(
                    _toolkitContext,
                    GuidList.CommandSetGuid,
                    (int) PkgCmdIDList.cmdidViewToolkitLogs,
                    this)
            };

            await Task.WhenAll(tasks);
        }

        private void InitializeLambdaTesterEventListener()
        {
            try
            {
                _lambdaTesterEventListener = new LambdaTesterEventListener(this);
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to set up event listeners for the Lambda Tester", e);
            }
        }

        private async Task InitializePublishToAwsAsync(IToolkitHostInfo hostVersion)
        {
            try
            {
                if (!hostVersion.SupportsPublishToAwsExperience())
                {
                    return;
                }

                LOGGER.Debug("Setting up Publish to AWS functionality");

                await JoinableTaskFactory.SwitchToMainThreadAsync(DisposalToken);

                // Activate the Publish to AWS Package and initialize it
                if (!(await GetServiceAsync(typeof(SInitializePublishToAwsPackage)) is IInitializePublishToAwsPackage initPublishPackageService))
                {
                    return;
                }

                await initPublishPackageService.InitializePackageAsync(_toolkitContext, ToolkitShellProviderService);

                // Publish to AWS commands can be initialized below, now that the Publishing Package is initialized

                await PublishToAwsCommand.InitializeAsync(
                    new PublishToAwsCommand(_toolkitContext, ToolkitShellProviderService, _publishSettingsRepository),
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidPublishToAws,
                    this);

                await PublishToAwsCommandSolutionExplorer.InitializeAsync(
                    new PublishToAwsCommandSolutionExplorer(_toolkitContext, ToolkitShellProviderService, _publishSettingsRepository),
                    GuidList.CommandSetGuid, (int) PkgCmdIDList.cmdidPublishToAwsSolutionExplorer,
                    this);

                LOGGER.Debug("Publish to AWS functionality initialized");
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to properly set up Publish to AWS functionality", e);
            }
        }

        /// <summary>
        /// Listener watching for transition to initialized state in shell, so we know VS is fully loaded
        /// </summary>
        /// <param name="propid"></param>
        /// <param name="propValue"></param>
        /// <returns></returns>
        public int OnShellPropertyChange(int propid, object propValue)
        {
            bool shellInitialized = false;

            if ((int)__VSSPROPID4.VSSPROPID_ShellInitialized == propid)
            {
                var propertyValue = (bool)propValue;
                LOGGER.InfoFormat("Received __VSSPROPID4.VSSPROPID_ShellInitialized property change, new value {0}", propertyValue);

                shellInitialized = propertyValue;
            }
            else if ((int) __VSSPROPID2.VSSPROPID_MainWindowVisibility == propid)
            {
                // VS 2019 emits VSSPROPID_MainWindowVisibility, not always VSSPROPID_ShellInitialized
                // VS 2017 does not emit VSSPROPID_MainWindowVisibility
                var propertyValue = (bool) propValue;
                LOGGER.InfoFormat("Received __VSSPROPID2.VSSPROPID_MainWindowVisibility property change, new value {0}", propertyValue);
                shellInitialized = propertyValue;
            }

            if (shellInitialized)
            {
                this.JoinableTaskFactory.Run(async () =>
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var shellService = await GetServiceAsync(typeof(SVsShell)) as IVsShell;
                    if (shellService != null)
                    {
                        ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(_vsShellPropertyChangeEventSinkCookie));
                    }
                });

                _vsShellPropertyChangeEventSinkCookie = 0;

                LOGGER.Debug("Toolkit considers VS to be initialized now");
                _shellInitialized = true;
                ShowFirstRun();
                ShowTelemetryNotice();
                ShowSupportedVersionNotice(ToolkitHosts.Vs2017);
                ShowSunsetNotificationsAsync().LogExceptionAndForget();
                ShowNotificationsAsync(_productEnvironment).LogExceptionAndForget();
            }

            return VSConstants.S_OK;
        }

        bool _shellInitialized = false;
        bool _toolkitInitialized = false;

        private void ShowFirstRun()
        {
            if (_shellInitialized && _toolkitInitialized)
            {
                this.JoinableTaskFactory.Run(async () =>
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    try
                    {
                        if (!ToolkitSettings.Instance.HasUserSeenFirstRunForm)
                        {
                            // TODO: IDE-10791 Remove legacy UX
                            if (ToolkitSettings.Instance.UseLegacyAccountUx)
                            {
                                new LegacyFirstRunController(this, _toolkitSettingsWatcher, _toolkitContext).Execute();
                            }
                            else
                            {
                                var view = new GettingStartedView();
                                Mvvm.SetViewModel(view, new GettingStartedViewModel(_toolkitContext));
                                _toolkitContext.ToolkitHost.OpenInEditor(view);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        LOGGER.ErrorFormat("Caught exception on first-run setup, message {0}, stack {1}", e.Message, e.StackTrace);
                    }
                });
            }
        }

        private void ShowTelemetryNotice()
        {
            if (!TelemetryNotice.CanShowNotice())
            {
                return;
            }

            this.JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                    LOGGER.Debug("Attempting to show Telemetry Banner");
                    _telemetryInfoBarManager = new TelemetryInfoBarManager(this);
                    _telemetryInfoBarManager.ShowTelemetryInfoBar();
                }
                catch (Exception e)
                {
                    LOGGER.Error("ShowTelemetryNotice error", e);
                }
            });
        }

        /// <summary>
        /// Displays an information bar indicating minimum supported version of the IDE
        /// </summary>
        /// <param name="hostDeprecated">represents the host version being deprecated</param>
        private void ShowSupportedVersionNotice(IToolkitHostInfo hostDeprecated)
        {
            var supportedVersionStrategy = new SupportedVersionStrategy(ToolkitContext.ToolkitHostInfo, hostDeprecated, ToolkitSettings.Instance);

            if (!supportedVersionStrategy.CanShowNotice())
            {
                return;
            }

            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                try
                {
                    LOGGER.Debug("Attempting to show minimum supported version info banner");
                    _supportedVersionBarManager = new SupportedVersionBarManager(this, supportedVersionStrategy);
                    _supportedVersionBarManager.ShowInfoBar();

                }
                catch (Exception e)
                {
                    LOGGER.Error("Error occurred while trying to show minimum supported version info banner", e);
                }
            });
        }

        private async Task ShowSunsetNotificationsAsync()
        {
            await TaskScheduler.Default;

            if (!_productEnvironment.TryGetBaseParentProductVersion(out var vsVersion))
            {
                vsVersion = null;
            }

#if VS2022
            var vs17Dot7Strategy = new SunsetVs17Dot6Strategy(vsVersion);
            if (await vs17Dot7Strategy.CanShowNoticeAsync())
            {
                var infoBarManager = new SunsetNotificationBarManager(this, vs17Dot7Strategy, _toolkitContext, JoinableTaskFactory, DisposalToken);
                infoBarManager.ShowInfoBar();
            }
#endif
        }

        /// <summary>
        /// Displays the latest Toolkit Notifications sourced from Hosted Files
        /// </summary>
        private async Task ShowNotificationsAsync(ProductEnvironment productEnvironment)
        {
            await TaskScheduler.Default;
            await Task.Delay(5000);
            _notificationInfoBarManager = new NotificationInfoBarManager(this, ToolkitContext, productEnvironment.AwsProductVersion);
            await _notificationInfoBarManager.ShowNotificationsAsync(S3FileFetcher.HOSTEDFILES_LOCATION + "Notifications/VisualStudio/VsInfoBar.json");
        }


        static void SetupMenuCommand(OleMenuCommandService mcs, Guid cmdSetGuid, uint commandId, EventHandler command, EventHandler queryStatus)
        {
            var commandID = new CommandID(cmdSetGuid, (int)commandId);
            var menuCommand = new OleMenuCommand(command, commandID);

            if(queryStatus != null)
                menuCommand.BeforeQueryStatus += new EventHandler(queryStatus);

            mcs.AddCommand(menuCommand);
        }

        /// <summary>
        /// Callback to create persistable object for different types of service deployments
        /// </summary>
        /// <returns></returns>
        static ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase PersistableProjectInfoCreator(string ownerServiceName, string deploymentType)
        {
            // beanstalk persistence was added in toolkit 1.2/persistence version 3; up until then we only had
            // cloudformation persistence
            if (!string.IsNullOrEmpty(ownerServiceName) && ownerServiceName == DeploymentServiceIdentifiers.BeanstalkServiceName)
                return new BeanstalkProjectPersistenceInfo();

            // deployment types was added to v3 but didn't need updated layout
            if (deploymentType == DeploymentTypeIdentifiers.VSToolkitDeployment)
                return new CloudFormationProjectPersistenceInfo();

            if (deploymentType == DeploymentTypeIdentifiers.CFNTemplateDeployment)
                return new CloudFormationTemplatePersistenceInfo();

            throw new ArgumentException(string.Format("Unable to find project persistence engine for service {0}, type {1}", ownerServiceName, deploymentType));
        }

        /// <summary>
        /// Called by the base package to load solution options if it finds the key we registered 
        /// during construction.
        /// </summary>
        /// <param name="key">Name of the stream.</param>
        /// <param name="stream">The stream from where the package should read user specific options.</param>
        protected override void OnLoadOptions(string key, Stream stream)
        {
            using (var reader = new BinaryReader(stream))
            {
                if (reader.BaseStream.Length == 0)
                    return;

                var dataLen = reader.ReadInt32();
                if (dataLen > 0)
                {
                    var data = reader.ReadBytes(dataLen);
                    var s = Encoding.Unicode.GetString(data);

                    _projectDeployments.ClearDeployments();
                    _projectDeployments.FromJson(s);
                }
            }
        }

        /// <summary>
        /// Called by the base package when the user options are being saved for the solution; if we have any
        /// project deployments, save them.
        /// </summary>
        /// <param name="key">Name of the stream.</param>
        /// <param name="stream">The stream to which the package should write user specific options.</param>
        protected override void OnSaveOptions(string key, Stream stream)
        {
            // ignore requests to persist against the old cloudformation-only tag
            if (string.Equals(key, DeprecatedDeploymentsPersistenceTag, StringComparison.OrdinalIgnoreCase))
                return;

            using (var writer = new BinaryWriter(stream))
            {
                var data = Encoding.Unicode.GetBytes(_projectDeployments.ToJson());
                if (data.Length > 0)
                {
                    writer.Write(data.Length);
                    writer.Write(data);
                }
            }
        }

#region VSToolkit (web app/web site) Deployment Commands

        /// <summary>
        /// Called by the IDE to determine if the PublishToAWS command should be visible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// Only visible if there is one selected item and that item is a project root node (should
        /// probably toughen to make that a web project too). One other possibility is to ask the IDE
        /// to run the same call against the Publish command and adopt the return.
        // Don't disable command if msdeploy not found; it's nicer for the user to be
        // able to download, install and re-run the command without VS restart
        /// </remarks>
        void PublishMenuCommand_BeforeQueryStatus(object sender, EventArgs evnt)
        {
            var publishMenuCommand = sender as OleMenuCommand;
            publishMenuCommand.Visible = false;

            try
            {
                if (CloudFormationPluginAvailable || BeanstalkPluginAvailable)
                {
                    var pi = VSUtility.SelectedWebProject;
                    var isWebProjectType = pi != null
                                           && pi.VsProjectType != VSWebProjectInfo.VsWebProjectType.NotWebProjectType;

                    // Only support web projects
                    if (!isWebProjectType)
                    {
                        return;
                    }

                    if (PublishToAwsSupportsSelectedProject())
                    {
                        publishMenuCommand.Text = "Publish to AWS Elastic Beanstalk (Legacy)...";
                    }
                    else
                    {
                        publishMenuCommand.Text = "Publish to AWS Elastic Beanstalk...";
                    }

                    publishMenuCommand.Visible = true;
                    publishMenuCommand.Enabled = !_performingDeployment;
                }
            }
            catch (Exception)
            {
            }
        }

        private static bool PublishToAwsSupportsSelectedProject()
        {
            var hierarchy = VSUtility.GetCurrentVSHierarchySelection(out _);
            if (!VsHierarchyHelpers.TryGetTargetFramework(hierarchy, VSConstants.VSITEMID_ROOT,
                    out var targetFramework))
            {
                return false;
            }

            return PublishableProjectSpecification.IsSatisfiedBy(targetFramework);
        }

        /// <summary>
        /// Called by the IDE to determine if the RepublishToAWS command should be visible
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <remarks>
        /// Only visible if there is one selected item and that item is a project root node (should
        /// probably toughen to make that a web project too) and we have prior AWS deployment history.
        /// </remarks>
        void RepublishMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var republishMenuCommand = sender as OleMenuCommand;
            republishMenuCommand.Visible = false;

            try
            {
                if (CloudFormationPluginAvailable || BeanstalkPluginAvailable)
                {
                    var pi = VSUtility.SelectedWebProject;
                    if (pi != null && pi.VsProjectType != VSWebProjectInfo.VsWebProjectType.NotWebProjectType)
                    {
                        var projectGuid = VSUtility.QueryProjectIDGuid(pi.VsHierarchy);
                        if (_projectDeployments.PersistedDeployments(projectGuid) > 0)
                        {
                            // Hoping the persistence layer is always accurate enough to enable this,
                            // but just in case not, it traps any exceptions as it digs into the data and
                            // returns null on any error at which point we'll keep the command hidden.
                            // Also, we don't make the expense of a call to validate the target is still
                            // available unless we know we can show the menu!
                            var customMenuText = _projectDeployments[projectGuid].QueryRepublishCommandText(DeploymentTypeIdentifiers.VSToolkitDeployment);
                            if (!string.IsNullOrEmpty(customMenuText)
                                        && !(_projectDeployments[projectGuid].LastDeploymentOfType(DeploymentTypeIdentifiers.VSToolkitDeployment).IsInvalid))
                            {
                                republishMenuCommand.Text = customMenuText;
                                republishMenuCommand.Visible = true;
                            }
                        }
                    }

                    republishMenuCommand.Enabled = !_performingDeployment;
                }
            }
            catch { }
        }

        // 'pings' the relevant service to see if the target of the persisted deployment
        // is still valid
        bool ValidateLastDeploymentAvailable(string projectGuid, IDeploymentHistory deploymentHistory)
        {
            // more efficient than 'is' test followed by 'as'
            var bdh = deploymentHistory as BeanstalkDeploymentHistory;
            if (bdh != null)
            {
                var bppi = _projectDeployments[projectGuid].DeploymentForService(DeploymentServiceIdentifiers.BeanstalkServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment)
                                as BeanstalkProjectPersistenceInfo;

                var isValid = false;
                var account = ToolkitFactory.Instance.RootViewModel.AccountFromIdentityKey(bppi.AccountUniqueID);
                if (account != null)
                {
                    var details = new Dictionary<string, object>
                    {
                        {BeanstalkConstants.DeploymentTargetQueryParam_ApplicationName, bdh.ApplicationName}, 
                        {BeanstalkConstants.DeploymentTargetQueryParam_EnvironmentName, bdh.EnvironmentName}
                    };
                    isValid = AWSBeanstalkPlugin.DeploymentService.IsRedeploymentTargetValid(account, bppi.LastRegionDeployedTo, details);
                }

                if (!isValid)
                    deploymentHistory.IsInvalid = true;
                return isValid;
            }

            var cfdh = deploymentHistory as CloudFormationDeploymentHistory;
            if (cfdh != null)
            {
                var cfppi = _projectDeployments[projectGuid].DeploymentForService(DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment)
                                as CloudFormationProjectPersistenceInfo;

                var isValid = false;
                var account = ToolkitFactory.Instance.RootViewModel.AccountFromIdentityKey(cfppi.AccountUniqueID);
                if (account != null)
                {
                    var details = new Dictionary<string, object>
                    {
                        {CloudFormationConstants.DeploymentTargetQueryParam_StackName, cfdh.LastStack}
                    };
                    isValid = AWSCloudFormationPlugin.DeploymentService.IsRedeploymentTargetValid(account, cfppi.LastRegionDeployedTo, details);
                }

                if (!isValid)
                    deploymentHistory.IsInvalid = true;
                return isValid;
            }

            deploymentHistory.IsInvalid = true;
            return false;
        }

        private void PublishContainerToAWS(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () => await PublishContainerToAwsAsync(sender, e));
        }

        private async Task PublishContainerToAwsAsync(object sender, EventArgs e)
        {
            if (this.AWSECSPlugin == null) return;

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var item = VSUtility.GetSelectedProject();

            if (item == null)
            {
                if (await GetServiceAsync(typeof(SAWSToolkitShellProvider)) is IAWSToolkitShellProvider shell)
                {
                    shell.ShowError("The selected item is not a project that can be deployed to AWS");
                }
                return;
            }

            var rootDirectory = Path.GetDirectoryName(item.FullName);

            var seedProperties = new Dictionary<string, object>();
            seedProperties[PublishContainerToAWSWizardProperties.SourcePath] = rootDirectory;
            seedProperties[PublishContainerToAWSWizardProperties.SelectedProjectFile] = item.FullName;

            seedProperties[PublishContainerToAWSWizardProperties.IsWebProject] = VSUtility.SelectedWebProject != null;

            StringBuilder safeProjectName = new StringBuilder();
            foreach(var c in Path.GetFileNameWithoutExtension(item.FullName).ToCharArray())
            {
                if (char.IsLetterOrDigit(c))
                    safeProjectName.Append(c);
            }
            seedProperties[PublishContainerToAWSWizardProperties.SafeProjectName] = safeProjectName.ToString();

            this.AWSECSPlugin.PublishContainerToAWS(seedProperties);
        }

        void PublishContainerToAWS_BeforeQueryStatus(object sender, EventArgs evnt)
        {
            var publishMenuCommand = sender as OleMenuCommand;
            publishMenuCommand.Visible = false;

            try
            {
                if (AWSECSPlugin == null) { return; }

                publishMenuCommand.Text = "Publish Container to AWS (Legacy)...";
                publishMenuCommand.Visible = VSUtility.IsNETCoreDockerProject;
                publishMenuCommand.Enabled = !_performingDeployment;
            }
            catch (Exception)
            {
            }
        }

        /// <summary>
        /// Start the Publish2AWS wizard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PublishToAWS(object sender, EventArgs e)
        {
            try
            {
                var pi = VSUtility.SelectedWebProject;
                if (pi == null || pi.VsProjectType == VSWebProjectInfo.VsWebProjectType.NotWebProjectType)
                    return;

                // don't pay the msdeploy install tax for coreclr project types
                if (pi.VsProjectType != VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject && _msdeployInstallVerified != true)
                {
                    var retry = false;
                    do
                    {
                        _msdeployInstallVerified = Utility.ProbeForMSDeploy();
                        if (_msdeployInstallVerified != true)
                            retry = Messaging.DisplayMSDeployRequiredMessage(GetParentWindow());

                    } while (retry);

                    if (_msdeployInstallVerified != true)
                        return;
                }

                IDictionary<string, object> wizardProperties;
                var ret = InitializeAndRunDeploymentWizard(pi, out wizardProperties);

                if (ret)
                {
                    // persist first so we take advantage of any save actions during build
                    PersistVSToolkitDeployment(pi, wizardProperties);
                    BuildAndDeployProject(pi, wizardProperties);
                }
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Exception during publishing to AWS: {0}", exc);
            }
        }

        /// <summary>
        /// Runs the new deployment wizard.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="wizardProperties"></param>
        /// <returns>True if the wizard ran to completion. False if the user cancelled or requested the legacy wizard.</returns>
        bool InitializeAndRunDeploymentWizard(VSWebProjectInfo projectInfo, out IDictionary<string, object> wizardProperties)
        {
            var tuple = this.JoinableTaskFactory.Run<Tuple<bool, IDictionary<string, object>>>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                var wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Deployment2AWS.View.Deploy2AWS", null);
                wizard.Title = "Publish to Amazon Web Services";

                SetVSToolkitDeploymentSeedData(wizard, projectInfo);

                // register the page groups we expect child pages to fit into
                wizard.RegisterPageGroups(DeploymentWizardPageGroups.DeploymentPageGroups);

                var beanstalk = AWSBeanstalkPlugin;
                if (beanstalk != null)
                {
                    var collatedPages = new List<IAWSWizardPageController>(beanstalk.DeploymentService.ConstructDeploymentPages(wizard, false))
                    {
                        new CommonUI.LegacyDeploymentWizard.PageControllers.DeploymentReviewPageController()
                    };
                    wizard.RegisterPageControllers(collatedPages, 0);
                }

                wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

                var uiShell = (await GetServiceAsync(typeof(SVsUIShell))) as IVsUIShell;
                if (uiShell == null)
                {
                    LOGGER.Error("GetServiceAsync(typeof(SVsUIShell)) returned null");
                    return new Tuple<bool, IDictionary<string, object>>(false, new Dictionary<string, object>());
                }

                IntPtr parent;
                uiShell.GetDialogOwnerHwnd(out parent);

                var success = beanstalk?.ShowPublishWizard(wizard) ?? false;

                IDictionary<string, object> localWizardProperties = wizard.CollectedProperties;

                return new Tuple<bool, IDictionary<string, object>>(success, localWizardProperties);
            });

            wizardProperties = tuple.Item2;
            return tuple.Item1;
        }

        /// <summary>
        /// Start republishing the project to the last-used CloudFormation stack or Elastic Beanstalk environment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RepublishToAWS(object sender, EventArgs e)
        {
            this.JoinableTaskFactory.Run(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();

                var pi = VSUtility.SelectedWebProject;
                if (pi == null || pi.VsProjectType == VSWebProjectInfo.VsWebProjectType.NotWebProjectType)
                    return;

                var projectGuid = VSUtility.QueryProjectIDGuid(pi.VsHierarchy);
                var deploymentHistory = _projectDeployments[projectGuid].LastDeploymentOfType(DeploymentTypeIdentifiers.VSToolkitDeployment);
                if (!ValidateLastDeploymentAvailable(projectGuid, deploymentHistory))
                {
                    string msg;
                    if (deploymentHistory is BeanstalkDeploymentHistory)
                        msg = string.Format("Environment '{0}' is no longer available; redeployment cannot proceed.",
                                            (deploymentHistory as BeanstalkDeploymentHistory).EnvironmentName);
                    else
                        msg = string.Format("Stack '{0}' is no longer available; redeployment cannot proceed.",
                                            (deploymentHistory as CloudFormationDeploymentHistory).LastStack);

                    ToolkitShellProviderService.ShowMessage("Not Available", msg);
                    return;
                }

                // use a one-page wizard to have the same look as the standard deployment wizard
                var wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.Deployment2AWS.View.Redeploy2AWS", null);
                SetVSToolkitFastRedeploymentSeedData(wizard, pi, deploymentHistory);

                var pageControllers = new List<IAWSWizardPageController>();

                // fast-track republish does not use page groups
                if (deploymentHistory is BeanstalkDeploymentHistory)
                {
                    wizard.Title = "Re-publish to AWS Elastic Beanstalk";
                    var beanstalk = AWSBeanstalkPlugin;
                    if (beanstalk != null)
                    {
                        pageControllers.AddRange(beanstalk.DeploymentService.ConstructDeploymentPages(wizard, true));
                    }
                }
                else
                {
                    wizard.Title = "Re-publish to AWS CloudFormation";
                    var cloudFormation = AWSCloudFormationPlugin;
                    if (cloudFormation != null)
                    {
                        pageControllers.AddRange(cloudFormation.DeploymentService.ConstructDeploymentPages(wizard, true));
                    }
                }

                wizard.RegisterPageControllers(pageControllers, 0);

                var uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
                if(uiShell == null)
                {
                    LOGGER.Error("GetServiceAsync(typeof(SVsUIShell)) returned null");
                    return;
                }

                IntPtr parent;
                uiShell.GetDialogOwnerHwnd(out parent);

                if (wizard.Run())
                {
                    // persist first so we take advantage of any save actions during build
                    PersistVSToolkitDeployment(pi, wizard.CollectedProperties);
                    BuildAndDeployProject(pi, wizard.CollectedProperties);
                }
            });
        }

#endregion

        /// <summary>
        /// Add seed/default properties for deployment wizard based on last-run persisted
        /// values if available plus some defaults if first-time-deployment
        /// </summary>
        /// <param name="wizard"></param>
        /// <param name="projectInfo"></param>
        void SetVSToolkitDeploymentSeedData(IAWSWizard wizard, VSWebProjectInfo projectInfo)
        {
            var seedProperties = new Dictionary<string, object>();

            try
            {
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_RedeployingAppVersion, false);

                var availableServiceOwners = new HashSet<string>();
                if (CloudFormationPluginAvailable)
                    availableServiceOwners.Add(DeploymentServiceIdentifiers.CloudFormationServiceName);
                if (BeanstalkPluginAvailable)
                    availableServiceOwners.Add(DeploymentServiceIdentifiers.BeanstalkServiceName);
                seedProperties.Add(CommonWizardProperties.propkey_NavigatorRootViewModel, ToolkitFactory.Instance.RootViewModel);
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_AvailableServiceOwners, availableServiceOwners);

                // key will be service owner name, value is templated DeploymentHistories<> relevant to service
                var previousDeployments = new Dictionary<string, object>();
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments, previousDeployments);

                var projectGuid = VSUtility.QueryProjectIDGuid(projectInfo.VsHierarchy);
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_VSProjectGuid, projectGuid);

                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_ProjectType,
                    projectInfo.VsProjectType == VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject
                        ? DeploymentWizardProperties.NetCoreWebProject
                        : DeploymentWizardProperties.StandardWebProject);

                SeedAvailableBuildConfigurations(projectInfo, seedProperties);
                SeedAvailableFrameworks(projectInfo, seedProperties);

                if (_projectDeployments.PersistedDeployments(projectGuid) > 0)
                {
                    // push all persisted service deployments into the seeds, then use the last-deployed service to
                    // dictate account, region etc. Key to persisted info per service is dynamic based on service name.
                    var deployments = _projectDeployments[projectGuid];
                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_LastServiceDeployedTo, deployments.LastServiceDeployment);

                    if (availableServiceOwners.Contains(DeploymentServiceIdentifiers.CloudFormationServiceName))
                    {
                        var cfppi = deployments.DeploymentForService(DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment) 
                                    as CloudFormationProjectPersistenceInfo;
                        if (cfppi != null)
                        {
                            previousDeployments.Add(DeploymentServiceIdentifiers.CloudFormationServiceName, cfppi.PreviousDeployments);
                            if (deployments.LastServiceDeployment == DeploymentServiceIdentifiers.CloudFormationServiceName)
                            {
                                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, cfppi.AccountUniqueID);
                                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, cfppi.LastRegionDeployedTo);
                            }
                        }
                    }

                    if (availableServiceOwners.Contains(DeploymentServiceIdentifiers.BeanstalkServiceName))
                    {
                        var bppi = deployments.DeploymentForService(DeploymentServiceIdentifiers.BeanstalkServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment) 
                                    as BeanstalkProjectPersistenceInfo;
                        if (bppi != null)
                        {
                            previousDeployments.Add(DeploymentServiceIdentifiers.BeanstalkServiceName, bppi.PreviousDeployments);

                            if (deployments.LastServiceDeployment == DeploymentServiceIdentifiers.BeanstalkServiceName)
                            {
                                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, bppi.AccountUniqueID);
                                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, bppi.LastRegionDeployedTo);
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                /* no great loss */
            }
            finally
            {
                seedProperties.Add(CommonWizardProperties.propkey_HostShellVersion, ToolkitShellProviderService.HostInfo.Version);

                if (!seedProperties.ContainsKey(DeploymentWizardProperties.SeedData.propkey_SeedName))
                {
                    this.JoinableTaskFactory.Run(async () =>
                    {
                        await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                        object value;
                        if (projectInfo.VsHierarchy.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_Name, out value)
                                    == VSConstants.S_OK && value != null && value is string)
                        {
                            var sb = new StringBuilder();
                            foreach (var c in (string)value)
                            {
                                if (Char.IsLetterOrDigit(c))
                                    sb.Append(c);
                            }
                            seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedName, sb.ToString());
                        }
                    });
                }

                // seed the suggested .NET runtime needed for the application from project properties, if available
                var targetRuntime = projectInfo.TargetRuntime;
                seedProperties.Add(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime, targetRuntime);

                seedProperties.Add(DeploymentWizardProperties.AppOptions.propkey_Enable32BitApplications, false);

                if (!seedProperties.ContainsKey(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel))
                {
                    var seedVersion = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel, string.Format("v{0}", seedVersion));
                }
            }

            if (seedProperties.Count > 0)
                wizard.SetProperties(seedProperties);
        }

        /// <summary>
        /// Sets the seed properties for a fast-track redeployment to the last service used with this project
        /// </summary>
        /// <param name="wizard"></param>
        /// <param name="projectInfo"></param>
        /// <param name="deploymentHistory"></param>
        void SetVSToolkitFastRedeploymentSeedData(IAWSWizard wizard, VSWebProjectInfo projectInfo, IDeploymentHistory deploymentHistory)
        {
            var seedProperties = new Dictionary<string, object>();

            try
            {
                seedProperties.Add(CommonWizardProperties.propkey_NavigatorRootViewModel, ToolkitFactory.Instance.RootViewModel);

                var projectGuid = VSUtility.QueryProjectIDGuid(projectInfo.VsHierarchy);
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_VSProjectGuid, projectGuid);

                // key will be service owner name, value is templated DeploymentHistories<> relevant to service
                var previousDeployments = new Dictionary<string, object>();

                SeedAvailableBuildConfigurations(projectInfo, seedProperties);

                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_ProjectType,
                    projectInfo.VsProjectType == VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject
                        ? DeploymentWizardProperties.NetCoreWebProject
                        : DeploymentWizardProperties.StandardWebProject);

                if (deploymentHistory is CloudFormationDeploymentHistory)
                {
                    var cfppi = _projectDeployments[projectGuid].DeploymentForService(DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment)
                            as CloudFormationProjectPersistenceInfo;

                    previousDeployments.Add(DeploymentServiceIdentifiers.CloudFormationServiceName, cfppi.PreviousDeployments);

                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, cfppi.AccountUniqueID);
                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, cfppi.LastRegionDeployedTo);

                    // CloudFormation deployment is still locked to the active build configuration
                    var activeConfiguration = seedProperties[DeploymentWizardProperties.SeedData.propkey_ActiveBuildConfiguration] as string;
                    wizard.SetProperty(DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration, activeConfiguration);
                }
                else
                {
                    var bppi = _projectDeployments[projectGuid].DeploymentForService(DeploymentServiceIdentifiers.BeanstalkServiceName, DeploymentTypeIdentifiers.VSToolkitDeployment)
                            as BeanstalkProjectPersistenceInfo;

                    previousDeployments.Add(DeploymentServiceIdentifiers.BeanstalkServiceName, bppi.PreviousDeployments);

                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, bppi.AccountUniqueID);
                    seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, bppi.LastRegionDeployedTo);

                    SeedAvailableFrameworks(projectInfo, seedProperties);
                }

                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments, previousDeployments);

                var seedVersion = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel, string.Format("v{0}", seedVersion));
            }
            catch { }
            finally
            {
                seedProperties.Add(CommonWizardProperties.propkey_HostShellVersion, ToolkitShellProviderService.HostInfo.Version);

                if (projectInfo.VsProjectType != VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject)
                {
                    var targetRuntime = projectInfo.TargetRuntime;
                    seedProperties.Add(DeploymentWizardProperties.AppOptions.propkey_TargetRuntime, targetRuntime);
                }
            }

            wizard.SetProperties(seedProperties);
        }

        /// <summary>
        /// Scans the collection of solution contexts for each solution configuration to subset
        /// down to the set of solution configurations that will build the project selected for
        /// deployment. For each selected solution configuration, we associate the project build
        /// configuration that will be in-context for that solution configuration (we need the
        /// project build configuration for msbuild-based builds, and the solution configuration
        /// name for automation-based builds).
        /// </summary>
        private void SeedAvailableBuildConfigurations(VSWebProjectInfo projectInfo, Dictionary<string, object> seedProperties)
        {
            JoinableTaskFactory.Run(
                async () => await SeedAvailableBuildConfigurationsAsync(projectInfo, seedProperties));
        }

        private async Task SeedAvailableBuildConfigurationsAsync(VSWebProjectInfo projectInfo, Dictionary<string, object> seedProperties)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = (DTE2) await GetServiceAsync(typeof(EnvDTE.DTE));
            Assumes.Present(dte);
            var solutionBuild = dte.Solution.SolutionBuild;
            var solutionConfigurations = solutionBuild.SolutionConfigurations;

            var uniqueProjectName = projectInfo.DTEProject.UniqueName;

            // Visual Studio's project automation model is a matrix of configuration names/platform names 
            // per solution and per project where the solution configuration names and project configuration 
            // names can be the same (or different) and solution platforms and project platforms must be the 
            // same. See here for more context: 
            // http://www.visualstudioextensibility.com/2008/08/29/the-convoluted-build-configuration-of-the-automation-model-envdte-envdte80/

            var buildConfigurations = new Dictionary<string, string>();
            foreach (var item in solutionConfigurations)
            {
                var solutionConfiguration = item as SolutionConfiguration;
                if (solutionConfiguration == null)
                    continue;

                foreach (var contextItem in solutionConfiguration.SolutionContexts)
                {
                    var context = contextItem as SolutionContext;
                    if (context == null)
                        continue;

                    if (context.ProjectName.Equals(uniqueProjectName, StringComparison.Ordinal) && context.ShouldBuild)
                    {
                        var solutionBuildKey = string.Concat(solutionConfiguration.Name, "|", context.PlatformName);
                        if (!buildConfigurations.ContainsKey(solutionBuildKey))
                        {
                            var projectConfigurationKey = string.Join("|", context.ConfigurationName, context.PlatformName);
                            buildConfigurations.Add(solutionBuildKey, projectConfigurationKey);
                        }
                    }
                }
            }

            // based on past experience with automation apis there is no guarantee that the same object will be returned 
            // here and in the collection enumeration above, so we still have to dig through the contexts to find the correct 
            // platform for the project at hand
            string activeConfigurationKey = string.Empty;
            var activeConfiguration = solutionBuild.ActiveConfiguration;
            foreach (var activeContextItem in activeConfiguration.SolutionContexts)
            {
                var context = activeContextItem as SolutionContext;
                if (context == null)
                    continue;

                if (context.ProjectName.Equals(uniqueProjectName, StringComparison.Ordinal) && context.ShouldBuild)
                {
                    activeConfigurationKey = string.Concat(activeConfiguration.Name, "|", context.PlatformName);
                    break;
                }
            }

            seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_ProjectBuildConfigurations, buildConfigurations);
            seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_ActiveBuildConfiguration, activeConfigurationKey);
        }

        /// <summary>
        /// For CoreCLR projects, sets up the available frameworks the user can deploy with. For traditional
        /// projects, it sets up the runtimes that can be used for the apppool. For both case, the seeded output
        /// is a dictionary of UI-visible text to the control code that is passed to the build/deployment handlers.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="seedProperties"></param>
        void SeedAvailableFrameworks(VSWebProjectInfo projectInfo, Dictionary<string, object> seedProperties)
        {
            var frameworks = new Dictionary<string, string>();

            if (projectInfo.VsProjectType == VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject)
            {
                var projectJsonLocation = Path.Combine(projectInfo.VsProjectLocation, "project.json");
                if (File.Exists(projectJsonLocation))
                {
                    JsonData root = JsonMapper.ToObject(File.ReadAllText(projectJsonLocation));
                    JsonData frameworksNode = root["frameworks"] as JsonData;
                    foreach (var key in frameworksNode.PropertyNames)
                    {
                        frameworks.Add(key, key);
                    }
                }
                else
                {
                    foreach(var framework in VSUtility.GetSelectedNetCoreProjectFrameworks())
                    {
                        frameworks.Add(framework, framework);
                    }
                }

                // Safety net in case MS changes the project.json system underneath us
                if (frameworks.Count == 0)
                {
                    frameworks.Add("netcoreapp1.0", "netcoreapp1.0");
                    frameworks.Add("netcoreapp1.1", "netcoreapp1.1");
                }
            }
            else
            {
                frameworks.Add("2.0 .NET Runtime", "2.0");
                frameworks.Add("4.0 .NET Runtime", "4.0");
            }

            seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_ProjectFrameworks, frameworks);
        }

        /// <summary>
        /// Persists useful last-run data into the .suo file
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="wizardProperties"></param>
        void PersistVSToolkitDeployment(VSWebProjectInfo projectInfo, IDictionary<string, object> wizardProperties)
        {
            try
            {
                var projectGuid = VSUtility.QueryProjectIDGuid(projectInfo.VsHierarchy);

                var deployedToService = wizardProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
                _projectDeployments.SetLastServiceDeploymentForProject(projectGuid, deployedToService, DeploymentTypeIdentifiers.VSToolkitDeployment);

                if (deployedToService == DeploymentServiceIdentifiers.CloudFormationServiceName)
                {
                    var cfppi
                        = GetPersistedInfoForService(projectGuid, deployedToService, DeploymentTypeIdentifiers.VSToolkitDeployment) 
                            as CloudFormationProjectPersistenceInfo;
                    var selectedAccount = CommonWizardProperties.AccountSelection.GetSelectedAccount(wizardProperties);
                    cfppi.AccountUniqueID = selectedAccount.SettingsUniqueKey;

                    var region = CommonWizardProperties.AccountSelection.GetSelectedRegion(wizardProperties).Id;
                    cfppi.LastRegionDeployedTo = region;
                    var cfdh = new CloudFormationDeploymentHistory(wizardProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string);
                    cfppi.PreviousDeployments.AddDeployment(selectedAccount.SettingsUniqueKey,
                                                            region,
                                                            cfdh);
                }
                else
                {
                    var bppi = GetPersistedInfoForService(projectGuid, deployedToService, DeploymentTypeIdentifiers.VSToolkitDeployment) 
                            as BeanstalkProjectPersistenceInfo;
                    var selectedAccount = CommonWizardProperties.AccountSelection.GetSelectedAccount(wizardProperties);

                    bppi.AccountUniqueID = selectedAccount.SettingsUniqueKey;

                    var region = CommonWizardProperties.AccountSelection.GetSelectedRegion(wizardProperties).Id;

                    bppi.LastRegionDeployedTo = region;

                    string usedVersion;
                    var isCustomApplicationVersion = false;
                    var seedVersion = wizardProperties[DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel] as string;
                    if (wizardProperties.ContainsKey(BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel))
                    {
                        usedVersion = wizardProperties[BeanstalkDeploymentWizardProperties.ApplicationProperties.propkey_VersionLabel] as string;
                        isCustomApplicationVersion = string.Compare(seedVersion, usedVersion, true) != 0;
                    }
                    else
                        usedVersion = seedVersion;

                    var isIncrementalDeployment = false;
                    var incrementalRepoLocation = string.Empty;
                    if (wizardProperties.ContainsKey(BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment))
                        isIncrementalDeployment = (bool)wizardProperties[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalDeployment];
                    if (isIncrementalDeployment)
                        incrementalRepoLocation = wizardProperties[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propkey_IncrementalPushRepositoryLocation] as string;

                    // if the user employed the legacy wizard, the build configuration setting is the IDE's active one
                    var bdh = new BeanstalkDeploymentHistory
                                (
                                    wizardProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_DeploymentName] as string,
                                    usedVersion,
                                    isCustomApplicationVersion,
                                    wizardProperties[BeanstalkDeploymentWizardProperties.EnvironmentProperties.propkey_EnvName] as string,
                                    isIncrementalDeployment,
                                    incrementalRepoLocation,
                                    wizardProperties[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] as string
                                );

                    bppi.PreviousDeployments.AddDeployment(selectedAccount.SettingsUniqueKey, region, bdh);
                }
            }
            catch (Exception) // not really a big deal if this fails
            {
            }
        }

        internal ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase GetPersistedInfoForService(string projectGuid, string serviceName, string deploymentType)
        {
            var ppi = _projectDeployments[projectGuid];
            foreach (var i in ppi.ProjectDeployments.Where(i => i.ServiceOwner == serviceName && i.DeploymentType == deploymentType))
            {
                return i;
            }

            var newinfo = PersistableProjectInfoCreator(serviceName, deploymentType);
            _projectDeployments.AddProjectPersistenceInfo(projectGuid, newinfo);
            return newinfo;
        }

        void BuildAndDeployProject(VSWebProjectInfo projectInfo, IDictionary<string, object> wizardProperties)
        {
            BuildAndDeploymentControllerBase bdc;
            var deploymentService = wizardProperties[DeploymentWizardProperties.DeploymentTemplate.propkey_TemplateServiceOwner] as string;
            if (deploymentService == DeploymentServiceIdentifiers.CloudFormationServiceName)
            {
                bdc = new CloudFormationBuildAndDeployController(Dispatcher.CurrentDispatcher)
                {
                    ServicePlugin = AWSCloudFormationPlugin,
                    DeploymentProcessor = new CloudFormationDeploymentProcessor()
                };
            }
            else
            {
                var useEbTool = false;
                if(wizardProperties[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_UseEbToolsToDeploy] is bool)
                {
                    useEbTool = (bool)wizardProperties[BeanstalkDeploymentWizardProperties.DeploymentModeProperties.propKey_UseEbToolsToDeploy];
                }

                if(useEbTool)
                {
                    var processor = new EbToolsDeploymentProcessor();
                    bdc = new BeanstalkBuildAndDeployController(Dispatcher.CurrentDispatcher)
                    {
                        ServicePlugin = AWSBeanstalkPlugin,
                        DeploymentProcessor = processor,
                        BuildProcessor = processor
                    };
                }
                else
                {
                    bdc = new BeanstalkBuildAndDeployController(Dispatcher.CurrentDispatcher)
                    {
                        ServicePlugin = AWSBeanstalkPlugin,
                        DeploymentProcessor = new BeanstalkDeploymentProcessor()
                    };
                }
            }

            bdc.HostServiceProvider = GetServiceOnUI;

            if(bdc.BuildProcessor == null)
            {
                switch (projectInfo.VsProjectType)
                {
                    case VSWebProjectInfo.VsWebProjectType.WebApplicationProject:
                        bdc.BuildProcessor = new WebAppProjectBuildProcessor();
                        break;
                    case VSWebProjectInfo.VsWebProjectType.WebSiteProject:
                        bdc.BuildProcessor = new WebSiteProjectBuildProcessor();
                        break;
                    case VSWebProjectInfo.VsWebProjectType.CoreCLRWebProject:
                        bdc.BuildProcessor = new CoreCLRWebAppProjectBuildProcessor();
                        break;
                }
            }

            bdc.ProjectInfo = projectInfo;
            bdc.Options = wizardProperties;
            bdc.Logger = new IDEConsoleLogger(ToolkitShellProviderService);
            bdc.OnCompletionCallback = BuildAndDeploymentCompleted;

            bdc.Execute();
        }

        object GetServiceOnUI(Type serviceType)
        {
            return this.JoinableTaskFactory.Run<object>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                return await this.GetServiceAsync(serviceType);
            });
        }

        // callback from build/deployment sequencer is done on our UI thread
        void BuildAndDeploymentCompleted(bool succeeded)
        {
            _performingDeployment = false;
        }

        internal readonly Dictionary<uint, WeakReference> ControlCache = new Dictionary<uint, WeakReference>();
        internal int ControlCounter = 0;

        internal IAWSToolkitControl PopControl(uint controlId)
        {
            IAWSToolkitControl control = null;
            WeakReference reference;
            if (ControlCache.TryGetValue(controlId, out reference))
            {
                if (reference.IsAlive)
                {
                    control = reference.Target as IAWSToolkitControl;
                }
                ControlCache.Remove(controlId);
            }

            return control;
        }

        internal readonly Dictionary<string, WeakReference> OpenedEditors = new Dictionary<string, WeakReference>();

        internal void ClearDeadWeakReferences(object stateInfo)
        {
            var toBeDeleted = (from kvp in OpenedEditors where !kvp.Value.IsAlive select kvp.Key).ToList();

            foreach (var key in toBeDeleted)
            {
                OpenedEditors.Remove(key);
            }
        }

        internal readonly Dictionary<string, string> ControlUniqueNameToFileName = new Dictionary<string, string>();

        internal string GetTempFileLocation()
        {
            var folder = Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/.aws/temp";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }

        /// <summary>
        /// Returns an IDE handle that can be used as a parent to extension modal dialogs
        /// </summary>
        internal IntPtr GetParentWindowHandle()
        {
            return this.JoinableTaskFactory.Run<IntPtr>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                IntPtr parentHwnd = IntPtr.Zero;

                var uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
                if (uiShell == null)
                {
                    return parentHwnd;
                }

                var result = uiShell.GetDialogOwnerHwnd(out parentHwnd);
                if (result != VSConstants.S_OK)
                {
                    LOGGER.Debug($"Unable to get GetDialogOwnerHwnd (result: {result})");
                }

                return parentHwnd;
            });
        }

        internal Window GetParentWindow()
        {
            return this.JoinableTaskFactory.Run<Window>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                var uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
                Assumes.Present(uiShell);
                if (uiShell.GetDialogOwnerHwnd(out var parent) != VSConstants.S_OK)
                {
                    return null;
                }
                var host = new Window();
                var wih = new WindowInteropHelper(host) { Owner = parent };

                return host;
            });
        }

        /// <summary>
        /// Checks whether the specified project is a solution folder
        /// </summary>
        public bool IsSolutionFolderProject(IVsHierarchy pHier)
        {
            return this.JoinableTaskFactory.Run<bool>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                var pFileFormat = pHier as IPersistFileFormat;
                if (pFileFormat != null)
                {
                    Guid guidClassID;
                    if (pFileFormat.GetClassID(out guidClassID) == VSConstants.S_OK
                            && guidClassID.CompareTo(_guidSolutionFolderProject) == 0)
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        /// <summary>
        /// This method loads a localized string based on the specified resource.
        /// </summary>
        /// <param name="resourceName">Resource to load</param>
        /// <returns>String loaded for the specified resource</returns>
        internal string GetResourceString(string resourceName)
        {
            return this.JoinableTaskFactory.Run<string>(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                string resourceValue;
                var resourceManager = await GetServiceAsync(typeof(SVsResourceManager)) as IVsResourceManager;
                if (resourceManager == null)
                {
                    throw new InvalidOperationException("Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
                }
                var packageGuid = this.GetType().GUID;
                var hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
                ErrorHandler.ThrowOnFailure(hr);
                return resourceValue;
            });
        }


#region Template Command Query Status

        void TemplateCommandSolutionExplorer_BeforeQueryStatus(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
                await TemplateCommand_BeforeQueryStatusAsync(sender as OleMenuCommand, false));
        }

        void TemplateCommandActiveDocument_BeforeQueryStatus(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
                await TemplateCommand_BeforeQueryStatusAsync(sender as OleMenuCommand, false));
        }

        private async Task TemplateCommand_BeforeQueryStatusAsync(OleMenuCommand menuCommand, bool activeDocument)
        {
            if (menuCommand == null) return;

            await JoinableTaskFactory.SwitchToMainThreadAsync();

            menuCommand.Visible = false;

            try
            {
                if (CloudFormationPluginAvailable)
                {
                    if (activeDocument)
                    {
                        var dte = GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                        Assumes.Present(dte);

                        if (dte.ActiveDocument != null)
                        {
                            var fullName = dte.ActiveDocument.FullName;
                            // Don't show commands for a serverless template because it needs to be deployed with code.
                            if (!string.Equals(Path.GetFileName(fullName),
                                Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME, StringComparison.OrdinalIgnoreCase))
                            {
                                menuCommand.Visible =
                                    fullName.EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension);
                            }
                        }
                    }
                    else
                    {
                        var item = VSUtility.GetSelectedProjectItem();
                        if(item != null && item.Name != null)
                        {
                            if (!string.Equals(Path.GetFileName(item.Name), Constants.AWS_SERVERLESS_TEMPLATE_DEFAULT_FILENAME, StringComparison.OrdinalIgnoreCase) &&
                                item.Name.EndsWith(ToolkitFileTypes.CloudFormationTemplateExtension))
                            {
                                menuCommand.Visible = true;
                            }
                        }
                    }
                }
            }
            catch { }
        }

#endregion

#region CloudFormation Template Deployment Command

        void DeployTemplateSolutionExplorer(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    var prjItem = VSUtility.GetSelectedProjectItem();
                    DeployTemplate(prjItem);
                }
                catch { }
            });
        }

        private void DeployTemplateActiveDocument(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();

                try
                {
                    var dte = GetGlobalService(typeof(EnvDTE.DTE)) as DTE2;
                    Assumes.Present(dte);
                    var prjItem = dte.ActiveDocument.ProjectItem;
                    DeployTemplate(prjItem);
                }
                catch { }
            });
        }

        void DeployTemplate(EnvDTE.ProjectItem prjItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (prjItem == null)
                return;

            if (!prjItem.Saved)
                prjItem.Save();

            IAWSLegacyDeploymentPersistence persistenceService = GetService(typeof(SAWSLegacyDeploymentPersistence)) as IAWSLegacyDeploymentPersistence;
            if (persistenceService == null)
                LOGGER.Warn("Failed to obtain IAWSLegacyDeploymentPersistence instance; deployment will ignore any previously persisted data about the project.");

            string fullPath = null;
            var node = prjItem.Object as FileNode;
            if (node != null && File.Exists(node.Url))
                fullPath = node.Url;
            else if (prjItem.DTE.ActiveDocument != null && prjItem.FileCount > 0 && File.Exists(prjItem.FileNames[0]))
                fullPath = prjItem.FileNames[0];

            Dictionary<string, object> initialParameters = null;
            // Only attempt to look up and store last used values for templates files in an AWS CloudFormation project
            if (node != null && node is TemplateFileNode && persistenceService != null)
            {
                var projectGuid = VSUtility.QueryProjectIDGuid(prjItem.ContainingProject);
                var persistenceKey = persistenceService.CalcPersistenceKeyForProjectItem(prjItem);
                initialParameters = persistenceService.SetTemplateDeploymentSeedData(projectGuid, persistenceKey);
            }

            var templateDeploymentData = this.AWSCloudFormationPlugin.DeployCloudFormationTemplate(fullPath, initialParameters ?? new Dictionary<string, object>());

            if (templateDeploymentData != null && node != null && node is TemplateFileNode && persistenceService != null)
            {
                persistenceService.PersistTemplateDeployment(prjItem, templateDeploymentData);
            }
        }

#endregion

#region Format Template Command

        void FormatTemplateSolutionExplorer(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();

                    var prjItem = VSUtility.GetSelectedProjectItem();
                    if (prjItem == null)
                        return;


                    if (prjItem.Document != null && prjItem.Document.Object("TextDocument") != null)
                    {
                        var txtDocument = (EnvDTE.TextDocument) prjItem.Document.Object("TextDocument");
                        FormatTemplate(txtDocument);
                    }
                    else
                    {
                        FormatTemplate(prjItem);
                    }
                }
                catch (Exception ex)
                {
                    LOGGER.Error("Failed to format document", ex);
                }
            });
        }


        void FormatTemplateActiveDocument(object sender, EventArgs e)
        {
            JoinableTaskFactory.Run(async () =>
            {
                try
                {
                    await JoinableTaskFactory.SwitchToMainThreadAsync();

                    if (!(GetGlobalService(typeof(EnvDTE.DTE)) is DTE2 dte))
                    {
                        return;
                    }

                    var txtDocument = (EnvDTE.TextDocument) dte.ActiveDocument.Object("TextDocument");
                    if (txtDocument != null)
                    {
                        FormatTemplate(txtDocument);
                    }
                    else
                    {
                        var prjItem = dte.ActiveDocument.ProjectItem;
                        FormatTemplate(prjItem);
                    }
                }
                catch (Exception ex)
                {
                    LOGGER.Error("Failed to format document", ex);
                }
            });
        }

        void FormatTemplate(EnvDTE.ProjectItem prjItem)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (!prjItem.Saved)
                prjItem.Save();


            if (prjItem.FileCount == 0 && !File.Exists(prjItem.FileNames[0]))
                return;
            var filePath = prjItem.FileNames[0];

            var templateBody = File.ReadAllText(filePath);
            var formattedText = FormatTemplate(templateBody);

            if (string.IsNullOrWhiteSpace(formattedText))
                return;

            File.WriteAllText(filePath, formattedText.Trim());
        }

        void FormatTemplate(EnvDTE.TextDocument txtDocument)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var startPoint = txtDocument.StartPoint.CreateEditPoint();
            var endPoint = txtDocument.EndPoint.CreateEditPoint();

            var text = startPoint.GetText(endPoint);
            if (string.IsNullOrWhiteSpace(text))
                return;

            var formattedText = FormatTemplate(text);

            if (string.IsNullOrWhiteSpace(formattedText))
                return;

            startPoint.ReplaceText(endPoint, formattedText.Trim(), (int)EnvDTE.vsEPReplaceTextOptions.vsEPReplaceTextAutoformat);
        }

        string FormatTemplate(string templateBody)
        {
            try
            {
                var formattedBody = CloudFormation.Parser.ParserUtil.PrettyFormat(templateBody);
                return formattedBody;
            }
            catch (Exception e)
            {
                if (GetService(typeof(SAWSToolkitShellProvider)) is IAWSToolkitShellProvider shell)
                {
                    shell.ShowError("Error", e.Message);
                }

                return null;
            }
        }

#endregion

#region Add CloudFormation Template Command

        void AddCloudFormationTemplate(object sender, EventArgs e)
        {
            uint selectedItemId;
            var hierachyNode = VSUtility.GetCurrentVSHierarchySelection(out selectedItemId);
            var vsProject = VSUtility.GetVsProjectForHierarchyNode(hierachyNode);

            if (vsProject == null)
                return;

            string strLocation = null;
            string strFilter = null;
            int dontShowAgain;
            var projectTypeGuid = GuidList.guidCloudFormationTemplateProjectFactory;

            this.JoinableTaskFactory.Run(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                var addItemDialog = await this.GetServiceAsync(typeof(IVsAddProjectItemDlg)) as IVsAddProjectItemDlg;
                Assumes.Present(addItemDialog);
                addItemDialog.AddProjectItemDlg(selectedItemId, ref projectTypeGuid, vsProject,
                                                        (uint)__VSADDITEMFLAGS.VSADDITEM_AddNewItems, "",
                                                         @"AWS CloudFormation\AWS CloudFormation Template", ref strLocation, ref strFilter, out dontShowAgain);
            });
        }

        void AddCloudFormationTemplate_BeforeQueryStatus(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Visible = false;

            try
            {
                if (!CloudFormationPluginAvailable)
                    return;

                uint selectedItemId;
                var hierarchyNode = VSUtility.GetCurrentVSHierarchySelection(out selectedItemId);
                if (hierarchyNode == null)
                    return;

                var ap = hierarchyNode as Microsoft.VisualStudio.Shell.Flavor.IVsAggregatableProjectCorrected;
                // we have a catch block, but testing non-null smooths debugging
                if (ap != null)
                {
                    var projTypeGuids = string.Empty;
                    ap.GetAggregateProjectTypeGuids(out projTypeGuids);
                    if (projTypeGuids.ToUpper().Contains(GuidList.guidCloudFormationTemplateProjectFactoryString.ToUpper()))
                    {
                        menuCommand.Visible = true;
                    }
                }
            }
            catch { }
        }

#endregion


#region Team Explorer Command

        private void AddTeamExplorerConnection(object sender, EventArgs e)
        {
            ConnectController.OpenConnection();
        }

#endregion


#region IVsPackage Members

        int IVsPackage.Close()
        {
            _documentEventWatcher?.Dispose();
            if (_toolkitCredentialInitializer != null)
            {
                _toolkitCredentialInitializer.AwsConnectionManager.ConnectionStateChanged -= AwsConnectionManager_ConnectionStateChanged;
            }
            _toolkitCredentialInitializer?.Dispose();

            _telemetryManager?.TelemetryLogger?.RecordSessionEnd(new SessionEnd());
            _telemetryManager?.Dispose();

            _telemetryInfoBarManager?.Dispose();
            _supportedVersionBarManager?.Dispose();
            _notificationInfoBarManager?.Dispose();
            _metricsOutputWindow?.Dispose();
            _toolkitOutputWindow?.Dispose();

            return Microsoft.VisualStudio.VSConstants.S_OK;
        }

#endregion

#region IVsInstalledProduct Members

        int IVsInstalledProduct.IdBmpSplash(out uint pIdBmp)
        {
            // no longer called since 2005; IdIcoLogoForAboutbox used for splash and about box
            pIdBmp = 0;
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.IdIcoLogoForAboutbox(out uint pIdIco)
        {
            pIdIco = 400;
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.OfficialName(out string pbstrName)
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pbstrName = string.Format("{0} {1}", fileVersionInfo.FileDescription, ToolkitShellProviderService.HostInfo.Version);

            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductDetails(out string pbstrProductDetails)
        {
            var fileVersionInfo= FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pbstrProductDetails = string.Format(GetResourceString("@112"),
                                                    fileVersionInfo.FileDescription,
                                                    ToolkitShellProviderService.HostInfo.Version,
                                                    fileVersionInfo.LegalCopyright);
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductID(out string pbstrPID)
        {
            pbstrPID = this.GetType().Assembly.GetName().Version.ToString();
            return VSConstants.S_OK;
        }

        #endregion

        #region IAWSToolkitShellThemeService

        // platformui namespace reference in the xaml file needs to be to a specific
        // shell version, and you can't just include references to prior shell version
        // assemblies at the project level otherwise package attribute definitions collide,
        // so we include a specific named copy of each file with just the namespace
        // changed (oh for nested xaml files!)
        static readonly Uri _vsshellThemeOverridesUri = new Uri(GetDefaultThemeUri(),
                                                                UriKind.RelativeOrAbsolute);

        static string GetDefaultThemeUri()
        {
            try
            {
                //Example: /AWSToolkitPackage;component/Themes/_AWSToolkitDefaultTheme.15.0.xaml;
                var assemblyName = typeof(AWSToolkitPackage).Assembly.GetName().Name;
                return $"/{assemblyName};component/Themes/_AWSToolkitDefaultTheme.15.0.xaml";
            }
            catch (Exception e)
            {
                LOGGER.Error("Unable to determine the assembly hosting Theme details. Toolkit may not be functional.", e);

                // Make an attempt at returning what worked for VS 2017/2019
                return "/AWSToolkitPackage;component/Themes/_AWSToolkitDefaultTheme.15.0.xaml";
            }
        }

        public void QueryShellThemeOverrides(out IEnumerable<Uri> apply, out IEnumerable<Uri> remove)
        {
            apply = null;
            remove = null;

            try
            {
                var a = new List<Uri> { _vsshellThemeOverridesUri };
                apply = a;
            }
            catch (Exception)
            {
            }
        }

        public object CaptionFontFamilyKey => ThemeFontResources.CaptionFontFamilyKey;
        public object CaptionFontSizeKey => ThemeFontResources.CaptionFontSizeKey;
        public object CaptionFontWeightKey => ThemeFontResources.CaptionFontWeightKey;

        public object EnvironmentBoldFontWeightKey => ThemeFontResources.EnvironmentBoldFontWeightKey;
        public object EnvironmentFontFamilyKey => ThemeFontResources.EnvironmentFontFamilyKey;
        public object EnvironmentFontSizeKey => ThemeFontResources.EnvironmentFontSizeKey;

        public object Environment122PercentFontSizeKey => ThemeFontResources.Environment122PercentFontSizeKey;

        public object Environment122PercentFontWeightKey => ThemeFontResources.Environment122PercentFontWeightKey;

        public object Environment133PercentFontSizeKey => ThemeFontResources.Environment133PercentFontSizeKey;

        public object Environment133PercentFontWeightKey => ThemeFontResources.Environment133PercentFontWeightKey;

        public object Environment155PercentFontSizeKey => ThemeFontResources.Environment155PercentFontSizeKey;

        public object Environment155PercentFontWeightKey => ThemeFontResources.Environment155PercentFontWeightKey;

        public object Environment200PercentFontSizeKey => ThemeFontResources.Environment200PercentFontSizeKey;

        public object Environment200PercentFontWeightKey => ThemeFontResources.Environment200PercentFontWeightKey;

        public object Environment310PercentFontSizeKey => ThemeFontResources.Environment310PercentFontSizeKey;

        public object Environment310PercentFontWeightKey => ThemeFontResources.Environment310PercentFontWeightKey;

        public object Environment375PercentFontSizeKey => ThemeFontResources.Environment375PercentFontSizeKey;

        public object Environment375PercentFontWeightKey => ThemeFontResources.Environment375PercentFontWeightKey;

        #endregion

        #region IVsPackageExtensionProvider Members

        object IVsPackageExtensionProvider.CreateExtensionInstance(ref Guid extensionPoint, ref Guid instance)
        {
            if (extensionPoint == typeof(Microsoft.VisualStudio.Shell.CodeContainerManagement.ICodeContainerProvider).GUID)
            {
                if (instance == typeof(CodeCommitCodeContainerProvider).GUID)
                {
                    return new CodeCommitCodeContainerProvider(_toolkitContext, this);
                }

                if (instance == typeof(CodeCatalystCodeContainerProvider).GUID)
                {
                    return new CodeCatalystCodeContainerProvider(_toolkitContext, this);
                }
            }

            return null;
        }

        #endregion

        #region IRegisterDataConnectionService

        public void AddDataConnection(DatabaseTypes type, string connectionName, string connectionString)
        {
            var decMgr = (Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager)GetService(typeof(Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager));
            Assumes.Present(decMgr);
            var guidProvider = new Guid("91510608-8809-4020-8897-fba057e22d54");
            var conn = decMgr.AddConnection(connectionName, guidProvider, connectionString, false);
            decMgr.SelectConnection(conn);

            this.JoinableTaskFactory.Run(async () =>
            {
                await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                var toolWindowGuid = new Guid(ToolWindowGuids.ServerExplorer);
                IVsWindowFrame toolWindow;
                var uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
                Assumes.Present(uiShell);
                if (uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out toolWindow) == VSConstants.S_OK)
                {
                    toolWindow.Show();
                }
            });
        }

        public void RegisterDataConnection(DatabaseTypes type, string connectionPrefixName, string host, int port, string masterUsername, string initialDBName)
        {
            var dlgFactory = GetService(typeof(Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialogFactory)) as Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialogFactory;
            Assumes.Present(dlgFactory);

            var dlg = dlgFactory.CreateConnectionDialog();

            switch (type)
            {
                case DatabaseTypes.SQLServer:
                    SetupSqlServerConnectionDialog(dlg, host, port, masterUsername);
                    break;
                case DatabaseTypes.MySQL:
                    SetupMySqlConnectionDialog(dlg, host, port, masterUsername, initialDBName);
                    break;
                default:
                    return;
            }

            if (dlg.ShowDialog())
            {
                // Retrieve the IVsDataExplorerConnectionManager service
                var decMgr = GetService(typeof(Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager)) as Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager;
                Assumes.Present(decMgr);

                var dbName = "unknown";
                if (DatabaseTypes.SQLServer == type)
                {
                    var startPos = dlg.DisplayConnectionString.IndexOf("Initial Catalog=");
                    if (startPos != -1)
                    {
                        startPos += "Initial Catalog=".Length;
                        int endPos = dlg.DisplayConnectionString.IndexOf(";", startPos);
                        dbName = endPos == -1 ? dlg.DisplayConnectionString.Substring(startPos) : dlg.DisplayConnectionString.Substring(startPos, endPos - startPos);

                        dbName = dbName.Trim();
                    }
                }
                else
                {
                    dbName = initialDBName;
                }


                var connectionName = string.Format("{0}.{1}", connectionPrefixName, dbName);
                // Add a connection node to the server explorer
                var conn = decMgr.AddConnection(connectionName, dlg.SelectedProvider, dlg.EncryptedConnectionString, true);
                //string.Format("Data Source={0},{1};Initial Catalog={2};User Id={3};Password={4};",
                //host, port, "norm_test", masterUsername, "testtest"), false);
                decMgr.SelectConnection(conn);

                this.JoinableTaskFactory.Run(async () =>
                {
                    await this.JoinableTaskFactory.SwitchToMainThreadAsync();
                    var toolWindowGuid = new Guid(ToolWindowGuids.ServerExplorer);
                    var uiShell = await GetServiceAsync(typeof(SVsUIShell)) as IVsUIShell;
                    Assumes.Present(uiShell);
                    if (uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out IVsWindowFrame toolWindow) == VSConstants.S_OK)
                    {
                        toolWindow.Show();
                    }
                });
            }

            dlg.Dispose();
        }


        static void SetupSqlServerConnectionDialog(Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialog dlg, string host, int port, string masterUsername)
        {
            // This is the guid that identifies the Provider (see HKLM\SOFTWARE\Microsoft\VisualStudio\9.0\DataProviders
            var provider = new Guid("91510608-8809-4020-8897-fba057e22d54");
            var technology = new Guid("77AB9A9D-78B9-4ba7-91AC-873F5338F1D2");
            var source = new Guid("067EA0D9-BA62-43f7-9106-34930C60C528");

            dlg.AddSources(technology);
            dlg.SelectedSource = source;

            var initialConnectionString = string.Format("Data Source={0},{1};User Id={2};Password=;Trusted_Connection=False", host, port, masterUsername);
            dlg.LoadExistingConfiguration(provider, initialConnectionString, false);
        }

        static void SetupMySqlConnectionDialog(Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialog dlg, string host, int port, string masterUsername, string dbName)
        {
            // This is the guid that identifies the Provider (see HKLM\SOFTWARE\Microsoft\VisualStudio\9.0\DataProviders
            var provider = new Guid("c6882346-e592-4da5-80ba-d2eadcda0359");
            var technology = new Guid("77AB9A9D-78B9-4ba7-91AC-873F5338F1D2");
            var source = new Guid("98FBE4D8-5583-4233-B219-70FF8C7FBBBD");

            try
            {
                dlg.AddSources(technology);
                dlg.SelectedSource = source;

                var initialConnectionString = string.Format("Server={0};Port={1};Uid={2};Database={3};", host, port, masterUsername, dbName);
                dlg.LoadExistingConfiguration(provider, initialConnectionString, false);
            }
            catch (Exception e)
            {
                throw RegisterDataConnectionException.CreateMySQLMissingProvider(e);
            }
        }

#endregion
		
#region IVsBroadcastMessageEvents

        const int WM_WININICHANGE = 0x001A;
        const int WM_DISPLAYCHANGE = 0x007E;
        const int WM_SYSCOLORCHANGE = 0x0015;
        const int WM_PALETTECHANGED = 0x0311;
        const int WM_PALETTEISCHANGING = 0x0310;
        const int WM_ACTIVATEAPP = 0x001C;

        public int OnBroadcastMessage(uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == WM_SYSCOLORCHANGE)
            {
                ThemeUtil.RaiseThemeChangeEvent();
                return VSConstants.S_OK;
            }

            return VSConstants.S_OK;
        }


#endregion
    }
}
