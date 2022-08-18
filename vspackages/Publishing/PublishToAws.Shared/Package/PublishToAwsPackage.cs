using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Install;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.PublishSetting;
using Amazon.AWSToolkit.Publish.Services;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Publish.Package
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PublishToAwsPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideService(typeof(SInitializePublishToAwsPackage), IsAsyncQueryable = true)]
    [ProvideService(typeof(SPublishToAws), IsAsyncQueryable = true)]
    [ProvideService(typeof(SCliServer), IsAsyncQueryable = true)]
    // TODO : Expose other services from this package
    // [ProvideService(typeof(SFoo), IsAsyncQueryable = true)]
    public sealed class PublishToAwsPackage : AsyncPackage, IPublishToAwsPackage
    {
        /// <summary>
        /// PublishToAwsPackage GUID string.
        /// </summary>
        public const string PackageGuidString = Constants.PublishToAwsPackageGuidStr;

        static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAwsPackage));

        private IInitializePublishToAwsPackage _initializePublishToAwsPackage;
        private CliServer _cliServer;

        private PublishContext _publishContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublishToAwsPackage"/> class.
        /// </summary>
        public PublishToAwsPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            try
            {
                Logger.Debug("Initializing PublishToAws Package");

                // When initialized asynchronously, the current thread may be a background thread at this point.
                // Do any initialization that requires the UI thread after switching to the UI thread.
                await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                // Enable the Main Toolkit package to load this package by requesting an initialization service
                this.AddService(typeof(SInitializePublishToAwsPackage), CreatePackageServicesAsync, true);

                Logger.Debug("Finished Initializing PublishToAws Package");
            }
            catch (Exception e)
            {
                Logger.Error("PublishToAws Package failed to initialize - Publish functionality may not be available", e);
            }
        }

        private async Task<object> CreatePackageServicesAsync(
            IAsyncServiceContainer container,
            CancellationToken cancellationToken,
            Type serviceType)
        {
            if (serviceType == typeof(SInitializePublishToAwsPackage))
            {
                if (_initializePublishToAwsPackage == null)
                {
                    _initializePublishToAwsPackage = new InitializePublishToAwsPackage();
                    _initializePublishToAwsPackage.Initialize += OnPackageInitialize;
                }

                return _initializePublishToAwsPackage;
            }

            if (serviceType == typeof(SCliServer))
            {
                if (_cliServer == null)
                {
                    _cliServer = await CliServerFactory.CreateAsync(_publishContext.InstallOptions,
                        _publishContext.PublishSettingsRepository, _publishContext.ToolkitShellProvider);
                }

                return _cliServer;
            }

            if (serviceType == typeof(SPublishToAws))
            {
                return new PublishToAws(_publishContext);
            }

            return null;
        }

        private void OnPackageInitialize(object sender, EventArgs e)
        {
            if (sender is InitializePublishToAwsPackage initializer)
            {
                var installOptions = InstallOptionsFactory.Create(initializer.ToolkitContext.ToolkitHostInfo);

                _publishContext = new PublishContext()
                {
                    PublishPackage = this,
                    ToolkitContext = initializer.ToolkitContext,
                    ToolkitShellProvider = initializer.ShellProvider,
                    InstallOptions = installOptions,
                    PublishSettingsRepository = new FilePublishSettingsRepository(),
                    InitializeCliTask = InitializeDeployCliInBackgroundAsync(installOptions, initializer.ToolkitContext)
                };

                // You only initialize once
                _initializePublishToAwsPackage.Initialize -= OnPackageInitialize;

                // TODO : Enable other services to be created from this Package now
                AddService(typeof(SPublishToAws), CreatePackageServicesAsync, true);
                AddService(typeof(SCliServer), CreatePackageServicesAsync, false);
            }
        }

        private async Task InitializeDeployCliInBackgroundAsync(InstallOptions installOptions, ToolkitContext toolkitContext)
        {
            await TaskScheduler.Default;
            await new DeployCli(installOptions, toolkitContext).InitializeAsync(DisposalToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (_initializePublishToAwsPackage != null)
            {
                _initializePublishToAwsPackage.Initialize -= OnPackageInitialize;
            }

            _cliServer?.Dispose();

            base.Dispose(disposing);
        }
    }
}
