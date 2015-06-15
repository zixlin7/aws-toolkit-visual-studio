using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Amazon.AWSToolkit.CommonUI.DeploymentWizard;
using EnvDTE;
using Microsoft.Samples.VisualStudio.IDE.OptionsPage;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.CommonUI.LegacyDeploymentWizard.Persistence;

using Amazon.AWSToolkit.CloudFormation;
using Amazon.AWSToolkit.ElasticBeanstalk;
using Amazon.AWSToolkit.Lambda;

using Amazon.AWSToolkit.VisualStudio.ToolWindow;
using Amazon.AWSToolkit.VisualStudio.HostedEditor;

using Amazon.AWSToolkit.VisualStudio.Registration;

using Amazon.AWSToolkit.VisualStudio.Shared;
using Amazon.AWSToolkit.VisualStudio.Shared.BuildProcessors;
using Amazon.AWSToolkit.VisualStudio.Shared.DeploymentProcessors;
using Amazon.AWSToolkit.VisualStudio.Shared.Loggers;
using Amazon.AWSToolkit.VisualStudio.Shared.ServiceInterfaces;

using Amazon.AWSToolkit.VisualStudio.Services;

using log4net;
using Amazon.AWSToolkit.PluginServices.Deployment;

using Microsoft.VisualStudio.Project;
using Window = System.Windows.Window;
using System.ComponentModel;
using Amazon.AWSToolkit.MobileAnalytics;

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
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration(true, null, null, null)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // This attribute registers a tool window exposed by this package.
    [ProvideToolWindow(typeof(AWSNavigatorToolWindow),
                       Style = VsDockStyle.Tabbed,
                       Orientation = ToolWindowOrientation.Left,
                       Transient = false,
                       Window = ToolWindowGuids80.ServerExplorer)]
    [ProvideEditorExtension(typeof(HostedEditorFactory), ".hostedControl", 50, 
              ProjectGuid = "{A2FE74E1-B743-11d0-AE1A-00A0C90FFFC3}", 
              TemplateDir = "Templates", 
              NameResourceID = 105,
              DefaultName = "Amazon.AWSToolkit.VisualStudio")]
    [ProvideKeyBindingTable(GuidList.guid_VSPackageEditorFactoryString, 102)]
    [ProvideEditorLogicalView(typeof(HostedEditorFactory), "{7651a703-06e5-11d1-8ebd-00a0c90f26ea}")]
    [ProvideEditorFactory(typeof(Amazon.AWSToolkit.VisualStudio.CloudFormationEditor.TemplateEditorFactory), 113)]
    [ProvideEditorExtension(typeof(Amazon.AWSToolkit.VisualStudio.CloudFormationEditor.TemplateEditorFactory),
          ".template", 10000, NameResourceID = 113)]
    [Guid(GuidList.guid_VSPackageString)]
    [AWSCommandLineRegistration(CommandLineToken = awsToolkitPluginsParam, DemandLoad = false, Arguments = 1)]
    [ProvideService(typeof(SAWSToolkitService))]
    // request autoload when user has a solution open so we can enable PublishToCloudFormation 
    // command based on selected project type and retrieve last-deployed options for projects
    // in the solution
    //[ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.SolutionExists_string)]
    [ProvideAutoLoad(Microsoft.VisualStudio.VSConstants.UICONTEXT.NoSolution_string)] // now need to force load when VS starts for CFN editor project stuff
    [ProvideProjectFactory(
        typeof(CloudFormationEditor.CloudFormationTemplateProjectFactory),
        null,
        "CloudFormation Template Project Files (*.cfproj);*.cfproj",
        "cfproj", "cfproj",
        ".\\NullPath",
        LanguageVsTemplate = "AWS")]
    [ProvideOptionPageAttribute(typeof(GeneralOptionsPage), "AWS Toolkit", "General", 150, 160, true)]
    [ProvideProfileAttribute(typeof(GeneralOptionsPage), "AWS Toolkit", "General", 150, 160, true, DescriptionResourceID = 150)]
    [ProvideOptionPageAttribute(typeof(ProxyOptionsPage), "AWS Toolkit", "Proxy", 150, 170, true)]
    [ProvideProfileAttribute(typeof(ProxyOptionsPage), "AWS Toolkit", "Proxy", 150, 170, true, DescriptionResourceID = 150)]
    public sealed class AWSToolkitPackage : ProjectPackage, IShellProvider, IShellProviderThemeService, IVsInstalledProduct, IRegisterDataConnectionService, IVsShellPropertyEvents, IVsBroadcastMessageEvents, IVsPackage
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AWSToolkitPackage));

        // registered VS command line param, /awsToolkitPlugins c:\path1[;c:\path2;c:\path3...]
        internal const string awsToolkitPluginsParam = "awsToolkitPlugins";

        AWSToolkitService _toolkitService;
        IAWSCloudFormation _cloudformationPlugin;
        IAWSElasticBeanstalk _beanstalkPlugin;
        IAWSLambda _lambdaPlugin;

        readonly NavigatorVsUIHierarchy _hier;
        readonly Dispatcher _shellDispatcher;

        // allows us to filter out undesirable solution elements from publishing enablement
        readonly Guid _guidSolutionFolderProject = new Guid(0x2150e333, 0x8fdc, 0x42a3, 0x94, 0x74, 0x1a, 0x39, 0x56, 0xd4, 0x6d, 0xe8);
        
        Guid _guidAwsOutputWindowPane = new Guid(0xb1c55205, 0xa332, 0x4dcb, 0xbf, 0xa0, 0x86, 0x62, 0x7f, 0x6d, 0x4, 0x86 );
        IVsOutputWindowPane _awsOutputWindowPane;

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

        private SimpleMobileAnalytics recorder;

        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public AWSToolkitPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
            Utility.ConfigureLog4Net();

            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(Utility.AssemblyResolveEventHandler);

            this._hier = new NavigatorVsUIHierarchy();
            this._shellDispatcher = Dispatcher.CurrentDispatcher;

            var serviceContainer = this as IServiceContainer;
            var callback = new ServiceCreatorCallback(CreateService);
            serviceContainer.AddService(typeof(SAWSToolkitService), callback, true);

            // .suo persistence keys must be registered during ctor according to docs
            AddOptionKey(DeploymentsPersistenceTag);           // this one used to store all cf/beanstalk deployment data going forward
            AddOptionKey(DeprecatedDeploymentsPersistenceTag); // so we can migrate prior version
			
            try
            {
                var vsShell = (IVsShell)GetGlobalService(typeof(SVsShell));
                uint cookie;
                vsShell.AdviseBroadcastMessages(this, out cookie);
            }
            catch (Exception e)
            {
                LOGGER.Warn("Failed to register for broadcast messages, theme change will not be detected", e);
            }

            //recorder = new SimpleMobileAnalytics();
            recorder = SimpleMobileAnalytics.Instance;
        }

        public ILog Logger { get { return LOGGER; } }

        public override string ProductUserContext
        {
            get
            {
                return null;
            }
        }

        public void OutputToConsole(string message, bool forceVisible)
        {
            // we may be calling OutputStringThreadSafe but the COM object represented by
            // _awsOutputWindowPane appears to still need to be called on the UI thread
            this.ShellDispatcher.Invoke((Action) (() =>
            {
                if (_awsOutputWindowPane == null)
                {
                    try
                    {
                        var output = (IVsOutputWindow) GetService(typeof (SVsOutputWindow));
                        if (output != null && output.CreatePane(ref _guidAwsOutputWindowPane,
                            "Amazon Web Services",
                            Convert.ToInt32(forceVisible),
                            Convert.ToInt32(true)) == VSConstants.S_OK)
                            output.GetPane(ref _guidAwsOutputWindowPane, out _awsOutputWindowPane);
                    }
                    catch (Exception e)
                    {
                        LOGGER.ErrorFormat("Caught exception trying to get SVsOutputWindow service and/or create AWS output window pane.");
                        LOGGER.ErrorFormat("Exception message: {0}", e.Message);
                    }

                    if (_awsOutputWindowPane == null)
                    {
                        LOGGER.ErrorFormat("Failed to obtain IVsOutputWindow instance and/or create AWS output window pane to show message '{0}'.", message);
                        return;
                    }
                }
                else if (forceVisible)
                    _awsOutputWindowPane.Activate();

                try
                {
                    _awsOutputWindowPane.OutputStringThreadSafe(string.Format("{0}\r\n", message));
                }
                catch (Exception e)
                {
                    LOGGER.ErrorFormat("Caught exception calling IVsOutputWindow.OutputStringThreadSafe.");
                    LOGGER.ErrorFormat("Exception message: '{0}', original message content '{1}'", e.Message, message);
                }
            }));
        }

        IAWSCloudFormation AWSCloudFormationPlugin
        {
            get
            {
                try
                {
                    if (_cloudformationPlugin == null)
                    {
                        InstantiateToolkitService();
                        _cloudformationPlugin = (_toolkitService as IAWSToolkitService).QueryAWSToolkitPluginService(typeof(IAWSCloudFormation))
                            as IAWSCloudFormation;
                    }
                }
                catch (Exception) { }
                return _cloudformationPlugin;
            }
        }

        bool CloudFormationPluginAvailable
        {
            get
            {
                return AWSCloudFormationPlugin != null;
            }
        }

        IAWSLambda LambdaPlugin
        {
            get
            {
                try
                {
                    if (_lambdaPlugin == null)
                    {
                        InstantiateToolkitService();
                        _lambdaPlugin = (_toolkitService as IAWSToolkitService).QueryAWSToolkitPluginService(typeof(IAWSLambda))
                            as IAWSLambda;
                    }
                }
                catch (Exception) { }
                return _lambdaPlugin;
            }
        }

        bool LambdaPluginAvailable
        {
            get
            {
                return LambdaPlugin != null;
            }
        }

        IAWSElasticBeanstalk AWSBeanstalkPlugin
        {
            get
            {
                try
                {
                    if (_beanstalkPlugin == null)
                    {
                        InstantiateToolkitService();
                        _beanstalkPlugin = (_toolkitService as IAWSToolkitService).QueryAWSToolkitPluginService(typeof(IAWSElasticBeanstalk))
                            as IAWSElasticBeanstalk;
                    }
                }
                catch (Exception) { }
                return _beanstalkPlugin;
            }
        }

        bool BeanstalkPluginAvailable
        {
            get
            {
                return AWSBeanstalkPlugin != null;
            }
        }

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            if (typeof(SAWSToolkitService) == serviceType)
            {
                InstantiateToolkitService();
                return _toolkitService;
            }

            return null;
        }

        void InstantiateToolkitService()
        {
            lock (this)
            {
                if (_toolkitService == null)
                {
                    LOGGER.Debug("Creating SAWSToolkitService service");
                    _toolkitService = new AWSToolkitService(this);
                }
            }
        }

        /// <summary>
        /// This function is called when the user clicks the menu item that shows the 
        /// tool window. See the Initialize method to see how the menu item is associated to 
        /// this function using the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void ShowToolWindow(object sender, EventArgs e)
        {
            ShowExplorerWindow();
        }

        void ShowExplorerWindow()
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = FindToolWindow(typeof(AWSNavigatorToolWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException(Resources.CanNotCreateWindow);
            }
            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this));
            base.Initialize();
            RegisterProjectFactory(new CloudFormationEditor.CloudFormationTemplateProjectFactory(this));

            // ranu build of package deploys us outside of build folder hierarchy, so toolkit's
            // default plugin load fails - use a command line switch so dev's can inform toolkit
            // of where to go look
            var additionalPluginFolders = string.Empty;
            var vsCmdLine = GetService(typeof(SVsAppCommandLine)) as IVsAppCommandLine;
            if (vsCmdLine != null)
            {
                int optPresent;
                string optValue;
                if (vsCmdLine.GetOption(awsToolkitPluginsParam, out optPresent, out optValue) == VSConstants.S_OK && optPresent != 0)
                    additionalPluginFolders = optValue as string;
            }

            // TODO: load hosted files location from store and set in S3FileFetcher.HostedFilesLocation

            var navigator = new NavigatorControl();
            ToolkitFactory.InitializeToolkit(navigator, this, additionalPluginFolders);
			
            var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));

            ThemeUtil.Initialize(dte.Version);			

            //Create Editor Factory. Note that the base Package class will call Dispose on it.
            RegisterEditorFactory(new HostedEditorFactory(this));

            // Add our command handlers for menu (commands must exist in the .vsct file)
            var mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                // Create the command for the tool window
                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidAWSNavigator, ShowToolWindow, null);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidPublishToAWS, PublishToAWS, PublishMenuCommand_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdIdRepublishToAWS, RepublishToAWS, RepublishMenuCommand_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidDeployTemplateSolutionExplorer, DeployTemplateSolutionExplorer, TemplateCommandSolutionExplorer_BeforeQueryStatus);
                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidDeployTemplateActiveDocument, DeployTemplateActiveDocument, TemplateCommandActiveDocument_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidEstimateTemplateCostSolutionExplorer, EstimateTemplateCostSolutionExplorer, TemplateCommandSolutionExplorer_BeforeQueryStatus);
                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidEstimateTemplateCostActiveDocument, EstimateTemplateCostActiveDocument, TemplateCommandActiveDocument_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidFormatTemplateSolutionExplorer, FormatTemplateSolutionExplorer, TemplateCommandSolutionExplorer_BeforeQueryStatus);
                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidFormatTemplateActiveDocument, FormatTemplateActiveDocument, TemplateCommandActiveDocument_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidAddCloudFormationTemplate, AddCloudFormationTemplate, AddCloudFormationTemplate_BeforeQueryStatus);

                SetupMenuCommand(mcs, GuidList.guid_VSPackageCmdSet, PkgCmdIDList.cmdidDeployToLambdaSolutionExplorer, UploadToLambda, UploadToLambdaMenuCommand_BeforeQueryStatus);
            }

            /* temp disabled whilst we investigate sporadic crashes under VS2013U4
            // register for shell property changes so we get notification that shell has completed load and can
            // run our 'first 5 mins' setup
            var shellService = GetService(typeof(SVsShell)) as IVsShell;
            if (shellService != null)
            {
                ErrorHandler.ThrowOnFailure(shellService.AdviseShellPropertyChanges(this, out _vsShellPropertyChangeEventSinkCookie));
            }*/

            RegisterEditorFactory(new CloudFormationEditor.TemplateEditorFactory(this));
        }

        /// <summary>
        /// Listener watching for transition to initialized state in shell, so we know VS is fully loaded
        /// </summary>
        /// <param name="propid"></param>
        /// <param name="propValue"></param>
        /// <returns></returns>
        public int OnShellPropertyChange(int propid, object propValue)
        {
            if ((int)__VSSPROPID4.VSSPROPID_ShellInitialized == propid)
            {
                var propertyValue = (bool)propValue;
                LOGGER.InfoFormat("Received __VSSPROPID4.VSSPROPID_ShellInitialized property change, new value {0}", propertyValue);

                if (propertyValue)
                {
                    var shellService = GetService(typeof (SVsShell)) as IVsShell;
                    if (shellService != null)
                    {
                        ErrorHandler.ThrowOnFailure(shellService.UnadviseShellPropertyChanges(_vsShellPropertyChangeEventSinkCookie));
                    }

                    _vsShellPropertyChangeEventSinkCookie = 0;

                    // see if the toolkit wants to run a 'first 5 minutes' setup dialog
                    this.ShellDispatcher.Invoke((Action) (() =>
                    {
                        try
                        {
                            ToolkitFactory.Instance.RunFirstTimeSetup();
                        }
                        catch (Exception e)
                        {
                            LOGGER.ErrorFormat("Caught exception on first-run setup, message {0}, stack {1}", e.Message, e.StackTrace);
                        }
                    }));
                }
            }

            return VSConstants.S_OK;
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
        void PublishMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            var publishMenuCommand = sender as OleMenuCommand;
            publishMenuCommand.Visible = false;

            try
            {
                if (CloudFormationPluginAvailable || BeanstalkPluginAvailable)
                {
                    var pi = VSUtility.SelectedWebProject;
                    publishMenuCommand.Visible
                        = (pi != null
                            && pi.VsProjectType != VSWebProjectInfo.VsWebProjectType.NotWebProjectType);
                    publishMenuCommand.Enabled = !_performingDeployment;
                }
            }
            catch (Exception)
            {
            }
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

        /// <summary>
        /// Start the Publish2AWS wizard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void PublishToAWS(object sender, EventArgs e)
        {
            try
            {
                if (_msdeployInstallVerified != true)
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

                var pi = VSUtility.SelectedWebProject;
                if (pi == null || pi.VsProjectType == VSWebProjectInfo.VsWebProjectType.NotWebProjectType)
                    return;

                IDictionary<string, object> wizardProperties;
                var ret = InitializeAndRunDeploymentWizard(pi, out wizardProperties);
                if (!ret)
                {
                    if (wizardProperties.ContainsKey(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode))
                    {
                        // if the user cancelled the new wizard and requested the legacy version, run it instead
                        ret = InitializeAndRunLegacyDeploymentWizard(pi, out wizardProperties);
                    }
                }

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
        /// Runs the new deployment wizard. The user can cancel this and request the legacy wizard, in which
        /// case the output properties need to contain the propkey_LegacyDeploymentMode key (all other property
        /// info is ignored).
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="wizardProperties"></param>
        /// <returns>True if the wizard ran to completion. False if the user cancelled or requested the legacy wizard.</returns>
        bool InitializeAndRunDeploymentWizard(VSWebProjectInfo projectInfo, out IDictionary<string, object> wizardProperties)
        {
            var wizard = AWSWizardFactory.CreateStandardWizard("Deploy2AWS", null);
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

            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            uiShell.GetDialogOwnerHwnd(out parent);

            var ret = wizard.Run();
            wizardProperties = wizard.CollectedProperties;

            return ret;
        }

        /// <summary>
        /// Runs the legacy deployment wizard. We inject the propkey_LegacyDeploymentMode key into the seed properties
        /// so that plugins can determine which style of pages to inject.
        /// </summary>
        /// <param name="projectInfo"></param>
        /// <param name="wizardProperties"></param>
        /// <returns>True if the wizard ran to completion, false if the user cancelled</returns>
        bool InitializeAndRunLegacyDeploymentWizard(VSWebProjectInfo projectInfo, out IDictionary<string, object> wizardProperties)
        {
            var wizard = AWSWizardFactory.CreateStandardWizard("LegacyDeploy2AWS", null);
            wizard.Title = "Publish to Amazon Web Services";

            wizard.SetProperty(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode, true);

            SetVSToolkitDeploymentSeedData(wizard, projectInfo);

            // fix the build configuration to deploy from the seed property we just set, so the legacy and new
            // wizards can share the same build logic
            var activeBuildConfiguration = wizard[DeploymentWizardProperties.SeedData.propkey_ActiveBuildConfiguration] as string;
            wizard[DeploymentWizardProperties.AppOptions.propkey_SelectedBuildConfiguration] = activeBuildConfiguration;

            var pageControllers = new List<IAWSWizardPageController>
            {
                new CommonUI.WizardPages.PageControllers.AccountRegistrationPageController(),
                new CommonUI.LegacyDeploymentWizard.PageControllers.DeploymentTemplateSelectorPageController()
            };

            // the legacy wizard does not use page groups
            var cloudFormation = AWSCloudFormationPlugin;
            if (cloudFormation != null)
            {
                var collatedPages = cloudFormation.DeploymentService.ConstructDeploymentPages(wizard, false);
                pageControllers.AddRange(collatedPages);
            }

            var beanstalk = AWSBeanstalkPlugin;
            if (beanstalk != null)
            {
                var collatedPages = beanstalk.DeploymentService.ConstructDeploymentPages(wizard, false);
                pageControllers.AddRange(collatedPages);
            }

            pageControllers.Add(new CommonUI.LegacyDeploymentWizard.PageControllers.DeploymentReviewPageController());
            wizard.RegisterPageControllers(pageControllers, 0);
            wizard.SetShortCircuitPage(AWSWizardConstants.WizardPageReferences.LastPageID);

            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            uiShell.GetDialogOwnerHwnd(out parent);

            var ret = wizard.Run();
            wizardProperties = wizard.CollectedProperties;

            return ret;
        }

        /// <summary>
        /// Start republishing the project to the last-used CloudFormation stack or Elastic Beanstalk environment
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void RepublishToAWS(object sender, EventArgs e)
        {
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

                ShowMessage("Not Available", msg);
                return;
            }

            // use a one-page wizard to have the same look as the standard deployment wizard
            var wizard = AWSWizardFactory.CreateStandardWizard("Redeploy2AWS", null);
            SetVSToolkitFastRedeploymentSeedData(wizard, pi, deploymentHistory);

            var pageControllers = new List<IAWSWizardPageController>();

            // fast-track republish does not use page groups
            if (deploymentHistory is BeanstalkDeploymentHistory)
            {
                wizard.SetProperty(DeploymentWizardProperties.SeedData.propkey_LegacyDeploymentMode, false);

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

            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            uiShell.GetDialogOwnerHwnd(out parent);

            if (wizard.Run())
            {
                // persist first so we take advantage of any save actions during build
                PersistVSToolkitDeployment(pi, wizard.CollectedProperties);
                BuildAndDeployProject(pi, wizard.CollectedProperties);
            }
        }

        #endregion

        #region Template Command Query Status

        void TemplateCommandSolutionExplorer_BeforeQueryStatus(object sender, EventArgs e)
        {
            TemplateCommand_BeforeQueryStatus(sender as OleMenuCommand, false);
        }

        void TemplateCommandActiveDocument_BeforeQueryStatus(object sender, EventArgs e)
        {
            TemplateCommand_BeforeQueryStatus(sender as OleMenuCommand, true);
        }

        void TemplateCommand_BeforeQueryStatus(OleMenuCommand menuCommand, bool activeDocument)
        {
            if (menuCommand == null)
                return;

            menuCommand.Visible = false;

            try
            {
                if (CloudFormationPluginAvailable)
                {
                    if (activeDocument)
                    {
                        var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;

                        if (dte.ActiveDocument != null)
                        {
                            var fullName = dte.ActiveDocument.FullName;
                            menuCommand.Visible = fullName.EndsWith(CloudFormation.EditorExtensions.TemplateContentType.Extension);
                        }
                    }
                    else
                    {
                        menuCommand.Visible = VSUtility.SelectedFileHasExtension(CloudFormation.EditorExtensions.TemplateContentType.Extension);
                    }
                }
            }
            catch { }
        }

        #endregion

        #region CloudFormation Template Deployment Command

        void DeployTemplateSolutionExplorer(object sender, EventArgs e)
        {
            try
            {
                var prjItem = VSUtility.GetSelectedProjectItem();
                DeployTemplate(prjItem);
            }
            catch { }
        }

        void DeployTemplateActiveDocument(object sender, EventArgs e)
        {
            try
            {
                var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var prjItem = dte.ActiveDocument.ProjectItem;
                DeployTemplate(prjItem);
            }
            catch { }
        }

        void DeployTemplate(EnvDTE.ProjectItem prjItem)
        {
            if (prjItem == null)
                return;

            if (!prjItem.Saved)
                prjItem.Save();

            string fullPath = null;
            var node = prjItem.Object as FileNode;
            if (node != null && File.Exists(node.Url))
                fullPath = node.Url;
            else if (prjItem.DTE.ActiveDocument != null && prjItem.FileCount > 0 && File.Exists(prjItem.FileNames[0]))
                fullPath = prjItem.FileNames[0];

            Dictionary<string, object> initialParameters = null;
            // Only attempt to look up and store last used values for templates files in an AWS CloudFormation project
            if (node != null && node is CloudFormationEditor.TemplateFileNode)
            {
                var projectGuid = VSUtility.QueryProjectIDGuid(prjItem.ContainingProject);
                initialParameters = SetTemplateDeploymentSeedData(projectGuid, CalcPersistenceKeyForProjectItem(prjItem));
            }

            var templateDeploymentData = this.AWSCloudFormationPlugin.DeployCloudFormationTemplate(fullPath, initialParameters ?? new Dictionary<string, object>());

            if (templateDeploymentData != null && node != null && node is CloudFormationEditor.TemplateFileNode)
            {
                PersistTemplateDeployment(prjItem, templateDeploymentData);
            }
        }

        #endregion

        #region Estimate Template Cost Command

        void EstimateTemplateCostSolutionExplorer(object sender, EventArgs e)
        {
            try
            {
                var prjItem = VSUtility.GetSelectedProjectItem();
                EstimateTemplateCost(prjItem);
            }
            catch { }
        }

        void EstimateTemplateCostActiveDocument(object sender, EventArgs e)
        {
            try
            {
                var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var prjItem = dte.ActiveDocument.ProjectItem;
                EstimateTemplateCost(prjItem);
            }
            catch { }
        }

        void EstimateTemplateCost(EnvDTE.ProjectItem prjItem)
        {
            if (prjItem == null)
                return;

            if (!prjItem.Saved)
                prjItem.Save();

            string fullPath = null;
            var node = prjItem.Object as FileNode;
            if (node != null && File.Exists(node.Url))
                fullPath = node.Url;
            else if (prjItem.DTE.ActiveDocument != null && prjItem.FileCount > 0 && File.Exists(prjItem.FileNames[0]))
                fullPath = prjItem.FileNames[0];

            Dictionary<string, object> initialParameters = null;
            // Only attempt to look up and store last used values for templates files in an AWS CloudFormation project
            if (node != null && node is CloudFormationEditor.TemplateFileNode)
            {
                var projectGuid = VSUtility.QueryProjectIDGuid(prjItem.ContainingProject);
                initialParameters = SetTemplateDeploymentSeedData(projectGuid, CalcPersistenceKeyForProjectItem(prjItem));
            }
            var deploymentData = this.AWSCloudFormationPlugin.GetUrlToCostEstimate(fullPath, initialParameters ?? new Dictionary<string, object>());

            if (deploymentData != null && !string.IsNullOrEmpty(deploymentData.CostEstimationCalculatorUrl))
            {
                prjItem.DTE.ItemOperations.Navigate(deploymentData.CostEstimationCalculatorUrl);

                if (node != null && node is CloudFormationEditor.TemplateFileNode)
                {
                    PersistTemplateDeployment(prjItem, deploymentData);
                }
            }
        }

        #endregion

        #region Format Template Command

        void FormatTemplateSolutionExplorer(object sender, EventArgs e)
        {
            try
            {
                var prjItem = VSUtility.GetSelectedProjectItem();
                if (prjItem == null)
                    return;

                
                if (prjItem.Document != null && prjItem.Document.Object("TextDocument") != null)
                {
                    var txtDocument = (EnvDTE.TextDocument)prjItem.Document.Object("TextDocument");
                    FormatTemplate(txtDocument);
                }
                else
                {
                    FormatTemplate(prjItem);
                }
            }
            catch { }
        }


        void FormatTemplateActiveDocument(object sender, EventArgs e)
        {
            try
            {
                var dte = GetGlobalService(typeof(EnvDTE.DTE)) as EnvDTE.DTE;
                var txtDocument = (EnvDTE.TextDocument)dte.ActiveDocument.Object("TextDocument");
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
            catch { }
        }

        void FormatTemplate(EnvDTE.ProjectItem prjItem)
        {
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
                ShowError("Error", e.Message);
                return null;
            }
        }

        #endregion

        #region Add Upload to Lambda Command

        void UploadToLambda(object sender, EventArgs e)
        {
            if(this.LambdaPluginAvailable)
            {
                var item = VSUtility.GetSelectedProject();
                if(item == null)
                {
                    ShowError("Select Item is not a project that can be deployed to AWS Lambda");
                    return;
                }

                var rootDirectory = Path.GetDirectoryName(item.FullName);

                var seedProperties = new Dictionary<string, string>();
                seedProperties[LambdaContants.SeedSourcePath] = rootDirectory;

                var prop = item.Properties.Item("StartupFile");
                if(prop != null && prop.Value is string)
                {
                    string fullPath = prop.Value as string;
                    string relativePath;
                    if(fullPath.StartsWith(rootDirectory))
                        relativePath = fullPath.Substring(rootDirectory.Length + 1);
                    else
                        relativePath = Path.GetFileName(fullPath);

                    if (!relativePath.StartsWith("_"))
                        seedProperties[LambdaContants.SeedFileName] = relativePath;
                    else
                        seedProperties[LambdaContants.SeedFileName] = "app.js";
                }

                this.LambdaPlugin.UploadFunctionFromPath(seedProperties);
            }
        }

        void UploadToLambdaMenuCommand_BeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
                return;

            menuCommand.Visible = false;

            try
            {
                if (!this.LambdaPluginAvailable)
                    return;

///                menuCommand.Visible = true;

                uint selectedItemId;
                var hierarchyNode = VSUtility.GetCurrentVSHierarchySelection(out selectedItemId);
                if (hierarchyNode == null)
                    return;

                var ap = hierarchyNode as Microsoft.VisualStudio.Shell.Flavor.IVsAggregatableProjectCorrected;
                string projTypeGuids = string.Empty;
                ap.GetAggregateProjectTypeGuids(out projTypeGuids);
                if (string.Equals(GuidList.guidNodeJSConsoleProjectFactoryString, projTypeGuids, StringComparison.InvariantCultureIgnoreCase))
                {
                    menuCommand.Visible = true;
                }
            }
            catch { }
        }

        #endregion

        #region Add CloudFormation Template Command

        void AddCloudFormationTemplate(object sender, EventArgs e)
        {
            uint selectedItemId;
            var hierachyNode = VSUtility.GetCurrentVSHierarchySelection(out selectedItemId);
            var vsProject = GetVsProjectForHierarchyNode(hierachyNode);

            if (vsProject == null)
                return;
            
            string strLocation = null;
            string strFilter = null;
            int dontShowAgain;
            var projectTypeGuid = GuidList.guidCloudFormationTemplateProjectFactory;

            var addItemDialog = this.GetService(typeof(IVsAddProjectItemDlg)) as IVsAddProjectItemDlg;
            addItemDialog.AddProjectItemDlg(selectedItemId, ref projectTypeGuid, vsProject,
                                                    (uint)__VSADDITEMFLAGS.VSADDITEM_AddNewItems, "",
                                                     @"AWS CloudFormation\AWS CloudFormation Template", ref strLocation, ref strFilter, out dontShowAgain);

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
                var projTypeGuids = string.Empty;
                ap.GetAggregateProjectTypeGuids(out projTypeGuids);
                if (projTypeGuids.ToUpper().Contains(GuidList.guidCloudFormationTemplateProjectFactoryString.ToUpper()))
                {
                    menuCommand.Visible = true;
                }
            }
            catch { }
        }

        #endregion

        /// <summary>
        /// To allow for templates in sub folders with the same name, attempt to make the full path for
        /// the template into a relative path. If we cannot make it relative, give up and use just the
        /// filename we have.
        /// </summary>
        /// <param name="prjItem"></param>
        /// <returns></returns>
        string CalcPersistenceKeyForProjectItem(EnvDTE.ProjectItem prjItem)
        {
            string fileNameKey = null;
            try
            {
                var projPath = prjItem.ContainingProject.Properties.Item("FullPath").Value.ToString();
                var filePath = prjItem.Properties.Item("FullPath").Value.ToString();

                fileNameKey = PackageUtilities.MakeRelative(projPath, filePath);
                if (Path.IsPathRooted(fileNameKey))
                    fileNameKey = null;
            }
            catch (Exception e)
            {
                Logger.DebugFormat("Exception attempting to set relative filepath for template {0} - {1}. Filename will be used for persistence key", 
                                   (prjItem.Object as FileNode).FileName, 
                                   e.Message);
            }
            finally
            {
                if (string.IsNullOrEmpty(fileNameKey))
                    fileNameKey = (prjItem.Object as FileNode).FileName;
            }

            return fileNameKey;
        }

        /// <summary>
        /// Persists data returned from the deployment of a template to CloudFormation
        /// </summary>
        /// <param name="persistableData"></param>
        void PersistTemplateDeployment(EnvDTE.ProjectItem prjItem, DeployedTemplateData persistableData)
        {
            if (persistableData == null)
                return;

            var projectGuid = VSUtility.QueryProjectIDGuid(prjItem.ContainingProject);
            var cftpi = GetPersistedInfoForService(projectGuid, DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.CFNTemplateDeployment)
                            as CloudFormationTemplatePersistenceInfo;
            cftpi.AccountUniqueID = persistableData.Account.SettingsUniqueKey;
            cftpi.LastRegionDeployedTo = persistableData.Region.SystemName;

            var persistenceKey = CalcPersistenceKeyForProjectItem(prjItem);
            var tdh = new TemplateDeploymentHistory(persistenceKey, persistableData.StackName, persistableData.TemplateProperties);
            cftpi.PreviousDeployments.AddDeployment(tdh);
            _projectDeployments.SetLastServiceDeploymentForProject(projectGuid,
                                                                    DeploymentServiceIdentifiers.CloudFormationServiceName,
                                                                    DeploymentTypeIdentifiers.CFNTemplateDeployment);
        }


        Dictionary<string, object> SetTemplateDeploymentSeedData(string projectGuid, string templateUri)
        {
            var seedData = new Dictionary<string, object>();
            if (_projectDeployments.PersistedDeployments(projectGuid) > 0)
            {
                var deployments = _projectDeployments[projectGuid];
                var cftpi = deployments.DeploymentForService(DeploymentServiceIdentifiers.CloudFormationServiceName, DeploymentTypeIdentifiers.CFNTemplateDeployment)
                                as CloudFormationTemplatePersistenceInfo;
                if (cftpi != null)
                {
                    var deploymentHistory = cftpi.PreviousDeployments;
                    if (deploymentHistory != null)
                    {
                        var tdh = deploymentHistory.DeploymentForTemplate(templateUri);
                        if (tdh != null)
                        {
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_SeedAccountGuid, cftpi.AccountUniqueID);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_LastRegionDeployedTo, cftpi.LastRegionDeployedTo);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_SeedName, tdh.LastStack);
                            seedData.Add(DeploymentWizardProperties.SeedData.propkey_TemplateProperties, tdh.TemplateProperties);
                        }
                    }
                }
            }

            return seedData;
        }

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

                SeedAvailableBuildConfigurations(projectInfo, seedProperties);

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
                if (!seedProperties.ContainsKey(DeploymentWizardProperties.SeedData.propkey_SeedName))
                {
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
                }

                // seed the suggested .NET framework needed for the application from project properties, if available
                var targetFramework = projectInfo.TargetFramework;
                if (!string.IsNullOrEmpty(targetFramework))
                {
                    if (targetFramework.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
                        targetFramework = targetFramework.Substring(1);
                }
                else
                {
                    if (ShellName == Constants.VS2012HostShell.ShellName
                            || ShellName == Constants.VS2013HostShell.ShellName
                            || ShellName == Constants.VS2015HostShell.ShellName)
                        targetFramework = VSWebProjectInfo.FrameworkVersionV45;
                    else
                        targetFramework = VSWebProjectInfo.FrameworkVersionV40;
                }
                seedProperties.Add(DeploymentWizardProperties.AppOptions.propkey_TargetFramework, targetFramework);

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
                }

                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_PreviousDeployments, previousDeployments);

                var seedVersion = DateTime.Now.ToUniversalTime().ToString("yyyyMMddHHmmss");
                seedProperties.Add(DeploymentWizardProperties.SeedData.propkey_SeedVersionLabel, string.Format("v{0}", seedVersion));
            }
            catch { }
            finally
            {
                var targetFramework = projectInfo.TargetFramework;
                if (string.IsNullOrEmpty(targetFramework))
                {
                    if (ShellName == Constants.VS2012HostShell.ShellName
                            || ShellName == Constants.VS2013HostShell.ShellName)
                        targetFramework = VSWebProjectInfo.FrameworkVersionV45;
                    else
                        targetFramework = VSWebProjectInfo.FrameworkVersionV40;
                }
                seedProperties.Add(DeploymentWizardProperties.AppOptions.propkey_TargetFramework, targetFramework);
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
        /// <param name="projectInfo"></param>
        /// <param name="seedProperties"></param>
        void SeedAvailableBuildConfigurations(VSWebProjectInfo projectInfo, Dictionary<string, object> seedProperties)
        {
            var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
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
                    var selectedAccount
                        = wizardProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                    cfppi.AccountUniqueID = selectedAccount.SettingsUniqueKey;

                    var region = (wizardProperties[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                                as RegionEndPointsManager.RegionEndPoints).SystemName;
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
                    var selectedAccount = wizardProperties[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;

                    bppi.AccountUniqueID = selectedAccount.SettingsUniqueKey;

                    var region = (wizardProperties[CommonWizardProperties.AccountSelection.propkey_SelectedRegion]
                                                as RegionEndPointsManager.RegionEndPoints).SystemName;

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

        ProjectDeploymentsPersistenceManager.PersistedProjectInfoBase GetPersistedInfoForService(string projectGuid, string serviceName, string deploymentType)
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
                bdc = new BeanstalkBuildAndDeployController(Dispatcher.CurrentDispatcher)
                {
                    ServicePlugin = AWSBeanstalkPlugin,
                    DeploymentProcessor = new BeanstalkDeploymentProcessor()
                };
            }

            bdc.HostServiceProvider = GetService;
            bdc.BuildProcessor = projectInfo.VsProjectType == VSWebProjectInfo.VsWebProjectType.WebApplicationProject
                                        ? new WebAppProjectBuildProcessor() as IBuildProcessor
                                        : new WebSiteProjectBuildProcessor() as IBuildProcessor;
            bdc.ProjectInfo = projectInfo;
            bdc.Options = wizardProperties;
            bdc.Logger = new IDEConsoleLogger(_toolkitService);
            bdc.OnCompletionCallback = BuildAndDeploymentCompleted;

            bdc.Execute();
        }

        // callback from build/deployment sequencer is done on our UI thread
        void BuildAndDeploymentCompleted(bool succeeded)
        {
            _performingDeployment = false;
        }

        readonly Dictionary<uint, WeakReference> _controlCache = new Dictionary<uint, WeakReference>();
        int _controlCounter = 0;
        public IAWSControl PopControl(uint controlId)
        {
            IAWSControl control = null;
            WeakReference reference;
            if (_controlCache.TryGetValue(controlId, out reference))
            {
                if (reference.IsAlive)
                {
                    control = reference.Target as IAWSControl;
                }
                this._controlCache.Remove(controlId);
            }

            return control;
        }

        string _knownShell = null;
        public string ShellName
        {
            get
            {
                if (string.IsNullOrEmpty(_knownShell))
                {
                    var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                    if (dte != null) // null can happen during initialization
                    {
                        if (dte.Version.StartsWith("10"))
                            _knownShell = Constants.VS2010HostShell.ShellName;
                        else if (dte.Version.StartsWith("11"))
                            _knownShell = Constants.VS2012HostShell.ShellName;
                        else if (dte.Version.StartsWith("12"))
                            _knownShell = Constants.VS2013HostShell.ShellName;
                        else
                            _knownShell = Constants.VS2015HostShell.ShellName;
                    }
                }

                return _knownShell ?? Constants.VS2010HostShell.ShellName;
            }
        }

        string _shellVersion = null;
        public string ShellVersion
        {
            get
            {
                if (string.IsNullOrEmpty(_shellVersion))
                {
                    var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                    if (dte != null) // null can happen during initialization
                    {
                        if (dte.Version.StartsWith("10"))
                            _shellVersion = "2010";
                        else if (dte.Version.StartsWith("11"))
                            _shellVersion = "2012";
                        else
                            _shellVersion = "2013";
                    }
                }

                return _shellVersion ?? "2010";
            }
        }

        public void OpenShellWindow(ShellWindows window)
        {
            switch (window)
            {
                case ShellWindows.Explorer:
                    ShellDispatcher.Invoke((Action)(this.ShowExplorerWindow));
                    break;

                case ShellWindows.Output:
                    break;
            }

        }

        readonly Dictionary<string, WeakReference> _openedEditors = new Dictionary<string, WeakReference>();
        public void OpenInEditor(IAWSControl editorControl)
        {

            var openShell = GetService(typeof(IVsUIShellOpenDocument)) as IVsUIShellOpenDocument;

            recorder.AddProperty(Attributes.OpenViewFullIdentifier, editorControl.GetType().FullName);
            recorder.RecordEventWithProperties();

            var logicalView = VSConstants.LOGVIEWID_Primary;
            var editorFactoryGuid = new Guid(GuidList.guid_VSPackageEditorFactoryString);

            var controlId = (uint)(++_controlCounter);
            _controlCache[controlId] = new WeakReference(editorControl);
            try
            {
                var uniqueId = editorControl.UniqueId;
                if (ToolkitFactory.Instance.Navigator.SelectedAccount != null)
                {
                    uniqueId += ToolkitFactory.Instance.Navigator.SelectedAccount.SettingsUniqueKey;
                }
                if (ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints != null)
                {
                    uniqueId += ToolkitFactory.Instance.Navigator.SelectedRegionEndPoints.SystemName;
                }

                string filename;
                if (!_controlUniqueNameToFileName.TryGetValue(uniqueId, out filename))
                {
                    filename = Guid.NewGuid() + ".hostedControl";
                    _controlUniqueNameToFileName[uniqueId] = filename;
                    _openedEditors[filename] = new WeakReference(editorControl);
                }
                else if (_openedEditors.ContainsKey(filename))
                {
                    var wr = _openedEditors[filename];
                    if (wr.IsAlive)
                    {
                        var existingOpenEditor = wr.Target as IAWSControl;
                        existingOpenEditor.RefreshInitialData(editorControl.GetInitialData());
                    }
                }

                IVsWindowFrame frame;
                var result = openShell.OpenSpecificEditor(
                    0,  // grfOpenSpecific 
                    getTempFileLocation() + "/" + filename, // pszMkDocument 
                    ref editorFactoryGuid,  // rGuidEditorType 
                    null, // pszPhysicalView 
                    ref logicalView, // rguidLogicalView +++
                    editorControl.Title, // pszOwnerCaption 
                    _hier, // pHier 
                    controlId, // itemid 
                    new IntPtr(0), // punkDocDataExisting 
                    null, // pSPHierContext 
                    out frame);

                if (result != VSConstants.S_OK)
                {
                    _controlCache.Remove(controlId);
                    Trace.WriteLine(result);
                }
                else
                {
                    frame.Show();
                }
            }
            catch
            {
                _controlCache.Remove(controlId);
            }

            ThreadPool.QueueUserWorkItem(ClearDeadWeakReferences, null);

        }

        public void OpenInEditor(string fileName)
        {
            try
            {
                var dte = (EnvDTE.DTE)GetService(typeof(EnvDTE.DTE));
                dte.ItemOperations.OpenFile(fileName, EnvDTE.Constants.vsViewKindTextView);
            }
            catch (Exception e)
            {
                ShowError(string.Format("Failed to open file {0}, exception message {1}", fileName, e.Message));
            }
        }

        void ClearDeadWeakReferences(object stateInfo)
        {
            var toBeDeleted = (from kvp in this._openedEditors where !kvp.Value.IsAlive select kvp.Key).ToList();

            foreach (var key in toBeDeleted)
            {
                _openedEditors.Remove(key);
            }
        }

        readonly Dictionary<string, string> _controlUniqueNameToFileName = new Dictionary<string, string>();
        private string getTempFileLocation()
        {
            var folder = Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + "/.aws/temp";
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);
            return folder;
        }


        public bool ShowModal(IAWSControl hostedControl)
        {
            return ShowModal(hostedControl, MessageBoxButton.OKCancel);
        }

        public bool ShowModal(IAWSControl hostedControl, MessageBoxButton buttons)
        {
            var host = DialogHostUtil.CreateDialogHost(buttons, hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public bool ShowModal(Window window, string metricId)
        {
            recorder.AddProperty(Attributes.OpenViewFullIdentifier, metricId);
            recorder.RecordEventWithProperties();

            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            if (uiShell.GetDialogOwnerHwnd(out parent) != VSConstants.S_OK)
            {
                Trace.Fail("Failed to get hwnd for ShowModal: " + window.Title);
                return false;
            }

            try
            {
                window.HorizontalAlignment = HorizontalAlignment.Center;
                window.VerticalAlignment = VerticalAlignment.Center;

                var wih = new WindowInteropHelper(window);
                wih.Owner = parent;

                uiShell.EnableModeless(0);
                var dialogResult = window.ShowDialog().GetValueOrDefault();
                return dialogResult;
            }
            catch (Exception e)
            {
                Trace.Fail("Error displaying modal dialog: " + e.Message);
                return false;
            }
            finally
            {
                uiShell.EnableModeless(1);
            }
        }

        public bool ShowModalFrameless(IAWSControl hostedControl)
        {
            var host = DialogHostUtil.CreateFramelessDialogHost(hostedControl);
            return ShowModal(host, hostedControl.MetricId);
        }

        public void ShowError(string message)
        {
            ShowError("Error", message);
        }

        public void ShowError(string title, string message)
        {
            this.ShellDispatcher.Invoke((Action)(() => MessageBox.Show(GetParentWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Error)));
        }

        public void ShowErrorWithLinks(string title, string message)
        {
            this.ShellDispatcher.Invoke((Action)(() => Messaging.ShowErrorWithLinks(GetParentWindow(), title, message)));
        }

        public void ShowMessage(string title, string message)
        {
            this.ShellDispatcher.Invoke((Action)(() => MessageBox.Show(GetParentWindow(), message, title, MessageBoxButton.OK, MessageBoxImage.Information)));
        }

        public void OutputToHostConsole(string message)   
        {
            OutputToConsole(message, false);
        }

        public void OutputToHostConsole(string message, bool forceVisible)
        {
            OutputToConsole(message, forceVisible);
        }

        Window GetParentWindow()
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            if (uiShell.GetDialogOwnerHwnd(out parent) != VSConstants.S_OK)
            {
                return null;
            }
            var host = new Window();
            var wih = new WindowInteropHelper(host) {Owner = parent};

            return host;
        }


        public void UpdateStatus(string status)
        {
            try
            {
                var statusBar = (IVsStatusbar)GetService(typeof(SVsStatusbar));
                int frozen;

                statusBar.IsFrozen(out frozen);

                if (frozen == 0)
                {
                    if (string.IsNullOrEmpty(status))
                        statusBar.Clear();
                    else
                        statusBar.SetText(status);
                }
            }
            catch (Exception) { }
        }

        public bool Confirm(string title, string message)
        {
            return Confirm(title, message, MessageBoxButton.YesNo);
        }

        public bool Confirm(string title, string message, MessageBoxButton buttons)
        {
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            IntPtr parent;
            if (uiShell.GetDialogOwnerHwnd(out parent) == VSConstants.S_OK)
            {
                var host = new Window();
                var wih = new WindowInteropHelper(host);
                wih.Owner = parent;

                var result = MessageBox.Show(host, message, title, buttons, MessageBoxImage.Exclamation);
                return result == MessageBoxResult.Yes || result == MessageBoxResult.OK;
            }

            Trace.Fail("Failed to get hwnd for error message: " + message);
            return false;
        }

        public Dispatcher ShellDispatcher
        {
            get { return _shellDispatcher; }
        }

        public T QueryShellProverService<T>() where T : class
        {
            return this as T;
        }

        #region IShellProviderThemeService

        void IShellProviderThemeService.ThemeWizard(IAWSWizard wizard)
        {
            wizard.SetProperty(AWSWizardConstants.WizardOptions.propkey_NavContainerBackground, VsBrushes.EnvironmentBackgroundKey);
        }

        // The vsshell theme overrides for all VS shell placements are supplemented with additional overrides for colors in
        // VS2010 and VS2012+ dark themes to fine tune appearance.
        static readonly Uri _vsshellThemeOverridesUri = new Uri("/AWSToolkitPackage;component/Themes/_VSShellThemeOverrides.xaml",
                                                                UriKind.RelativeOrAbsolute);
        static readonly Uri _darkThemeSupplementaryUri = new Uri("/AWSToolkitPackage;component/Themes/_DarkThemeSupplementary.xaml",
                                                                 UriKind.RelativeOrAbsolute);
        static readonly Uri _vs2010ThemeSupplementaryUri = new Uri("/AWSToolkitPackage;component/Themes/_VS2010ThemeSupplementary.xaml",
                                                                   UriKind.RelativeOrAbsolute);

        void IShellProviderThemeService.QueryShellThemeOverrides(out IEnumerable<Uri> apply, out IEnumerable<Uri> remove)
        {
            apply = null;
            remove = null;

            try
            {
                var a = new List<Uri> { _vsshellThemeOverridesUri };
                apply = a;

                if (ShellVersion == "2010")
                    a.Add(_vs2010ThemeSupplementaryUri);
                else
                {
                    if (ThemeUtil.GetCurrentTheme() == VsTheme.Dark)
                        a.Add(_darkThemeSupplementaryUri);
                    else
                    {
                        var r = new List<Uri> { _darkThemeSupplementaryUri };
                        remove = r;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        #endregion
        
        /// <summary>
        /// Checks whether the specified project is a solution folder
        /// </summary>
        public bool IsSolutionFolderProject(IVsHierarchy pHier)
        {
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
        }

        /// <summary>
        /// This method loads a localized string based on the specified resource.
        /// </summary>
        /// <param name="resourceName">Resource to load</param>
        /// <returns>String loaded for the specified resource</returns>
        internal string GetResourceString(string resourceName)
        {
            string resourceValue;
            var resourceManager = (IVsResourceManager)GetService(typeof(SVsResourceManager));
            if (resourceManager == null)
            {
                throw new InvalidOperationException("Could not get SVsResourceManager service. Make sure the package is Sited before calling this method");
            }
            var packageGuid = this.GetType().GUID;
            var hr = resourceManager.LoadResourceString(ref packageGuid, -1, resourceName, out resourceValue);
            ErrorHandler.ThrowOnFailure(hr);
            return resourceValue;
        }

        public static IVsProject GetVsProjectForHierarchyNode(IVsHierarchy hierachyNode)
        {
            IVsProject vsProject = null;
            if (hierachyNode is IVsProject)
                vsProject = hierachyNode as IVsProject;
            else if (hierachyNode is FolderNode)
            {
                IVsHierarchy node;
                uint projectItemId;
                ((FolderNode)hierachyNode).NodeProperties.GetProjectItem(out node, out projectItemId);
                vsProject = node as IVsProject;
            }

            return vsProject;
        }

        #region IVsPackage Members

        int IVsPackage.Close()
        {
            SimpleMobileAnalytics.Instance.StopSession();
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
            pbstrName = string.Format("{0} {1}", fileVersionInfo.FileDescription, ShellVersion);

            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductDetails(out string pbstrProductDetails)
        {
            var fileVersionInfo= FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            pbstrProductDetails = string.Format(GetResourceString("@112"),
                                                    fileVersionInfo.FileDescription,
                                                    ShellVersion,
                                                    fileVersionInfo.LegalCopyright);
            return VSConstants.S_OK;
        }

        int IVsInstalledProduct.ProductID(out string pbstrPID)
        {
            pbstrPID = this.GetType().Assembly.GetName().Version.ToString();
            return VSConstants.S_OK;
        }

        #endregion

        #region IRegisterDataConnectionService

        public void AddDataConnection(DatabaseTypes type, string connectionName, string connectionString)
        {
            var decMgr = (Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager)GetService(typeof(Microsoft.VisualStudio.Data.Services.IVsDataExplorerConnectionManager));
            var guidProvider = new Guid("91510608-8809-4020-8897-fba057e22d54");
            var conn = decMgr.AddConnection(connectionName, guidProvider, connectionString, false);
            decMgr.SelectConnection(conn);

            var toolWindowGuid = new Guid(ToolWindowGuids.ServerExplorer);
            IVsWindowFrame toolWindow;
            var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            if (uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out toolWindow) == VSConstants.S_OK)
            {
                toolWindow.Show();
            }
        }

        public void RegisterDataConnection(DatabaseTypes type, string connectionPrefixName, string host, int port, string masterUsername, string initialDBName)
        {
            var dlgFactory = GetService(typeof(Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialogFactory)) as Microsoft.VisualStudio.Data.Services.IVsDataConnectionDialogFactory;

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

                var toolWindowGuid = new Guid(ToolWindowGuids.ServerExplorer);
                IVsWindowFrame toolWindow;
                var uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
                if (uiShell.FindToolWindow((uint)__VSFINDTOOLWIN.FTW_fForceCreate, ref toolWindowGuid, out toolWindow) == VSConstants.S_OK)
                {
                    toolWindow.Show();
                }
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
