using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.PluginServices.Publishing;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Services;
using Amazon.AWSToolkit.Publish.ViewModels;
using Amazon.AWSToolkit.Tasks;

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
        private CancellationTokenSource _publishSummaryTokenSource = new CancellationTokenSource();
        private CancellationTokenSource _requiredPublishPropertiesTokenSource = new CancellationTokenSource();
        private readonly AutoResetEvent _setupSessionEvent = new AutoResetEvent(true);
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

            _disposables.Push(_setupSessionEvent);
            _disposables.Push(_cancellationTokenSource);
            _disposables.Push(_publishSummaryTokenSource);
            _disposables.Push(_requiredPublishPropertiesTokenSource);

            _viewModel = CreateViewModel(args, cliServer);

            publishContext.ConnectionManager.ConnectionStateChanged += ConnectionManagerOnConnectionStateChanged;
            publishContext.ConnectionManager.ChangeConnectionSettings(args.CredentialId, args.Region);

            InitializeComponent();

            DataContext = _viewModel;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            _viewModel.WorkflowDuration.Start();
        }

        private PublishToAwsDocumentViewModel CreateViewModel(ShowPublishToAwsDocumentArgs args, ICliServer cliServer)
        {
            var viewModel = new PublishToAwsDocumentViewModel(_publishContext)
            {
                CredentialsId = args.CredentialId,
                Region = args.Region,
                ProjectName = args.ProjectName,
                ProjectPath = args.ProjectPath,
                ViewStage = PublishViewStage.Target,
            };
            viewModel.LoadPublishSettings();

            var configurationDetailFactory = new ConfigurationDetailFactory(viewModel, _publishContext.ToolkitShellProvider.GetDialogFactory());
            var client = cliServer.GetRestClient(viewModel.GetCredentials);
            viewModel.DeployToolController = new DeployToolController(client, configurationDetailFactory);
            viewModel.DeploymentClient = cliServer.GetDeploymentClient();

            var publishCommand = new PublishCommand(viewModel, _publishContext.ToolkitShellProvider);
            var configCommand = new ConfigCommand(viewModel);
            var targetCommand = new TargetCommand(viewModel);
            var startOverCommand = new StartOverCommand(viewModel, this);
            var optionsCommand = new PersistBannerVisibilityCommand(_publishContext.PublishSettingsRepository, viewModel);
            var closeFailureBannerCommand = CloseFailureBannerCommandFactory.Create(viewModel);
            var stackViewerCommand = StackViewerCommandFactory.Create(viewModel);
            var copyToClipboardCommand = CopyToClipboardCommand.Create(viewModel);

            _disposables.Push(publishCommand);
            _disposables.Push(configCommand);
            _disposables.Push(targetCommand);
            _disposables.Push(startOverCommand);

            viewModel.PublishToAwsCommand = publishCommand;
            viewModel.ConfigTargetCommand = configCommand;
            viewModel.BackToTargetCommand = targetCommand;
            viewModel.StartOverCommand = startOverCommand;
            viewModel.PersistOptionsSettingsCommand = optionsCommand;
            viewModel.CloseFailureBannerCommand = closeFailureBannerCommand;
            viewModel.ReenableOldPublishCommand = CreateReenableOldPublishCommand(viewModel);
            viewModel.StackViewerCommand = stackViewerCommand;
            viewModel.CopyToClipboardCommand = copyToClipboardCommand;

            return viewModel;
        }

        public void Dispose()
        {
            _viewModel.WorkflowDuration.Stop();
            Logger.Debug($"Disposing Publish dialog: {_viewModel.ProjectName}");

            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;

            _requiredPublishPropertiesTokenSource.Cancel();
            _publishSummaryTokenSource.Cancel();
            _cancellationTokenSource.Cancel();

            while (_disposables.Count > 0)
            {
                var disposable = _disposables.Pop();
                disposable.Dispose();
            }

            var isPublished = _viewModel.ViewStage == PublishViewStage.Publish;
            _viewModel.RecordPublishEndMetric(isPublished);

            Logger.Debug($"Disposed Publish dialog: {_viewModel.ProjectName}");
        }

        public override bool CanClose()
        {
            if (_viewModel.IsPublishing)
            {
                return _publishContext.ToolkitShellProvider.Confirm("Publish In Progress",
                    $"Publish will continue in the background but you will not see any updates.{Environment.NewLine}Are you sure you want to close?");
            }

            JoinableTaskFactory.Run(async () =>
            {
                await TeardownDeploymentSession().ConfigureAwait(false);
            });

            return true;
        }

        private void ConnectionManagerOnConnectionStateChanged(object sender, ConnectionStateChangeArgs e)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                _viewModel.IsLoading = !e.State.IsTerminal;

                if (e.State.IsTerminal)
                {
                    if (!(e.State is ConnectionState.ValidConnection))
                    {
                        _publishContext.ToolkitShellProvider.OutputToHostConsole(
                            $"Publish {_viewModel?.ProjectName} to AWS does not have a valid credentials-region combination.{Environment.NewLine}Close the publish screen, select valid credentials in the AWS Explorer, and try again.{Environment.NewLine}{e.State.Message}.",
                            true);

                        _viewModel.Recommendations.Clear();
                        _viewModel.RepublishTargets.Clear();
                        return;
                    }

                    JoinableTaskFactory.RunAsync(InitializePublishDocument).Task.LogExceptionAndForget();
                }
            });
        }

        private async Task InitializePublishDocument()
        {
            try
            {
                using (var _ = new DocumentLoadingIndicator(_viewModel, JoinableTaskFactory))
                {
                    await TaskScheduler.Default;
                    await EnsureDeploymentSessionId().ConfigureAwait(false);
                    await LoadPublishTargetsAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                _publishContext.ToolkitShellProvider.OutputError(e, Logger);
                _publishContext.ToolkitShellProvider.ShowMessage("Unable to setup a deployment session", "An error occurred while starting a deployment session. See the Output window for details.");
            }
        }

        /// <summary>
        /// Responsible for setting up the deployment session.
        /// Early exits if the deployment session is already established.
        /// 
        /// This is the first thing that needs to happen after establishing any valid credentials,
        /// because all other publish API calls are based on the resulting SessionId.
        /// </summary>
        private async Task EnsureDeploymentSessionId()
        {
            try
            {
                // Prevent concurrent attempts at setting up a deployment session
                _setupSessionEvent.WaitOne();

                // We only ever set up the deployment session once
                if (!string.IsNullOrWhiteSpace(_viewModel.SessionId))
                {
                    return;
                }

                await SetupDeploymentSessionAsync().ConfigureAwait(false);
            }
            catch (SessionException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new SessionException("Unable to set up a deployment session. Try restarting Visual Studio.", e);
            }
            finally
            {
                _setupSessionEvent.Set();
            }
        }

        private async Task SetupDeploymentSessionAsync()
        {
            using (var tokenSource = CreateCancellationTokenSource())
            {
                await TaskScheduler.Default;
                await _viewModel.StartDeploymentSession(tokenSource.Token).ConfigureAwait(false);
                await _viewModel.JoinDeploymentSession().ConfigureAwait(false);
            }
        }

        public async Task LoadPublishTargetsAsync()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;
                    // query appropriate recommendations
                    await _viewModel.InitializePublishTargets(tokenSource.Token);
                    _viewModel.SetPublishTargetsLoaded(true);
                }
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

        private async Task TeardownDeploymentSession()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;

                    await _viewModel.StopDeploymentSession(tokenSource.Token)
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
                UpdateRequiredPublishProperties();
                UpdateConfiguration();
            }
            if (e.PropertyName == nameof(PublishToAwsDocumentViewModel.ConfigurationDetails))
            {
                if (_viewModel.ConfigurationDetails != null)
                {
                    foreach (var config in _viewModel.ConfigurationDetails.GetDetailAndDescendants())
                    {
                        config.PropertyChanged += Config_PropertyChanged;
                    }
                }
            }

            if (e.PropertyName == nameof(PublishToAwsDocumentViewModel.ViewStage))
            {
                if (_viewModel.ViewStage == PublishViewStage.Publish)
                {
                    _viewModel.SetIsPublishing(true);
                }
            }

            if (e.PropertyName == nameof(PublishToAwsDocumentViewModel.ProgressStatus))
            {
                if (_viewModel.ProgressStatus != ProgressStatus.Loading)
                {
                    ReloadPublishedResources().LogExceptionAndForget();
                }
            }
        }

        private void UpdateConfiguration()
        {
            ResetConfigDetails();
            if (_viewModelChangeHandler.ShouldRefreshTarget(_viewModel))
            {
                ReloadTargetConfigurations().LogExceptionAndForget();
            }
        }

        private void UpdateRequiredPublishProperties()
        {
            CancelRequiredPublishUpdatesInProgress();
            _viewModel.UpdateRequiredPublishProperties(_requiredPublishPropertiesTokenSource.Token).LogExceptionAndForget();
        }

        private void ResetConfigDetails()
        {
            if (_viewModel.ConfigurationDetails != null)
            {
                foreach (var config in _viewModel.ConfigurationDetails.GetDetailAndDescendants())
                {
                    config.PropertyChanged -= Config_PropertyChanged;
                }

                _viewModel.ConfigurationDetails = null;
            }
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

        private async Task ReloadTargetConfigurations()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;
                    await _viewModel.SetDeploymentTarget(tokenSource.Token).ConfigureAwait(false);

                    var loadTargetConfigurationTask = LoadTargetConfigurations();
                    var reloadSystemCapabilitiesTask = ReloadSystemCapabilities();
                    await Task.WhenAll(loadTargetConfigurationTask, reloadSystemCapabilitiesTask).ConfigureAwait(false);
                }
                _viewModel.IsDefaultConfig = true;
            }
            catch (Exception e)
            {
                Logger.Error("Error reloading configuration details", e);
            }
        }

        private async Task ReloadSystemCapabilities()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;
                    await _viewModel.RefreshSystemCapabilities(tokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error reloading system capabilities", e);
            }
        }

        private async Task ReloadPublishedResources()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                {
                    await TaskScheduler.Default;
                    await _viewModel.RefreshPublishedResources(tokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error reloading published resources", e);
                _publishContext.ToolkitShellProvider.OutputToHostConsole("Unable to retrieve published resource details. See Toolkit logs for details.", true);
            }
        }

        private async Task LoadTargetConfigurations()
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                using (var _ = new DocumentLoadingIndicator(_viewModel, JoinableTaskFactory))
                {
                    await TaskScheduler.Default;
                    await _viewModel.RefreshTargetConfigurations(tokenSource.Token).ConfigureAwait(false);
                    await TaskScheduler.Default;
                    await _viewModel.RefreshConfigurationSettingValues(tokenSource.Token).ConfigureAwait(false);
                }
            }
            catch (PublishException e)
            {
                _publishContext.ToolkitShellProvider.OutputError(e, Logger);
            }
            catch (Exception e)
            {
                _publishContext.ToolkitShellProvider.OutputError(
                    new Exception($"Unable to retrieve publish settings: {e.Message}", e), Logger);
            }
        }

        private async Task SetTargetConfiguration(ConfigurationDetail configurationDetail)
        {
            try
            {
                using (var tokenSource = CreateCancellationTokenSource())
                using (var _ = new DocumentLoadingIndicator(_viewModel, JoinableTaskFactory))
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
                        await LoadTargetConfigurations();
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error($"Error setting configuration of type {configurationDetail.Name}", e);
            }
        }

        private void Config_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ConfigurationDetail.Value))
            {
                SetTargetConfiguration(sender as ConfigurationDetail).LogExceptionAndForget();
                _viewModel.IsDefaultConfig = false;
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

        private ICommand CreateReenableOldPublishCommand(PublishToAwsDocumentViewModel viewModel)
        {
            return new ShowExceptionAndForgetCommand(
                ReenableOldPublishCommandFactory.Create(viewModel),
                _publishContext.ToolkitShellProvider);
        }
    }
}
