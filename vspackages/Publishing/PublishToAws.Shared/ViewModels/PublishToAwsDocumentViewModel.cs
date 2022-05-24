using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Feedback;
using Amazon.AWSToolkit.Publish.Commands;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime;

using AWS.Deploy.ServerMode.Client;

using log4net;

using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// This ViewModel contains the functionality and data that
    /// backs the "Publish to AWS" document tab (<see cref="Views.PublishApplicationView"/>)
    /// </summary>
    public class PublishToAwsDocumentViewModel : BaseModel, INotifyDataErrorInfo
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PublishToAwsDocumentViewModel));
        private const string DeploymentStatusMessage = "Deployment Status: ";

        /// <summary>
        /// The set of fields that have an impact on <see cref="PublishSummary"/>
        /// </summary>
        public static readonly IList<string> SummaryAffectingProperties = new List<string>()
        {
            nameof(ProjectName),
            nameof(StackName),
            nameof(PublishDestination),
            nameof(ConfigurationDetails),
        };

        public static readonly IList<string> PublishAffectingProperties = new List<string>()
        {
            nameof(PublishDestination), nameof(PublishStackName)
        };

        private readonly PublishApplicationContext _publishContext;
        private readonly PublishConnectionViewModel _publishConnection;

        private IDeployToolController _deployToolController;
        private IDeploymentCommunicationClient _deploymentCommunicationClient;
        private volatile int _isLoadingScope = 0;
        private bool _isLoading;
        private string _errorMessage = string.Empty;
        private bool _publishTargetsLoaded;
        private bool _isOptionsBannerEnabled;
        private bool _isRepublish;
        private bool _isDefaultConfig = true;
        private string _projectPath;
        private Guid _projectGuid;
        private string _projectName;
        private string _publishSummary;
        private string _targetDescription;
        private PublishViewStage _viewStage;
        private PublishCommand _publishToAwsCommand;
        private ConfigCommand _configTargetCommand;
        private TargetCommand _backToTargetCommand;
        private ICommand _persistOptionsSettingsCommand;
        private ICommand _learnMoreCommand;
        private ICommand _feedbackCommand;
        private string _deploymentSessionId;
        private string _targetRecipe;
        private ICollectionView _republishCollectionView;

        private readonly PublishProjectViewModel _publishProjectViewModel;

        /// <summary>
        /// Holds the currently selected publishing target
        /// </summary>
        private PublishDestinationBase _publishDestination;

        /// <summary>
        /// Holds the previously selected target when toggling between New/Existing deployments (<see cref="IsRepublish"/>)
        /// </summary>
        private PublishDestinationBase _publishDestinationCache;

        private string _stackName;

        private string _publishStackName;

        private Stopwatch _workflowDuration = new Stopwatch();


        private ObservableCollection<PublishRecommendation> _recommendations =
            new ObservableCollection<PublishRecommendation>();

        private ObservableCollection<RepublishTarget> _republishTargets =
            new ObservableCollection<RepublishTarget>();

        private bool _loadingSystemCapabilities = false;

        private ObservableCollection<TargetSystemCapability> _systemCapabilities =
            new ObservableCollection<TargetSystemCapability>();

        private ObservableCollection<ConfigurationDetail> _configurationDetails =
            new ObservableCollection<ConfigurationDetail>();

        public readonly MissingCapabilities MissingCapabilities =
            new MissingCapabilities();

        public readonly UnsupportedSettingTypes UnsupportedSettingTypes =
            new UnsupportedSettingTypes();

        /// <summary>
        /// Class constructor
        /// </summary>
        public PublishToAwsDocumentViewModel(PublishApplicationContext publishContext)
        {
            _publishContext = publishContext;
            _publishConnection = new PublishConnectionViewModel(
                publishContext.ConnectionManager,
                publishContext.PublishPackage.JoinableTaskFactory);

            _publishProjectViewModel = new PublishProjectViewModel(_publishContext);
        }

        public void LoadPublishSettings()
        {
            JoinableTaskFactory.RunAsync(async () =>
            {
                await TaskScheduler.Default;
                await LoadOptionsButtonSettingsAsync().ConfigureAwait(false);
            }).Task.LogExceptionAndForget();
        }

        public JoinableTaskFactory JoinableTaskFactory => _publishContext.PublishPackage.JoinableTaskFactory;
        public PublishConnectionViewModel Connection => _publishConnection;

        public PublishProjectViewModel PublishProjectViewModel => _publishProjectViewModel;

        /// <summary>
        /// Represents the republish targets collection view displayed in the Targets view
        /// </summary>
        public ICollectionView RepublishCollectionView
        {
            get => _republishCollectionView;
            set => SetProperty(ref _republishCollectionView, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Whether or not the UI should show a loading indicator
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Whether or not the initial targets load is complete
        /// </summary>
        public bool PublishTargetsLoaded
        {
            get => _publishTargetsLoaded;
            set => SetProperty(ref _publishTargetsLoaded, value);
        }


        /// <summary>
        /// Whether or not the UI should show the new publish experience options banner
        /// </summary>
        public bool IsOptionsBannerEnabled
        {
            get => _isOptionsBannerEnabled;
            set => SetProperty(ref _isOptionsBannerEnabled, value);
        }

        /// <summary>
        /// Indicates whether or not the application is being re-published to an existing target
        /// </summary>
        public bool IsRepublish
        {
            get => _isRepublish;
            set => SetProperty(ref _isRepublish, value);
        }

        /// <summary>
        /// Swaps <see cref="_publishDestination"/> and <see cref="_publishDestinationCache"/> if
        /// <see cref="IsRepublish"/> does not align with the currently selected target type.
        /// 
        /// Intended to be called after toggling <see cref="IsRepublish"/>
        /// </summary>
        public void CyclePublishDestination()
        {
            if (!ShouldCyclePublishDestination())
            {
                return;
            }

            PublishDestinationBase toCache = PublishDestination;
            PublishDestinationBase toRestore = GetPublishDestinationToRestore();

            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                SetCachedPublishDestination(toCache);
                PublishDestination = toRestore;
            });
        }

        protected void SetCachedPublishDestination(PublishDestinationBase publishDestination)
        {
            _publishDestinationCache = publishDestination;
        }

        private bool ShouldCyclePublishDestination()
        {
            if (IsRepublish)
            {
                return !(PublishDestination is RepublishTarget);
            }

            return !(PublishDestination is PublishRecommendation);
        }

        private PublishDestinationBase GetPublishDestinationToRestore()
        {
            if (IsRepublish)
            {
                return GetRepublishTargetOrFallback(_publishDestinationCache as RepublishTarget);
            }

            return GetRecommendationOrFallback(_publishDestinationCache as PublishRecommendation);
        }

        public PublishRecommendation GetRecommendationOrFallback(PublishRecommendation recommendation)
        {
            return Recommendations.FirstOrDefault(r => r == recommendation) ??
                   Recommendations.FirstOrDefault(r => r.IsRecommended) ??
                   Recommendations.FirstOrDefault();
        }

        public RepublishTarget GetRepublishTargetOrFallback(RepublishTarget republishTarget)
        {
            return RepublishTargets.FirstOrDefault(r => r == republishTarget) ??
                   RepublishTargets.FirstOrDefault(r => r.IsRecommended) ??
                   RepublishTargets.FirstOrDefault();
        }

        /// <summary>
        /// Indicates whether or not the application is published using the default configurations
        /// </summary>
        public bool IsDefaultConfig
        {
            get => _isDefaultConfig;
            set => SetProperty(ref _isDefaultConfig, value);
        }

        /// <summary>
        /// The time lapsed since the user enters the publish experience
        /// </summary>
        public Stopwatch WorkflowDuration
        {
            get => _workflowDuration;
            set => SetProperty(ref _workflowDuration, value);
        }

        /// <summary>
        /// Full path of the project (eg .csproj file) that will be published
        /// </summary>
        public string ProjectPath
        {
            get => _projectPath;
            set => SetProperty(ref _projectPath, value);
        }

        /// <summary>
        /// Full path of the project (eg .csproj file) that will be published
        /// </summary>
        public Guid ProjectGuid
        {
            get => _projectGuid;
            set => SetProperty(ref _projectGuid, value);
        }

        /// <summary>
        /// Display-friendly name of the project that will be published
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// Indicates the current step of the Publish Workflow
        /// </summary>
        public PublishViewStage ViewStage
        {
            get => _viewStage;
            set => SetProperty(ref _viewStage, value);
        }

        /// <summary>
        /// SignalR client used to get deployment status updates from the Publish CLI Server
        /// </summary>
        public IDeploymentCommunicationClient DeploymentClient
        {
            get => _deploymentCommunicationClient;
            set => SetProperty(ref _deploymentCommunicationClient, value);
        }


        /// <summary>
        /// Rest client used to communicate with the Publish CLI Server
        /// </summary>
        public IDeployToolController DeployToolController
        {
            get => _deployToolController;
            set
            {
                _deployToolController = value;
                PublishProjectViewModel.SetDeployToolController(value);
            }
        }

        /// <summary>
        /// Command that starts the publish to aws workflow
        /// </summary>
        public PublishCommand PublishToAwsCommand
        {
            get => _publishToAwsCommand;
            set => SetProperty(ref _publishToAwsCommand, value);
        }

        /// <summary>
        /// Command that displays the configuration details view
        /// </summary>
        public ConfigCommand ConfigTargetCommand
        {
            get => _configTargetCommand;
            set => SetProperty(ref _configTargetCommand, value);
        }

        /// <summary>
        /// Command that moves back to display the Target view
        /// </summary>
        public TargetCommand BackToTargetCommand
        {
            get => _backToTargetCommand;
            set => SetProperty(ref _backToTargetCommand, value);
        }

        /// <summary>
        /// Command that persists settings related to new publish options banner
        /// </summary>
        public ICommand PersistOptionsSettingsCommand
        {
            get => _persistOptionsSettingsCommand;
            set => SetProperty(ref _persistOptionsSettingsCommand, value);
        }

        /// <summary>
        /// Command that opens up the user guide for the new Publish Experience
        /// </summary>
        public ICommand LearnMoreCommand => _learnMoreCommand ?? (_learnMoreCommand = CreateLearnMoreCommand());

        /// <summary>
        /// Command that shows the feedback panel for the Publish experience
        /// </summary>
        public ICommand FeedbackCommand => _feedbackCommand ?? (_feedbackCommand = CreateFeedbackCommand());

        /// <summary>
        /// Stack name for the deployed application
        /// </summary>
        public string StackName
        {
            get => _stackName;
            set => SetProperty(ref _stackName, value);
        }

        // NOTE: This field should be made data-driven through the deploy API.
        // Until that is available, we hard-code its behavior.
        public bool IsApplicationNameRequired
        {
            get
            {
                if (PublishDestination == null) { return false; }
                if (!(PublishDestination is PublishRecommendation)) { return false; }

                return PublishDestination.DeploymentArtifact != DeploymentArtifact.ElasticContainerRegistry;
            }
        }

        /// <summary>
        /// Stack name for the application specified in the Publish view of Select Targets UI
        /// </summary>
        public string PublishStackName
        {
            get => _publishStackName;
            set => SetProperty(ref _publishStackName, value);
        }

        /// <summary>
        /// Recipe name for the target to which application is deployed
        /// </summary>
        public string TargetRecipe
        {
            get => _targetRecipe;
            set => SetProperty(ref _targetRecipe, value);
        }

        /// <summary>
        /// Identifies the CLI server's deployment session for this project.
        /// It is set to null when there is no active session.
        /// </summary>
        public string SessionId
        {
            get => _deploymentSessionId;
            protected set => SetProperty(ref _deploymentSessionId, value);
        }

        /// <summary>
        /// Recommendations that show up in the Publish Dialog
        /// </summary>
        public ObservableCollection<PublishRecommendation> Recommendations
        {
            get => _recommendations;
            set => SetProperty(ref _recommendations, value);
        }

        public ObservableCollection<ConfigurationDetail> ConfigurationDetails
        {
            get => _configurationDetails;
            set => SetProperty(ref _configurationDetails, value);
        }

        /// <summary>
        /// The target to publish (or republish) the user's application to.
        /// </summary>
        public PublishDestinationBase PublishDestination
        {
            get => _publishDestination;
            set
            {
                SetProperty(ref _publishDestination, value);
                NotifyPropertyChanged(nameof(IsApplicationNameRequired));
            }
        }

        /// <summary>
        /// Existing publish targets that show up in the Republish view
        /// </summary>
        public ObservableCollection<RepublishTarget> RepublishTargets
        {
            get => _republishTargets;
            set => SetProperty(ref _republishTargets, value);
        }

        public bool LoadingSystemCapabilities
        {
            get => _loadingSystemCapabilities;
            private set => SetProperty(ref _loadingSystemCapabilities, value);
        }

        public ObservableCollection<TargetSystemCapability> SystemCapabilities
        {
            get => _systemCapabilities;
            private set => SetProperty(ref _systemCapabilities, value);
        }

        /// <summary>
        /// The summary shown in the UI reflecting what would happen if the project were published right now.
        /// </summary>
        public string PublishSummary
        {
            get => _publishSummary;
            set => SetProperty(ref _publishSummary, value);
        }

        /// <summary>
        /// The summary shown in the UI reflecting what would happen if the project were published right now.
        /// </summary>
        public string TargetDescription
        {
            get => _targetDescription;
            set => SetProperty(ref _targetDescription, value);
        }

        public PublishApplicationContext PublishContext => _publishContext;

        public bool IsSessionEstablished => !string.IsNullOrWhiteSpace(SessionId);

        public bool IsStackNameSet => !string.IsNullOrWhiteSpace(StackName);

        public bool HasValidationErrors()
        {
            if (AsINotifyDataErrorInfo.HasErrors)
            {
                return true;
            }

            var configurationDetailValidation = ConfigurationDetails?.GetDetailAndDescendants()
                       .Any(detail => detail.HasErrors)
                   ?? false;
            if (configurationDetailValidation)
            {
                return true;
            }

            var systemCapabilitiesValidation = SystemCapabilities?.Any() ?? false;
            if (systemCapabilitiesValidation)
            {
                return true;
            }

            return false;
        }

        public bool IsValidRegion(ToolkitRegion region)
        {
            if (string.IsNullOrWhiteSpace(region?.Id))
            {
                return false;
            }

            return !_publishContext.RegionProvider.IsRegionLocal(region.Id);
        }

        public ToolkitRegion GetValidRegion(ICredentialIdentifier credentialIdentifier)
        {
            try
            {
                var props = _publishContext.CredentialSettings.GetProfileProperties(credentialIdentifier);

                var region = _publishContext.RegionProvider.GetRegion(props.Region) ??
                             _publishContext.RegionProvider.GetRegion(props.SsoRegion);

                if (IsValidRegion(region))
                {
                    return region;
                }
            }
            catch (Exception e)
            {
                Logger.Error("Unable to determine a proper region, Publish to AWS will start with an unexpected region", e);
            }
            return GetFallbackRegion();
        }

        private ToolkitRegion GetFallbackRegion()
        {
            return _publishContext.RegionProvider.GetRegion(RegionEndpoint.USEast1.SystemName) ??
                   _publishContext.RegionProvider.GetRegions("aws").FirstOrDefault() ??
                   throw new Exception("Toolkit does not have valid region data");
        }

        /// <summary>
        /// Resolves <see cref="AWSCredentials"/> to be used with Deploying.
        /// </summary>
        public Task<AWSCredentials> GetCredentialsAsync()
        {
            return Task.FromResult(_publishContext.ConnectionManager.ActiveCredentials);
        }

        public async Task RestartDeploymentSessionAsync(CancellationToken cancellationToken)
        {
            await ClearTargetSelectionAsync(cancellationToken).ConfigureAwait(false);
            await StopDeploymentSessionAsync(cancellationToken).ConfigureAwait(false);
            await StartDeploymentSessionAsync(cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(
                JoinDeploymentSessionAsync(),
                SetRepublishTargetsAsync(new ObservableCollection<RepublishTarget>(), cancellationToken),
                SetRecommendationsAsync(new ObservableCollection<PublishRecommendation>(), cancellationToken)
            ).ConfigureAwait(false);
        }

        public async Task ClearTargetSelectionAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            ErrorMessage = string.Empty;
            PublishDestination = null;
            SetCachedPublishDestination(null);
            SystemCapabilities?.Clear();
            TargetDescription = string.Empty;
            PublishSummary = string.Empty;
        }

        /// <summary>
        /// Defines a deployment session on the CLI Server for this project, if one isn't already defined.
        /// <see cref="SessionId"/> is assigned the resulting Session Id.
        /// </summary>
        public async Task StartDeploymentSessionAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (IsSessionEstablished)
                {
                    return;
                }

                Logger.Debug("Starting a deployment session");

                var response = await DeployToolController.StartSessionAsync(Connection.Region.Id, ProjectPath, cancellationToken)
                    .ConfigureAwait(false);

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                SessionId = response.SessionId;
                PublishStackName = response.DefaultApplicationName;

                Logger.Debug("Deployment session started");
            }
            catch (Exception e)
            {
                throw new SessionException($"Unable to start a deployment session:{Environment.NewLine}{GetExceptionInnerMessage(e)}", e);
            }
        }

        /// <summary>
        /// Removes this project's deployment session from the CLI Server.
        /// <see cref="SessionId"/> is cleared.
        /// </summary>
        public async Task StopDeploymentSessionAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (!IsSessionEstablished)
                {
                    return;
                }

                Logger.Debug("Stopping a deployment session");

                await DeployToolController.StopSessionAsync(SessionId, cancellationToken).ConfigureAwait(false);

                Logger.Debug("Deployment session stopped");
            }
            catch (Exception e)
            {
                throw new SessionException("Unable to close current deployment session." +
                                           " If the problem persists, manually terminate the AWS Deploy Tool in your process manager." +
                                           $"{Environment.NewLine}Reason: {e.Message}", e);
            }
            finally
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                SessionId = null;
            }
        }

        /// <summary>
        /// Loads deployment recommendations for this project.
        /// <see cref="Recommendations"/> is populated with any loaded recommendations.
        /// </summary>
        public async Task RefreshRecommendationsAsync(CancellationToken cancellationToken)
        {
            var recommendations = new ObservableCollection<PublishRecommendation>();

            try
            {
                ThrowIfSessionIsNotCreated();

                var publishRecommendations = await DeployToolController.GetRecommendationsAsync(SessionId, ProjectPath, cancellationToken).ConfigureAwait(false);

                recommendations = new ObservableCollection<PublishRecommendation>(publishRecommendations);
            }
            catch (Exception e)
            {
                throw new PublishException(
                    $"Failure loading deployment recommendations:{Environment.NewLine}" +
                    $"{GetExceptionInnerMessage(e)}{Environment.NewLine}" +
                    "You might need to reload the publish experience.", e);
            }
            finally
            {
                await SetRecommendationsAsync(recommendations, cancellationToken);

                var message = $"Publish targets found for {ProjectName}: {recommendations.Count}";
                Logger.Debug(message);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
            }
        }

        /// <summary>
        /// Loads existing publishing targets for this project.
        /// <see cref="RepublishTargets"/> is populated with any loaded targets
        /// </summary>
        public async Task RefreshExistingTargetsAsync(CancellationToken cancellationToken)
        {
            var targets = new ObservableCollection<RepublishTarget>();

            try
            {
                ThrowIfSessionIsNotCreated();

                var republishTargets = await DeployToolController.GetRepublishTargetsAsync(SessionId, ProjectPath, cancellationToken)
                    .ConfigureAwait(false);

                targets = new ObservableCollection<RepublishTarget>(republishTargets);
            }
            catch (Exception e)
            {
                throw new PublishException(
                    $"Failure loading re-deployment targets:{Environment.NewLine}" +
                    $"{GetExceptionInnerMessage(e)}{Environment.NewLine}" +
                    "You might need to reload the publish experience.", e);
            }
            finally
            {
                await SetRepublishTargetsAsync(targets, cancellationToken);

                var message = $"Republish targets found for {ProjectName}: {targets.Count}";
                Logger.Debug(message);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
            }
        }

        public async Task RefreshSystemCapabilitiesAsync(CancellationToken cancellationToken)
        {
            var capabilities = new List<TargetSystemCapability>();

            try
            {
                ThrowIfSessionIsNotCreated();

                var recipeId = PublishDestination?.RecipeId;
                if (string.IsNullOrWhiteSpace(recipeId)) { return; }

                using (await CreateLoadingSystemCapabilitiesScopeAsync().ConfigureAwait(false))
                {
                    var systemCapabilities = await DeployToolController
                        .GetCompatibilityAsync(SessionId, cancellationToken)
                        .ConfigureAwait(false);

                    capabilities.AddRange(systemCapabilities);

                    UpdateMissingCapabilities(recipeId, capabilities);
                }
            }
            catch (Exception e)
            {
                throw new PublishException($"Error getting system requirements: {GetExceptionInnerMessage(e)}", e);
            }
            finally
            {
                await SetSystemCapabilitiesAsync(capabilities, cancellationToken);
            }
        }

        private async Task<IDisposable> CreateLoadingSystemCapabilitiesScopeAsync()
        {
            await SetLoadingSystemCapabilitiesAsync(true).ConfigureAwait(false);

            return new DisposingAction(() =>
            {
                JoinableTaskFactory.Run(async () => await SetLoadingSystemCapabilitiesAsync(false).ConfigureAwait(false));
            });
        }

        private async Task SetLoadingSystemCapabilitiesAsync(bool isLoading)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            LoadingSystemCapabilities = isLoading;
        }

        private void UpdateMissingCapabilities(string recipeId, IEnumerable<TargetSystemCapability> capabilities)
        {
            MissingCapabilities.Update(recipeId, capabilities);
        }

        /// <summary>
        /// Connects deployment client to the session such that it can listen to logs of the deployment session
        /// </summary>
        /// <returns></returns>
        public async Task JoinDeploymentSessionAsync()
        {
            ThrowIfSessionIsNotCreated();

            await DeploymentClient.JoinSession(SessionId);
            // register callback to log all deployment output in the progress dialog
            DeploymentClient.ReceiveLogSectionStart = OnDeploymentClientStartLogSection;
            DeploymentClient.ReceiveLogInfoMessage = OnDeploymentClientReceiveLog;
            DeploymentClient.ReceiveLogErrorMessage = OnDeploymentClientReceiveLog;
        }

        /// <summary>
        /// Event: A new message log section has been started
        /// </summary>
        private void OnDeploymentClientStartLogSection(string sectionName, string description)
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                PublishProjectViewModel.CreateMessageGroup(sectionName, description);
            });
        }

        /// <summary>
        /// Event: The deployment service emitted a log entry
        /// </summary>
        private void OnDeploymentClientReceiveLog(string text)
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                await UpdateDeploymentProgressAsync(text);
            });
        }

        /// <summary>
        /// Populates <see cref="ConfigurationDetails"/> with configuration settings for the
        /// selected target (<see cref="PublishDestination"/>).
        /// 
        /// Settings that may offer a selection of values (<see cref="ConfigurationDetail.ValueMappings"/>) are also loaded.
        /// </summary>
        public async Task RefreshTargetConfigurationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfSessionIsNotCreated();

                if (PublishDestination == null)
                {
                    await ClearConfigurationDetailsAsync();
                    return;
                }

                var recipeId = PublishDestination?.RecipeId;

                var configurationDetails =
                    (await LoadConfigurationDetailsAsync(SessionId, cancellationToken).ConfigureAwait(false))
                    .ToList();

                await SetConfigurationDetailsAsync(configurationDetails);
                UpdateUnsupportedSetting(recipeId, configurationDetails);
            }
            catch (Exception e)
            {
                await ClearConfigurationDetailsAsync();
                throw new PublishException($"Unable to retrieve configuration details: {GetExceptionInnerMessage(e)}", e);
            }
        }

        public async Task<IEnumerable<ConfigurationDetail>> LoadConfigurationDetailsAsync(string sessionId, CancellationToken cancellationToken)
        {
            var configurationDetails = await DeployToolController.GetConfigSettingsAsync(sessionId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return await DeployToolController.UpdateConfigSettingValuesAsync(sessionId, configurationDetails, cancellationToken);
        }

        public async Task ClearConfigurationDetailsAsync()
        {
            await SetConfigurationDetailsAsync(Enumerable.Empty<ConfigurationDetail>());
        }

        public async Task SetConfigurationDetailsAsync(IEnumerable<ConfigurationDetail> configurationDetails)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(configurationDetails);
        }

        private void UpdateUnsupportedSetting(string recipeId, IList<ConfigurationDetail> configurationDetails)
        {
            UnsupportedSettingTypes.Update(recipeId, configurationDetails);
        }

        /// <summary>
        /// Sets a value for the given configuration setting of the selected target.
        /// A selected target needs to be defined prior to making this call.
        /// The function returns a string representing the error in setting a value (if any).
        /// </summary>
        public async Task<ValidationResult> SetTargetConfigurationAsync(ConfigurationDetail configurationDetail, CancellationToken cancellationToken)
        {
            ThrowIfSessionIsNotCreated();

            return await DeployToolController.ApplyConfigSettingsAsync(SessionId, configurationDetail, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<bool> ValidateTargetConfigurationsAsync()
        {
            try
            {
                var validation = await SetTargetConfigurationsAsync(CancellationToken.None).ConfigureAwait(false);

                if (!validation.HasErrors())
                {
                    return true;
                }

                var detailsByLeafId = ConfigurationDetails.GetDetailAndDescendants()
                    .ToDictionary(d => d.GetLeafId());

                var missingConfigErrors = new Dictionary<string, string>();

                foreach (var leafId in validation.GetErrantDetailIds())
                {
                    if (detailsByLeafId.TryGetValue(leafId, out var detail))
                    {
                        detail.ValidationMessage = validation.GetError(leafId);
                    }
                    else
                    {
                        missingConfigErrors.Add(leafId, validation.GetError(leafId));
                    }
                }

                // Log appropriate validation errors for configurations that are not found in the list of Configuration Details
                if (missingConfigErrors.Any())
                {
                    var errorLogMessage = string.Join(Environment.NewLine,
                        missingConfigErrors.Select(x => $"{x.Key}: {x.Value}"));
                    Logger.Error($"The following configuration details were not found: {Environment.NewLine}{errorLogMessage}");
                }

                return false;
            }
            catch (Exception e)
            {
                Logger.Error("Failed to validate configuration details.", e);
                throw;
            }
        }

        public async Task UpdateSummaryAsync(CancellationToken cancellationToken)
        {
            try
            {
                var publishSummary = GeneratePublishSummary().Trim();

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                PublishSummary = publishSummary;
            }
            catch (Exception e)
            {
                // This function is called frequently, do not spam log
                if (Debugger.IsAttached)
                {
                    Debug.Assert(false, $"Error updating summary: {e.Message}");
                }
            }
        }

        public string GeneratePublishSummary()
        {
            try
            {
                return ConfigurationDetails.GenerateSummary(IsRepublish);
            }
            catch (Exception e)
            {
                // This function is called frequently, do not spam log
                if (Debugger.IsAttached)
                {
                    Debug.Assert(false, $"Error generating summary: {e.Message}");
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Set the selected recommendation for a given Session Id.
        /// </summary>
        public async Task SetDeploymentTargetAsync(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfSessionIsNotCreated();

                if (IsRepublish)
                {
                    await DeployToolController.SetDeploymentTargetAsync(SessionId, PublishDestination as RepublishTarget, cancellationToken);
                }
                else
                {
                    await DeployToolController.SetDeploymentTargetAsync(SessionId, PublishDestination as PublishRecommendation, StackName, cancellationToken);
                }

                await UpdatePublishStackNameValidationAsync(string.Empty);
            }
            catch (InvalidApplicationNameException e)
            {
                await UpdatePublishStackNameValidationAsync(e.Message);

                throw;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting deployment target", e);
                throw;
            }
        }

        public async Task UpdatePublishStackNameValidationAsync(string validationMessage)
        {
            _errors[nameof(PublishStackName)] = validationMessage;
            await RaiseErrorsChangedAsync(nameof(PublishStackName)).ConfigureAwait(false);
        }

        public async Task UpdatePublishProjectViewModelAsync()
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            PublishProjectViewModel.StackName = StackName;
            PublishProjectViewModel.RecipeName = TargetRecipe;
            PublishProjectViewModel.RegionName = Connection.RegionDisplayName;
            PublishProjectViewModel.ArtifactType = PublishDestination.DeploymentArtifact;
            PublishProjectViewModel.SessionId = SessionId;
        }

        /// <summary>
        /// Publishes this project.
        /// State is expected to be valid prior to calling this function.
        /// </summary>
        public async Task<PublishProjectResult> PublishApplicationAsync()
        {
            Func<Task<PublishProjectResult>> publish = PublishProjectViewModel.PublishProjectAsync;
            Func<Func<Task<PublishProjectResult>>, Task<PublishProjectResult>> telemetry = EmitMetricsForPublishProjectAsync;
            Func<Func<Task<PublishProjectResult>>, Task<PublishProjectResult>> ui = AdjustUiForPublishProjectAsync;

            return await ui(() => telemetry(publish));
        }

        public async Task<PublishProjectResult> EmitMetricsForPublishProjectAsync(
            Func<Task<PublishProjectResult>> publishProjectAsyncFunc)
        {
            PublishProjectResult result = null;
            Stopwatch publishDuration = new Stopwatch();

            using (new DisposingAction(() => publishDuration.Stop()))
            {
                publishDuration.Start();

                result = await publishProjectAsyncFunc().ConfigureAwait(false);

                var metricResult = result.IsSuccess ? Result.Succeeded : Result.Failed;
                RecordPublishDeployMetric(metricResult,
                    publishDuration.Elapsed.TotalMilliseconds,
                    result.ErrorCode,
                    result.ErrorMessage);
            }

            return result;
        }

        public async Task<PublishProjectResult> AdjustUiForPublishProjectAsync(
            Func<Task<PublishProjectResult>> publishProjectAsyncFunc)
        {
            PublishProjectResult result = null;

            using (PublishProjectViewModel.IsPublishedScope())
            {
                await SetProgressStatusAsync(ProgressStatus.Loading);
                _publishContext.ToolkitShellProvider.OutputToHostConsole($"Starting to publish {ProjectName}");

                result = await publishProjectAsyncFunc().ConfigureAwait(false);

                await ApplyPublishResultToUiAsync(result).ConfigureAwait(false);
            }

            return result;
        }

        public void SetPublishTargetsLoaded(bool value)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                PublishTargetsLoaded = value;
            });
        }

        /// <summary>
        /// Updates properties required for publishing user's application based on current target view
        /// i.e. publish/republish
        /// </summary>
        public async Task UpdateRequiredPublishPropertiesAsync(CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                StackName = GetUpdatedStackName();
                TargetRecipe = PublishDestination?.RecipeName ?? string.Empty;
                TargetDescription = PublishDestination?.Description ?? string.Empty;
            }
            catch (Exception e)
            {
                Logger.Error("Error updating application stack name", e);
            }
        }

        /// <summary>
        /// Sets values for each corresponding config setting of the selected target.
        /// A selected target needs to be defined prior to making this call.
        /// The function returns a dictionary of configIds and corresponding string representing the error in setting a value for that configId (if any).
        /// </summary>
        private async Task<ValidationResult> SetTargetConfigurationsAsync(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfSessionIsNotCreated();

                return await DeployToolController.ApplyConfigSettingsAsync(SessionId, ConfigurationDetails, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Error("Failed to update configuration details.", e);
                throw;
            }
        }

        /// <summary>
        /// Updates deployment progress status in the Publish in Progress View
        /// </summary>
        private async Task UpdateDeploymentProgressAsync(string text)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _publishProjectViewModel.AppendLineDeploymentMessage(text);
        }

        /// <summary>
        /// Sets the Recommendations list and either re-selects the
        /// same Recommendation (by RecipeId) or a most recommended target.
        /// </summary>
        private async Task SetRecommendationsAsync(ObservableCollection<PublishRecommendation> recommendations,
            CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                Recommendations = recommendations;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting Recommendations list", e);
            }
        }

        /// <summary>
        /// Sets the RepublishTargets list and either re-selects the
        /// same target (by Stack name) or the first target.
        /// </summary>
        private async Task SetRepublishTargetsAsync(ObservableCollection<RepublishTarget> targets,
            CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                RepublishTargets = targets;

                UpdateRepublishCollectionView();
            }
            catch (Exception e)
            {
                Logger.Error("Error setting republish targets list", e);
            }
        }

        public async Task SetIsRepublishAsync(bool value, CancellationToken cancelToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancelToken);
                IsRepublish = value;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting is republish", e);
            }
        }

        public async Task InitializePublishTargetsAsync(CancellationToken cancelToken)
        {
            await Task.WhenAll(RefreshExistingTargetsAsync(cancelToken), RefreshRecommendationsAsync(cancelToken)).ConfigureAwait(false);
        }

        private void UpdateRepublishCollectionView()
        {
            RepublishCollectionView = CollectionViewSource.GetDefaultView(RepublishTargets);
            RepublishCollectionView.GroupDescriptions.Clear();
            RepublishCollectionView.GroupDescriptions.Add(
                new PropertyGroupDescription(nameof(RepublishTarget.Category)));
        }

        public async Task SetSystemCapabilitiesAsync(IEnumerable<TargetSystemCapability> capabilities,
            CancellationToken cancellationToken)
        {
            try
            {
                var missingRequirements = capabilities.ToArray();
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                SystemCapabilities = new ObservableCollection<TargetSystemCapability>(missingRequirements);

                if (missingRequirements.Any())
                {
                    var missingRequirementsStr = string.Join(", ", missingRequirements.Select(c => c.Name).OrderBy(s => s));
                    var message =
                        $"Cannot publish {ProjectName} to {PublishDestination?.Name}, missing requirements: {missingRequirementsStr}";
                    Logger.Debug(message);
                    _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Error setting system capabilities list", e);
            }
        }

        /// <summary>
        /// Sets the Deployment Progress Status
        /// </summary>
        private async Task SetProgressStatusAsync(ProgressStatus status)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                _publishProjectViewModel.ProgressStatus = status;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting progress status.", e);
            }
        }

        /// <summary>
        /// Reports final deployment status in the Publish View
        /// </summary>
        private async Task ApplyPublishResultToUiAsync(PublishProjectResult publishResult)
        {
            try
            {
                string statusMessage = CreateStatusMessage(publishResult);
                ProgressStatus status = publishResult.IsSuccess ? ProgressStatus.Success : ProgressStatus.Fail;

                await SetProgressStatusAsync(status);
                await UpdateDeploymentProgressAsync(statusMessage);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(statusMessage,
                    true);

                await SetIsFailureBannerEnabledAsync(!publishResult.IsSuccess).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error reporting final deployment status.", e);
            }
        }

        private string CreateStatusMessage(PublishProjectResult result)
        {
            var builder = new StringBuilder();

            if (result.IsSuccess)
            {
                builder.Append($"{StackName} was published as {TargetRecipe}");
            }
            else
            {
                builder.Append($"{StackName} could not be published as {TargetRecipe}");
                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    builder.Append($": {result.ErrorMessage}");
                }

                builder.AppendLine();
                builder.Append($"{ProjectName} failed to publish to AWS.");
            }

            return builder.ToString();
        }

        private async Task SetIsFailureBannerEnabledAsync(bool value)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            _publishProjectViewModel.IsFailureBannerEnabled = value;
        }

        private void ThrowIfSessionIsNotCreated()
        {
            if (!IsSessionEstablished)
            {
                throw new Exception("No deployment session available");
            }
        }

        private string GetUpdatedStackName()
        {
            if (PublishDestination is RepublishTarget republishTarget)
            {
                return republishTarget.Name ?? string.Empty;
            }

            return PublishStackName ?? string.Empty;
        }

        protected async Task LoadOptionsButtonSettingsAsync()
        {
            var showPublishBanner = true;
            try
            {
                var publishSettings = await _publishContext.PublishSettingsRepository.GetAsync();
                showPublishBanner = publishSettings.ShowPublishBanner;
            }
            catch (Exception e)
            {
                Logger.Error("Error retrieving show publish banner settings", e);
            }
            finally
            {
                await SetOptionsButtonPropertiesAsync(showPublishBanner);
            }
        }


        /// <summary>
        /// Sets the options button related properties 
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task SetOptionsButtonPropertiesAsync(bool result)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            IsOptionsBannerEnabled = result;
        }

        private ICommand CreateFeedbackCommand()
        {
            var toolkitContext = new ToolkitContext
            {
                TelemetryLogger = _publishContext.TelemetryLogger,
                ToolkitHost = _publishContext.ToolkitShellProvider
            };
            return new SendFeedbackCommand(toolkitContext);
        }

        private ICommand CreateLearnMoreCommand()
        {
            var toolkitHost = _publishContext.ToolkitShellProvider;
            return new ShowExceptionAndForgetCommand(LearnMoreCommandFactory.Create(toolkitHost), toolkitHost);
        }

        /// <summary>
        /// Creates a loading scope that decrements the scope on disposal.
        /// Creation and disposal of the scope will update <see cref="IsLoading"/>.
        /// </summary>
        public IDisposable CreateLoadingScope()
        {
            IncrementLoadingScope();
            return new DisposingAction(DecrementLoadingScope);
        }

        private void IncrementLoadingScope()
        {
            Interlocked.Increment(ref _isLoadingScope);
            UpdateIsLoading();
        }

        private void DecrementLoadingScope()
        {
            Interlocked.Decrement(ref _isLoadingScope);
            UpdateIsLoading();
        }

        private void UpdateIsLoading()
        {
            JoinableTaskFactory.Run(async () =>
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                IsLoading = _isLoadingScope > 0;
            });
        }

        public void RecordPublishEndMetric(bool published)
        {
            Dictionary<string, object> capabilityMetrics = CreateCapabilityMetrics();

            var payload = new PublishEnd()
            {
                AwsAccount = _publishContext.ConnectionManager.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = _publishContext.ConnectionManager.ActiveRegion?.Id ?? MetadataValue.NotSet,
                Duration = WorkflowDuration.Elapsed.TotalMilliseconds,
                Published = published
            };

            _publishContext.TelemetryLogger.RecordPublishEnd(payload, metricDatum =>
            {
                foreach (var item in capabilityMetrics
                             .Where(item => !metricDatum.Metadata.ContainsKey(item.Key)))
                {
                    metricDatum.AddMetadata(item.Key, item.Value);
                }
                return metricDatum;
            });
        }

        public void RecordPublishUnsupportedSettingMetric()
        {
            UnsupportedSettingTypes.RecordMetric(_publishContext);
        }

        private Dictionary<string, object> CreateCapabilityMetrics()
        {
            var capabilityMetrics = new Dictionary<string, object>();
            Action<string> addToCapabilityMetrics = (key) => capabilityMetrics[key] = true;

            MissingCapabilities
                .Resolved
                .ToList()
                .ForEach(capability => addToCapabilityMetrics($"resolvedComponent_{capability}"));

            MissingCapabilities
                .Missing
                .ToList()
                .ForEach(capability => addToCapabilityMetrics($"missingComponent_{capability}"));

            return capabilityMetrics;
        }

        private void RecordPublishDeployMetric(Result result, double elapsedMs, string errorCode, string errorMessage)
        {
            var payload = new PublishDeploy()
            {
                AwsAccount = _publishContext.ConnectionManager.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = _publishContext.ConnectionManager.ActiveRegion?.Id ?? MetadataValue.NotSet,
                Result = result,
                Duration = elapsedMs,
                InitialPublish = !IsRepublish,
                DefaultConfiguration = IsDefaultConfig,
                RecipeId = PublishDestination?.BaseRecipeId ?? PublishDestination?.RecipeId,
                IsGeneratedProject = PublishDestination?.IsGenerated,
            };

            if (payload.InitialPublish)
            {
                payload.RecommendedTarget = PublishDestination?.IsRecommended;
            }

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                payload.ErrorCode = errorCode;
            }

            _publishContext.TelemetryLogger.RecordPublishDeploy(payload, metricDatum =>
            {
                if (!string.IsNullOrWhiteSpace(errorMessage))
                {
                    metricDatum.SplitAndAddMetadata("reason", errorMessage.RedactAll());
                }

                return metricDatum;
            });
        }

        private string GetExceptionInnerMessage(Exception e)
        {
            if (e is ApiException apiException)
            {
                return apiException.Response;
            }

            return e.Message;
        }

        #region INotifyDataErrorInfo

        private readonly IDictionary<string, string> _errors = new Dictionary<string, string>();

        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            var errors = new List<string>();

            if (propertyName == null)
            {
                errors.AddRange(_errors.Values.Where(x => !string.IsNullOrWhiteSpace(x)));
            }
            else if (_errors.TryGetValue(propertyName, out string value))
            {
                errors.Add(value);
            }

            return errors;
        }

        bool INotifyDataErrorInfo.HasErrors => _errors.Values.Any(value => !string.IsNullOrWhiteSpace(value));

        private event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        event EventHandler<DataErrorsChangedEventArgs> INotifyDataErrorInfo.ErrorsChanged
        {
            add => ErrorsChanged += value;
            remove => ErrorsChanged -= value;
        }

        public INotifyDataErrorInfo AsINotifyDataErrorInfo => this;

        #endregion

        private async Task RaiseErrorsChangedAsync(string propertyName)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();

            NotifyPropertyChanged(nameof(INotifyDataErrorInfo.HasErrors));
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
