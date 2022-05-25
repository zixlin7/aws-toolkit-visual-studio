using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Threading;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.Views
{
    public partial class PublishToAwsDocumentControl : BaseAWSControl, IDisposable
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAwsDocumentControl));

        private readonly PublishApplicationContext _publishContext;
        private JoinableTaskFactory JoinableTaskFactory => _publishContext.PublishPackage.JoinableTaskFactory;
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _connectionRestartTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _publishSummaryTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _requiredPublishPropertiesTokenSource = new CancellationTokenSource();
        private readonly ResettableCancellationToken _reloadConfigurationsTokenSource = new ResettableCancellationToken();
        private readonly PublishToAwsDocumentViewModel _viewModel;
        private readonly Stack<IDisposable> _disposables = new Stack<IDisposable>();

        private readonly PublishDocumentViewModelChangeHandler _viewModelChangeHandler =
            new PublishDocumentViewModelChangeHandler();

        public override string Title => $"Publish to AWS: {_viewModel.ProjectName}";
        public override string UniqueId => $"AWS:Publish:{_viewModel.ProjectName}";

        public override bool IsUniquePerAccountAndRegion => false;

        /// <summary>
        /// Constructor used by XAML Viewer at design time
        /// </summary>
        public PublishToAwsDocumentControl()
            : this(new ShowPublishToAwsDocumentArgs(), null, null)
        {
        }

        /// <summary>
        /// Constructor used by Toolkit at run time
        /// </summary>
        public PublishToAwsDocumentControl(ShowPublishToAwsDocumentArgs args, PublishApplicationContext publishContext, ICliServer cliServer)
        {
            _publishContext = publishContext;

            _disposables.Push(_cancellationTokenSource);
            _disposables.Push(_connectionRestartTokenSource);
            _disposables.Push(_publishSummaryTokenSource);
            _disposables.Push(_requiredPublishPropertiesTokenSource);
            _disposables.Push(_reloadConfigurationsTokenSource);

            _viewModel = CreateViewModel(args, cliServer);

            _viewModel.Connection.StartListeningToConnectionManager();
            publishContext.ConnectionManager.ConnectionStateChanged += ConnectionManagerOnConnectionStateChanged;
            publishContext.ConnectionManager.ChangeConnectionSettings(args.CredentialId, GetInitialRegion(_viewModel, args));

            InitializeComponent();

            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            _viewModel.WorkflowDuration.Start();
            Loaded += PublishToAwsDocumentControl_Loaded;
            Unloaded += PublishToAwsDocumentControl_Unloaded;
        }

        private void PublishToAwsDocumentControl_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.Connection.StartListeningToConnectionManager();
        }

        private void PublishToAwsDocumentControl_Unloaded(object sender, System.Windows.RoutedEventArgs e)
        {
            _viewModel.Connection.StopListeningToConnectionManager();
        }

        private PublishToAwsDocumentViewModel CreateViewModel(ShowPublishToAwsDocumentArgs args, ICliServer cliServer)
        {
            var viewModel = new PublishToAwsDocumentViewModel(_publishContext)
            {
                ProjectName = args.ProjectName,
                ProjectPath = args.ProjectPath,
                ProjectGuid = args.ProjectGuid,
                ViewStage = PublishViewStage.Target,
            };
            viewModel.LoadPublishSettings();

            var configurationDetailFactory = new ConfigurationDetailFactory(viewModel.Connection, _publishContext.ToolkitShellProvider.GetDialogFactory());
            var client = cliServer.GetRestClient(viewModel.GetCredentialsAsync);
            viewModel.DeployToolController = new DeployToolController(client, configurationDetailFactory);
            viewModel.DeploymentClient = cliServer.GetDeploymentClient();

            var publishCommand = new PublishCommand(viewModel, _publishContext.ToolkitShellProvider);
            var configCommand = new ConfigCommand(viewModel);
            var targetCommand = new TargetCommand(viewModel);
            var startOverCommand = new StartOverCommand(viewModel, this);
            var optionsCommand = new PersistBannerVisibilityCommand(_publishContext.PublishSettingsRepository, viewModel);
            var closeFailureBannerCommand = CloseFailureBannerCommandFactory.Create(viewModel.PublishProjectViewModel, JoinableTaskFactory);
            var artifactViewerCommand = DeploymentArtifactViewerCommand.Create(viewModel);
            var copyToClipboardCommand = CopyToClipboardCommand.Create(viewModel.PublishProjectViewModel, _publishContext.ToolkitShellProvider);

            _disposables.Push(publishCommand);
            _disposables.Push(configCommand);
            _disposables.Push(targetCommand);
            _disposables.Push(startOverCommand);

            viewModel.PublishToAwsCommand = publishCommand;
            viewModel.ConfigTargetCommand = configCommand;
            viewModel.BackToTargetCommand = targetCommand;
            viewModel.Connection.SelectCredentialsCommand = SelectCredentialsCommandFactory.Create(viewModel);
            viewModel.PublishProjectViewModel.StartOverCommand = startOverCommand;
            viewModel.PersistOptionsSettingsCommand = optionsCommand;
            viewModel.PublishProjectViewModel.CloseFailureBannerCommand = closeFailureBannerCommand;
            viewModel.PublishProjectViewModel.ViewPublishedArtifactCommand = artifactViewerCommand;
            viewModel.PublishProjectViewModel.CopyToClipboardCommand = copyToClipboardCommand;

            return viewModel;
        }

        private static ToolkitRegion GetInitialRegion(
            PublishToAwsDocumentViewModel viewModel, ShowPublishToAwsDocumentArgs args)
        {
            return viewModel.IsValidRegion(args.Region)
                ? args.Region
                : viewModel.GetValidRegion(args.CredentialId);
        }

        public void Dispose()
        {
            _viewModel.WorkflowDuration.Stop();
            Logger.Debug($"Disposing Publish dialog: {_viewModel.ProjectName}");

            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _publishContext.ConnectionManager.ConnectionStateChanged -= ConnectionManagerOnConnectionStateChanged;

            _requiredPublishPropertiesTokenSource.Cancel();
            _connectionRestartTokenSource.Cancel();
            _publishSummaryTokenSource.Cancel();
            _cancellationTokenSource.Cancel();

            while (_disposables.Count > 0)
            {
                var disposable = _disposables.Pop();
                disposable.Dispose();
            }

            var isPublished = _viewModel.ViewStage == PublishViewStage.Publish;
            _viewModel.RecordPublishEndMetric(isPublished);
            _viewModel.RecordPublishUnsupportedSettingMetric();

            Logger.Debug($"Disposed Publish dialog: {_viewModel.ProjectName}");
        }

        public override bool CanClose()
        {
            if (_viewModel.PublishProjectViewModel.IsPublishing)
            {
                return _publishContext.ToolkitShellProvider.Confirm("Publish In Progress",
                    $"Publish will continue in the background but you will not see any updates.{Environment.NewLine}Are you sure you want to close?");
            }

            JoinableTaskFactory.Run(async () =>
            {
                await TeardownDeploymentSessionAsync().ConfigureAwait(false);
            });

            return true;
        }

        private void ConnectionManagerOnConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                CancelConnectionRestartInProgress();
                var cancellationToken = _connectionRestartTokenSource.Token;

                _viewModel.IsLoading = !e.State.IsTerminal;

                if (e.State.IsTerminal)
                {
                    JoinableTaskFactory.RunAsync(async () =>
                    {
                        await OnTerminalConnectionManagerStateAsync(e.State, cancellationToken).ConfigureAwait(false);
                    }, JoinableTaskCreationOptions.LongRunning).Task.LogExceptionAndForget();
                }
            });
        }

        private async Task OnTerminalConnectionManagerStateAsync(ConnectionState connectionState,
            CancellationToken cancellationToken)
        {
            try
            {
                _viewModel.ErrorMessage = string.Empty;
                if (connectionState is ConnectionState.ValidConnection)
                {
                    await RestartDeploymentSessionAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _publishContext.ToolkitShellProvider.OutputToHostConsole(
                        $"Publish {_viewModel?.ProjectName} to AWS does not have a valid credentials-region combination.{Environment.NewLine}Select valid credentials and try again.{Environment.NewLine}{connectionState.Message}.",
                        true);

                    await _viewModel.StopDeploymentSessionAsync(cancellationToken);
                    await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                    _viewModel.Recommendations.Clear();
                    _viewModel.RepublishTargets.Clear();
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing publish state", e);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(
                    $"Publish to AWS failed to load publish details using the current credentials:{Environment.NewLine}{e.Message}", true);

                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _viewModel.ErrorMessage = e.Message;
            }
        }

        private async Task RestartDeploymentSessionAsync(CancellationToken cancellationToken)
        {
            using (_viewModel.CreateLoadingScope())
            {
                await TaskScheduler.Default;
                await _viewModel.RestartDeploymentSessionAsync(cancellationToken).ConfigureAwait(false);
                await LoadPublishTargetsAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task LoadPublishTargetsAsync(CancellationToken cancellationToken)
        {
            try
            {
                await TaskScheduler.Default;
                // query appropriate recommendations
                await _viewModel.InitializePublishTargetsAsync(cancellationToken);

                // pre-select the re-publish option if there are any existing publish targets
                var targetSelectionMode = _viewModel.RepublishTargets.Any() ? TargetSelectionMode.ExistingTargets : TargetSelectionMode.NewTargets;
                await _viewModel.SetTargetSelectionModeAsync(targetSelectionMode, cancellationToken);

                _viewModel.SetPublishTargetsLoaded(true);
            }
            catch (PublishException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new PublishException("Unable to load publish targets. Please try re-opening the Publish to AWS experience.", e);
            }
        }

        private async Task TeardownDeploymentSessionAsync()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;

                    await _viewModel.StopDeploymentSessionAsync(tokenSource.Token)
                        .ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Failed to close deployment session", e);
            }
        }

        /// <summary>
        /// Creates a cancellation token source representing the token state
        /// of this view, and the publish package.
        /// 
        /// Caller is responsible for disposing the created token source.
        /// </summary>
        private CancellationTokenSource CreateCancellationTokenSource()
        {
            return CancellationTokenSource.CreateLinkedTokenSource(
                _cancellationTokenSource.Token,
                _publishContext.PublishPackage.DisposalToken);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (AffectsSummary(e.PropertyName))
            {
                CancelSummaryUpdatesInProgress();
                _viewModel.UpdateSummaryAsync(_publishSummaryTokenSource.Token).LogExceptionAndForget();
            }
            if (AffectsPublishProperties(e.PropertyName))
            {
                OnPublishAffectingPropertiesUpdated();
            }
            if (e.PropertyName == nameof(PublishToAwsDocumentViewModel.ConfigurationDetails))
            {
                if (_viewModel.ConfigurationDetails != null)
                {
                    foreach (var config in _viewModel.ConfigurationDetails)
                    {
                        // Prevent double-registrations
                        config.DetailChanged -= Config_DetailChanged;
                        config.DetailChanged += Config_DetailChanged;
                    }
                }
            }
        }

        private void OnPublishAffectingPropertiesUpdated()
        {
            UpdateRequiredPublishProperties();
            UpdateConfigurationAsync().LogExceptionAndForget();
        }

        private async Task UpdateConfigurationAsync()
        {
            using (_viewModel.CreateLoadingScope())
            {
                ResetConfigDetails();
                await _viewModel.SetSystemCapabilitiesAsync(Enumerable.Empty<TargetSystemCapability>().ToList(),
                    CancellationToken.None);

                if (_viewModelChangeHandler.IsTargetRefreshNeeded(_viewModel))
                {
                    await ReloadTargetConfigurationsAsync();
                }
            }
        }

        private void UpdateRequiredPublishProperties()
        {
            CancelRequiredPublishUpdatesInProgress();
            _viewModel.UpdateRequiredPublishPropertiesAsync(_requiredPublishPropertiesTokenSource.Token).LogExceptionAndForget();
        }

        private void ResetConfigDetails()
        {
            if (_viewModel.ConfigurationDetails != null)
            {
                foreach (var config in _viewModel.ConfigurationDetails)
                {
                    config.DetailChanged -= Config_DetailChanged;
                }

                _viewModel.ConfigurationDetails = null;
            }
        }

        private void Config_DetailChanged(object sender, DetailChangedEventArgs e)
        {
            SetTargetConfigurationAsync(e.Detail).LogExceptionAndForget();
            _viewModel.IsDefaultConfig = false;
        }

        private void CancelConnectionRestartInProgress()
        {
            _connectionRestartTokenSource.Cancel();
            _connectionRestartTokenSource.Dispose();
            _connectionRestartTokenSource = new CancellationTokenSource();
        }

        private void CancelSummaryUpdatesInProgress()
        {
            _publishSummaryTokenSource.Cancel();
            _publishSummaryTokenSource.Dispose();
            _publishSummaryTokenSource = new CancellationTokenSource();
        }

        private void CancelRequiredPublishUpdatesInProgress()
        {
            _requiredPublishPropertiesTokenSource.Cancel();
            _requiredPublishPropertiesTokenSource.Dispose();
            _requiredPublishPropertiesTokenSource = new CancellationTokenSource();
        }

        private async Task ReloadTargetConfigurationsAsync()
        {
            try
            {
                var cancelToken = _reloadConfigurationsTokenSource.Reset();

                await TaskScheduler.Default;
                await _viewModel.SetDeploymentTargetAsync(cancelToken).ConfigureAwait(false);

                var loadTargetConfigurationTask = LoadTargetConfigurationsAsync(cancelToken);
                var reloadSystemCapabilitiesTask = ReloadSystemCapabilitiesAsync(cancelToken);
                await Task.WhenAll(loadTargetConfigurationTask, reloadSystemCapabilitiesTask).ConfigureAwait(false);

                _viewModel.IsDefaultConfig = true;
            }
            catch (Exception e)
            {
                Logger.Error("Error reloading configuration details", e);
            }
        }

        private async Task ReloadSystemCapabilitiesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await TaskScheduler.Default;
                await _viewModel.RefreshSystemCapabilitiesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error reloading system capabilities", e);
            }
        }

        private async Task LoadTargetConfigurationsAsync(CancellationToken cancellationToken)
        {
            using (_viewModel.CreateLoadingScope())
            {
                await TaskScheduler.Default;
                await _viewModel.RefreshTargetConfigurationsAsync(cancellationToken).ConfigureAwait(false);
                if (_viewModel.ViewStage == PublishViewStage.Configure)
                {
                    await _viewModel.UpdateConfigurationViewModelAsync();
                }
            }
        }

        private async Task SetTargetConfigurationAsync(ConfigurationDetail configurationDetail)
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                using (_viewModel.CreateLoadingScope())
                {
                    await TaskScheduler.Default;
                    var validationResult = await _viewModel.SetTargetConfigurationAsync(configurationDetail, tokenSource.Token)
                        .ConfigureAwait(false);

                    if (validationResult.HasErrors())
                    {
                        configurationDetail.ApplyValidationErrors(validationResult);
                    }
                    else
                    {
                        await LoadTargetConfigurationsAsync(tokenSource.Token);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error setting configuration of type {configurationDetail.Name}", e);
            }
        }

        private bool AffectsSummary(string propertyName)
        {
            return PublishToAwsDocumentViewModel.SummaryAffectingProperties.Contains(propertyName);
        }

        private bool AffectsPublishProperties(string propertyName)
        {
            return PublishToAwsDocumentViewModel.PublishAffectingProperties.Contains(propertyName);
        }
    }
}
