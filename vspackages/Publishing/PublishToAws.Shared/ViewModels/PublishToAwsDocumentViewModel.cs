using System;
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
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Telemetry;
using Amazon.Runtime;

using AWS.Deploy.ServerMode.Client;

using log4net;

using Microsoft.VisualStudio.Threading;

using Task = System.Threading.Tasks.Task;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// This ViewModel contains the functionality and data that
    /// backs the "Publish to AWS" document tab (<see cref="Views.PublishApplicationView"/>)
    /// </summary>
    public class PublishToAwsDocumentViewModel : BaseModel, IPublishToAwsProperties
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
            nameof(CredentialsId),
            nameof(Region),
            nameof(Recommendation),
            nameof(RepublishTarget),
            nameof(IsRepublish),
            nameof(ConfigurationDetails),
        };

        public static readonly IList<string> PublishAffectingProperties = new List<string>()
        {
            nameof(RepublishTarget),
            nameof(Recommendation),
            nameof(IsRepublish),
            nameof(PublishStackName)
        };

        private readonly PublishApplicationContext _publishContext;
        
        private IDeployToolController _deployToolController;
        private IDeploymentCommunicationClient _deploymentCommunicationClient;
        private bool _isLoading;
        private bool _publishTargetsLoaded;
        private bool _isOptionsBannerEnabled;
        private bool _isFailureBannerEnabled;
        private bool _isOldPublishExperienceEnabled;
        private bool _isRepublish;
        private bool _isDefaultConfig = true;
        private ICredentialIdentifier _credentialsId;
        private string _projectPath;
        private string _projectName;
        private string _publishSummary;
        private string _targetDescription;
        private ToolkitRegion _region;
        private PublishViewStage _viewStage;
        private PublishCommand _publishToAwsCommand;
        private ConfigCommand _configTargetCommand;
        private TargetCommand _backToTargetCommand;
        private ICommand _startOverCommand;
        private ICommand _persistOptionsSettingsCommand;
        private ICommand _closeFailureBannerCommand;
        private ICommand _learnMoreCommand;
        private ICommand _feedbackCommand;
        private ICommand _reenableOldPublishCommand;
        private ICommand _stackViewerCommand;
        private ICommand _copyToClipboardCommand;
        private ProgressStatus _progressStatus;
        private string _deploymentSessionId;
        private string _publishProgress;
        private PublishRecommendation _recommendation;
        private RepublishTarget _republishTarget;
        private string _targetRecipe;
        private ICollectionView _republishCollectionView;
        private string _publishedStackName;

        private string _stackName;

        private string _publishStackName;

        private bool _isPublishing;
        private Stopwatch _publishDuration = new Stopwatch();
        private Stopwatch _workflowDuration = new Stopwatch();


        private ObservableCollection<PublishRecommendation> _recommendations =
            new ObservableCollection<PublishRecommendation>();

        private ObservableCollection<RepublishTarget> _republishTargets =
            new ObservableCollection<RepublishTarget>();
        private ObservableCollection<TargetSystemCapability> _systemCapabilities =
            new ObservableCollection<TargetSystemCapability>();
        private ObservableCollection<ConfigurationDetail> _configurationDetails =
            new ObservableCollection<ConfigurationDetail>();
        private ObservableCollection<PublishResource> _publishResources =
            new ObservableCollection<PublishResource>();

        public readonly MissingCapabilities MissingCapabilities =
            new MissingCapabilities();


        public PublishToAwsDocumentViewModel(PublishApplicationContext publishContext)
        {
            _publishContext = publishContext;
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

        /// <summary>
        /// Represents the republish targets collection view displayed in the Targets view
        /// </summary>
        public ICollectionView RepublishCollectionView
        {
            get => _republishCollectionView;
            set => SetProperty(ref _republishCollectionView, value);
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
        /// Whether or not the UI should show the publish failure banner
        /// </summary>
        public bool IsFailureBannerEnabled
        {
            get => _isFailureBannerEnabled;
            set => SetProperty(ref _isFailureBannerEnabled, value);
        }

        /// <summary>
        /// Indicates whether or not the old publish experience related menu items are visible in the UI
        /// </summary>
        public bool IsOldPublishExperienceEnabled
        {
            get => _isOldPublishExperienceEnabled;
            set => SetProperty(ref _isOldPublishExperienceEnabled, value);
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
        /// Indicates whether or not the application is published using the default configurations
        /// </summary>
        public bool IsDefaultConfig
        {
            get => _isDefaultConfig;
            set => SetProperty(ref _isDefaultConfig, value);
        }

        /// <summary>
        /// Indicates whether or not a publish is in progress
        /// </summary>
        public bool IsPublishing
        {
            get => _isPublishing;
            set => SetProperty(ref _isPublishing, value);
        }

        /// <summary>
        /// The time it takes to publish the project
        /// </summary>
        public Stopwatch PublishDuration
        {
            get => _publishDuration;
            set => SetProperty(ref _publishDuration, value);
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
        /// The currently selected Credentials to Publish with
        /// </summary>
        public ICredentialIdentifier CredentialsId
        {
            get => _credentialsId;
            set => SetProperty(ref _credentialsId, value);
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
        /// Display-friendly name of the project that will be published
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// AWS Region to publish to
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set => SetProperty(ref _region, value);
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
        /// Indicates the current deployment progress status of the application 
        /// </summary>
        public ProgressStatus ProgressStatus
        {
            get => _progressStatus;
            set => SetProperty(ref _progressStatus, value);
        }

        /// <summary>
        /// The published application's CloudFormation stack name
        /// </summary>
        public string PublishedStackName
        {
            get => _publishedStackName;
            set => SetProperty(ref _publishedStackName, value);
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
            set => SetProperty(ref _deployToolController, value);
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
        /// Command that resets the overall state and takes the user back to the starting (Target selection) screen
        /// </summary>
        public ICommand StartOverCommand
        {
            get => _startOverCommand;
            set => SetProperty(ref _startOverCommand, value);
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
        /// Command that closes publish failure banner
        /// </summary>
        public ICommand CloseFailureBannerCommand
        {
            get => _closeFailureBannerCommand;
            set => SetProperty(ref _closeFailureBannerCommand, value);
        }
        /// <summary>
        /// Command that allows viewing the created CloudFormation stack
        /// </summary>
        public ICommand StackViewerCommand
        {
            get => _stackViewerCommand;
            set => SetProperty(ref _stackViewerCommand, value);
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
        /// Command that re-enable the previous publish experiences such as `Publish to AWS Container` and `Publish to Beanstalk`
        /// </summary>
        public ICommand ReenableOldPublishCommand
        {
            get => _reenableOldPublishCommand;
            set => SetProperty(ref _reenableOldPublishCommand, value);
        }

        /// <summary>
        /// Command that copies to clipboard the published resources details
        /// </summary>
        public ICommand CopyToClipboardCommand
        {
            get => _copyToClipboardCommand;
            set => SetProperty(ref _copyToClipboardCommand, value);
        }

        /// <summary>
        /// Stack name for the deployed application
        /// </summary>
        public string StackName
        {
            get => _stackName;
            set => SetProperty(ref _stackName, value);
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
        /// The target to use when publishing the user's application.
        /// </summary>
        public PublishRecommendation Recommendation
        {
            get => _recommendation;
            set => SetProperty(ref _recommendation, value);
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

        public ObservableCollection<PublishResource> PublishResources
        {
            get => _publishResources;
            set => SetProperty(ref _publishResources, value);
        }

        /// <summary>
        /// The target to use from list of existing targets
        /// in Republish view of Select Targets UI
        /// </summary>
        public RepublishTarget RepublishTarget
        {
            get => _republishTarget;
            set => SetProperty(ref _republishTarget, value);
        }

        /// <summary>
        /// Existing publish targets that show up in the Republish view
        /// </summary>
        public ObservableCollection<RepublishTarget> RepublishTargets
        {
            get => _republishTargets;
            set => SetProperty(ref _republishTargets, value);
        }

        public ObservableCollection<TargetSystemCapability> SystemCapabilities
        {
            get => _systemCapabilities;
            set => SetProperty(ref _systemCapabilities, value);
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

        /// <summary>
        /// Deployment progress messages displayed in the Publish in Progress View
        /// </summary>
        public string PublishProgress
        {
            get => _publishProgress;
            set => SetProperty(ref _publishProgress, value);
        }
        public PublishApplicationContext PublishContext => _publishContext;

        public bool IsSessionEstablished => !string.IsNullOrWhiteSpace(SessionId);

        public bool IsStackNameSet => !string.IsNullOrWhiteSpace(StackName);

        public string RegionDisplay => Region?.DisplayName ?? "";

        public string CredentialsDisplay => CredentialsId?.DisplayName ?? "";

        public bool HasValidationErrors()
        {
            var configurationDetailValidation = ConfigurationDetails?.GetDetailAndDescendants()
                       .Any(detail => detail.HasErrors)
                   ?? false;
            if (configurationDetailValidation)
                return true;

            var systemCapabilitiesValidation = SystemCapabilities?.Any() ?? false;
            if (systemCapabilitiesValidation)
                return true;

            return false;
        }

        /// <summary>
        /// Resolves <see cref="AWSCredentials"/> to be used with Deploying.
        /// </summary>
        public Task<AWSCredentials> GetCredentials()
        {
            return Task.FromResult(_publishContext.ConnectionManager.ActiveCredentials);
        }

        public async Task RestartDeploymentSessionAsync(CancellationToken cancellationToken)
        {
            await StopDeploymentSession(cancellationToken).ConfigureAwait(false);
            await StartDeploymentSession(cancellationToken).ConfigureAwait(false);

            await Task.WhenAll(
                JoinDeploymentSession(),
                SetRepublishTargets(new ObservableCollection<RepublishTarget>(), cancellationToken),
                SetRecommendations(new ObservableCollection<PublishRecommendation>(), cancellationToken)
            ).ConfigureAwait(false);
        }

        /// <summary>
        /// Defines a deployment session on the CLI Server for this project, if one isn't already defined.
        /// <see cref="SessionId"/> is assigned the resulting Session Id.
        /// </summary>
        /// <returns>True on success (or session already defined), False on error</returns>
        public async Task StartDeploymentSession(CancellationToken cancellationToken)
        {
            try
            {
                if (IsSessionEstablished)
                {
                    return;
                }

                Logger.Debug("Starting a deployment session");

                var response = await DeployToolController.StartSessionAsync(Region.Id, ProjectPath, cancellationToken).ConfigureAwait(false);

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                SessionId = response.SessionId;
                PublishStackName = response.DefaultApplicationName;

                Logger.Debug("Deployment session started");
            }
            catch (Exception e)
            {
                throw new SessionException("Unable to start a deployment session. Please try opening the publish experience again.", e);
            }
        }

        /// <summary>
        /// Removes this project's deployment session from the CLI Server.
        /// <see cref="SessionId"/> is cleared.
        /// </summary>
        /// <returns>True on success, False on error</returns>
        public async Task StopDeploymentSession(CancellationToken cancellationToken)
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
                throw new SessionException("Error stopping the deployment session. You may need to manually terminate the AWS Deploy Tool in your process manager.", e);
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
        ///
        /// </summary>
        /// <returns>True on success, False on error</returns>
        public async Task RefreshRecommendations(CancellationToken cancellationToken)
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
                throw new PublishException("Error getting deployment recommendations. Please reload the publish experience.", e);
            }
            finally
            {
                await SetRecommendations(recommendations, cancellationToken);

                var message = $"Publish targets found for {ProjectName}: {recommendations.Count}";
                Logger.Debug(message);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
            }
        }

        /// <summary>
        /// Loads existing publishing targets for this project.
        /// <see cref="RepublishTargets"/> is populated with any loaded targets
        /// </summary>
        /// <returns>True on success, False on error</returns>
        public async Task RefreshExistingTargets(CancellationToken cancellationToken)
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
                throw new PublishException("Error getting existing publish targets. Please reload the publish experience.", e);
            }
            finally
            {
                await SetRepublishTargets(targets, cancellationToken);

                var message = $"Republish targets found for {ProjectName}: {targets.Count}";
                Logger.Debug(message);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
            }
        }

        public async Task RefreshSystemCapabilities(CancellationToken cancellationToken)
        {
            var capabilities = new ObservableCollection<TargetSystemCapability>();
            var recipeId = string.Empty;
            try
            {
                ThrowIfSessionIsNotCreated();
                recipeId = GetPublishRecipeId();

                var systemCapabilities = await DeployToolController.GetCompatibilityAsync(SessionId, cancellationToken)
                    .ConfigureAwait(false);

                capabilities = new ObservableCollection<TargetSystemCapability>(systemCapabilities);
            }
            catch (Exception e)
            {
                throw new PublishException("Error getting system compatibilities for given targets.", e);
            }
            finally
            {
                await SetSystemCapabilities(capabilities, cancellationToken);
                UpdateMissingCapabilities(recipeId, capabilities);
                if (capabilities.Any())
                {
                    var message = $"{ProjectName} cannot be published to {Recommendation.Name}, system dependencies are missing ({capabilities.Count}).";
                    Logger.Debug(message);
                    _publishContext.ToolkitShellProvider.OutputToHostConsole(message, true);
                }
            }
        }

        private void UpdateMissingCapabilities(string recipeId, ObservableCollection<TargetSystemCapability> capabilities)
        {
            MissingCapabilities.Update(recipeId, capabilities);
        }

        /// <summary>
        /// Connects deployment client to the session such that it can listen to logs of the deployment session
        /// </summary>
        /// <returns></returns>
        public async Task JoinDeploymentSession()
        {
            ThrowIfSessionIsNotCreated();

            await DeploymentClient.JoinSession(SessionId);
            // register callback to log all deployment output in the progress dialog
            DeploymentClient.ReceiveLogAllLogAction = UpdateDeploymentProgress;
        }

        /// <summary>
        /// Loads the list of configuration settings for the selected target <see cref="Recommendation"/> <see cref="Republish Target"/>
        /// <see cref="ConfigurationDetails"/> is populated with any loaded configuration details
        /// A selected target needs to be defined prior to making this call.
        /// </summary>
        public async Task RefreshTargetConfigurations(CancellationToken cancellationToken)
        {
            IList<ConfigurationDetail> configSettings = new List<ConfigurationDetail>();

            try
            {
                ThrowIfSessionIsNotCreated();

                configSettings = await DeployToolController.GetConfigSettings(SessionId, cancellationToken);
            }
            catch (Exception e)
            {
                throw new PublishException("Unable to retrieve configuration details.", e);
            }
            finally
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(configSettings);
            }
        }

        /// <summary>
        /// Loads possible values associated with the configuration details for the selected target
        /// <see cref="ConfigurationDetails"/> is re-populated with updated configuration details
        /// Configuration details must be initially retrieved prior to making this call.
        /// </summary>
        public async Task RefreshConfigurationSettingValues(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfSessionIsNotCreated();
                var configSettings = await DeployToolController.UpdateConfigSettingValuesAsync(SessionId, ConfigurationDetails.ToList(), cancellationToken);

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                ConfigurationDetails = new ObservableCollection<ConfigurationDetail>(configSettings);
            }
            catch (Exception ex)
            {
                throw new PublishException("Unable to update configuration detail values", ex);
            }
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

        public async Task ClearPublishedResources(CancellationToken cancellationToken)
        {
            await SetPublishResources(string.Empty, Enumerable.Empty<PublishResource>().ToList(), cancellationToken)
                .ConfigureAwait(false);
        }

        /// <summary>
        ///  Loads the list of resources created once the application is published
        /// <see cref="PublishResources"/> is populated with any published resources
        /// </summary>
        /// <param name="cancellationToken"></param>
        public async Task RefreshPublishedResources(CancellationToken cancellationToken)
        {
            IList<PublishResource> resource = new List<PublishResource>();
            var stackId = string.Empty;
            try
            {
                ThrowIfSessionIsNotCreated();

                var details = await DeployToolController.GetDeploymentDetails(SessionId, cancellationToken);

                stackId = details?.StackId;
                var resourceSummaries = details?.DisplayedResources;
                resource = resourceSummaries.Select(x => x.AsPublishResource()).ToList();
            }
            catch (Exception e)
            {
                Logger.Error("Unable to retrieve published resource details.", e);
                throw;
            }
            finally
            {
                await SetPublishResources(stackId, resource, cancellationToken);
            }
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
        public async Task SetDeploymentTarget(CancellationToken cancellationToken)
        {
            try
            {
                ThrowIfSessionIsNotCreated();

                await DeployToolController.SetDeploymentTarget(SessionId, StackName, GetPublishRecipeId(), IsRepublish, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.Error("Error setting deployment target", e);
                throw;
            }
        }

        /// <summary>
        /// Publishes this project.
        /// State is expected to be valid prior to calling this function.
        /// </summary>
        public async Task PublishApplication()
        {
            PublishDuration.Start();
            var finalStatusMessage = new StringBuilder();
            var progressStatus = ProgressStatus.Fail;
            var result = Result.Failed;
            var errorCode = string.Empty;
            try
            {
                SetIsFailureBannerEnabled(false);
                _publishContext.ToolkitShellProvider.OutputToHostConsole($"Starting to publish {ProjectName}");
                await DeployToolController.StartDeploymentAsync(SessionId).ConfigureAwait(false);

                // wait for the deployment to finish
                var deployStatusOutput = await WaitForDeployment(SessionId).ConfigureAwait(false);
                _publishContext.ToolkitShellProvider.UpdateStatus($"{DeploymentStatusMessage}{deployStatusOutput.Status}");

                if (deployStatusOutput.Status != DeploymentStatus.Success)
                {
                    finalStatusMessage.Append($"{StackName} could not be published as {TargetRecipe}");
                    progressStatus = ProgressStatus.Fail;
                    result = Result.Failed;
                    if (deployStatusOutput.Exception != null)
                    {
                        errorCode = deployStatusOutput.Exception.ErrorCode;
                        finalStatusMessage.Append($": {deployStatusOutput.Exception.Message}");
                    }
                }
                else
                {
                    finalStatusMessage.Append($"{StackName} Published as {TargetRecipe}");
                    progressStatus = ProgressStatus.Success;
                    result = Result.Succeeded;
                }
            }
            catch (Exception e)
            {
                finalStatusMessage.Append($"{ProjectName} failed to publish to AWS.");
                progressStatus = ProgressStatus.Fail;
                result = Result.Failed;
                _publishContext.ToolkitShellProvider.UpdateStatus($"{DeploymentStatusMessage}Error");
                throw;
            }
            finally
            {
                await ReportFinalStatus(progressStatus, finalStatusMessage.ToString());
                PublishDuration.Stop();
                SetIsPublishing(false);
                SetIsFailureBannerEnabled(progressStatus == ProgressStatus.Fail);
                RecordPublishDeployMetric(result, errorCode);
            }
        }

        public void SetIsPublishing(bool value)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                IsPublishing = value;
            });
        }

        private void SetIsFailureBannerEnabled(bool value)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                IsFailureBannerEnabled = value;
            });
        }

        public void SetPublishTargetsLoaded(bool value)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                PublishTargetsLoaded = value;
            });
        }

        private string GetPublishRecipeId()
        {
            return IsRepublish ? RepublishTarget?.RecipeId : Recommendation?.RecipeId;
        }


        /// <summary>
        /// Updates properties required for publishing user's application based on current target view
        /// i.e. publish/republish
        /// </summary>
        public async Task UpdateRequiredPublishProperties(CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                StackName = GetUpdatedStackName();
                TargetRecipe = GetUpdatedTargetRecipe();
                TargetDescription = GetUpdatedRecommendationDescription();
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
        /// Keep polling for deployment status and wait for it to finish until timeout
        /// </summary>
        /// <param name="sessionId"></param>
        private async Task<GetDeploymentStatusOutput> WaitForDeployment(string sessionId)
        {
            await WaitUntil(async () =>
            {
                var status = (await DeployToolController.GetDeploymentStatusAsync(sessionId))?.Status;
                var progressStatus = $"{DeploymentStatusMessage}{status}";
                await SetProgressStatus(ProgressStatus.Loading);
                _publishContext.ToolkitShellProvider.UpdateStatus(progressStatus);
                return status != null && status != DeploymentStatus.Executing;
            }, TimeSpan.FromSeconds(1));

            return await DeployToolController.GetDeploymentStatusAsync(sessionId);
        }

        /// <summary>
        /// Helper method for waiting until a task is finished
        /// </summary>
        /// <param name="predicate">Termination condition for breaking the wait loop</param>
        /// <param name="frequency">Interval between the two executions of the task</param>
        private async Task WaitUntil(Func<Task<bool>> predicate, TimeSpan frequency)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!await predicate())
                {
                    await Task.Delay(frequency);
                }
            });
            await waitTask;
        }

        /// <summary>
        /// Updates deployment progress status in the Publish in Progress View
        /// </summary>
        /// <param name="progressMessage"></param>
        private void UpdateDeploymentProgress(string progressMessage)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                PublishProgress += string.Concat(progressMessage, Environment.NewLine);
            });
        }

        /// <summary>
        /// Sets the Recommendations list and either re-selects the
        /// same Recommendation (by RecipeId) or a most recommended target.
        /// </summary>
        private async Task SetRecommendations(ObservableCollection<PublishRecommendation> recommendations,
            CancellationToken cancellationToken)
        {
            try
            {
                var selectedRecipeId = Recommendation?.RecipeId;

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                Recommendations = recommendations;

                Recommendation = recommendations.FirstOrDefault(r => r.RecipeId == selectedRecipeId) ??
                                 recommendations.FirstOrDefault(r => r.IsRecommended) ??
                                 recommendations.FirstOrDefault();
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
        private async Task SetRepublishTargets(ObservableCollection<RepublishTarget> targets,
            CancellationToken cancellationToken)
        {
            try
            {
                var selectedStack = RepublishTarget?.Name;

                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                RepublishTargets = targets;

                RepublishTarget = targets.FirstOrDefault(r => r.Name == selectedStack) ??
                                  targets.FirstOrDefault(r => r.IsRecommended) ??
                                  targets.FirstOrDefault();

                UpdateRepublishCollectionView();
              
            }
            catch (Exception e)
            {
                Logger.Error("Error setting republish targets list", e);
            }
        }

        private async Task SetPublishResources(string stackId,
            IList<PublishResource> resource, CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

                PublishedStackName = stackId;
                PublishResources = new ObservableCollection<PublishResource>(resource);
            }
            catch (Exception e)
            {
                Logger.Error("Error setting published resources", e);
            }
        }

        public async Task SetIsRepublish(Boolean value, CancellationToken cancelToken)
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                IsRepublish = value;
            });
        }

        public async Task InitializePublishTargets(CancellationToken cancelToken)
        {
            var republishTargets = await DeployToolController.GetRepublishTargetsAsync(SessionId, ProjectPath, cancelToken);
            await SetIsRepublish(republishTargets.Any(), cancelToken);
            await Task.WhenAll(RefreshExistingTargets(cancelToken), RefreshRecommendations(cancelToken)).ConfigureAwait(false);
        }

        private void UpdateRepublishCollectionView()
        {
            RepublishCollectionView = CollectionViewSource.GetDefaultView(RepublishTargets);
            RepublishCollectionView.GroupDescriptions.Clear();
            RepublishCollectionView.GroupDescriptions.Add(
                new PropertyGroupDescription(nameof(RepublishTarget.Category)));
        }

        private async Task SetSystemCapabilities(ObservableCollection<TargetSystemCapability> capabilities,
            CancellationToken cancellationToken)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                SystemCapabilities = capabilities;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting system capabilities list", e);
            }
        }

        /// <summary>
        /// Sets the Deployment Progress Status
        /// </summary>
        private async Task SetProgressStatus(ProgressStatus status)
        {
            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync();
                ProgressStatus = status;
            }
            catch (Exception e)
            {
                Logger.Error("Error setting progress status.", e);
            }
        }

        /// <summary>
        /// Reports final deployment status in the Publish View
        /// </summary>
        /// <param name="status">final deployment status </param>
        /// <param name="statusMessage">final deployment status message</param>
        private async Task ReportFinalStatus(ProgressStatus status, string statusMessage)
        {
            try
            {
                await SetProgressStatus(status);
                UpdateDeploymentProgress(statusMessage);
                _publishContext.ToolkitShellProvider.OutputToHostConsole(statusMessage,
                    true);
            }
            catch (Exception e)
            {
                Logger.Error("Error reporting final deployment status.", e);
            }
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
            return IsRepublish ? RepublishTarget?.Name ?? "" : PublishStackName;
        }

        private string GetUpdatedTargetRecipe()
        {
            return IsRepublish ? RepublishTarget?.RecipeName ?? "" : Recommendation?.Name ?? "";
        }

        private string GetUpdatedRecommendationDescription()
        {
            return IsRepublish ? RepublishTarget?.Description ?? "" : Recommendation?.Description ?? "";
        }

        protected async Task LoadOptionsButtonSettingsAsync()
        {
            var showPublishBanner = true;
            var showOldExperience = true;
            try
            {
                var publishSettings = await _publishContext.PublishSettingsRepository.GetAsync();
                showPublishBanner = publishSettings.ShowPublishBanner;
                showOldExperience = publishSettings.ShowOldPublishExperience;
            }
            catch (Exception e)
            {
                Logger.Error("Error retrieving show publish banner settings", e);
            }
            finally
            {
                await SetOptionsButtonPropertiesAsync(showPublishBanner, showOldExperience);
            }
        }


        /// <summary>
        /// Sets the options button related properties 
        /// </summary>
        /// <param name="result"></param>
        /// <param name="showOldExperience"></param>
        /// <returns></returns>
        private async Task SetOptionsButtonPropertiesAsync(bool result, bool showOldExperience)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            IsOptionsBannerEnabled = result;
            IsOldPublishExperienceEnabled = showOldExperience;
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

        public void RecordPublishEndMetric(bool published)
        {
            Dictionary<string, object> capabilityMetrics = CreateCapabilityMetrics();
            this._publishContext.TelemetryLogger.RecordPublishEnd(new PublishEnd()
            {
                AwsAccount = _publishContext.ConnectionManager.ActiveAccountId,
                AwsRegion = _publishContext.ConnectionManager.ActiveRegion.Id,
                Duration = WorkflowDuration.Elapsed.TotalMilliseconds,
                Published = published
            }, capabilityMetrics);
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

        private void RecordPublishDeployMetric(Result result, string errorCode)
        {
            var payload = new PublishDeploy()
            {
                AwsAccount = _publishContext.ConnectionManager.ActiveAccountId,
                AwsRegion = _publishContext.ConnectionManager.ActiveRegion?.Id,
                Result = result,
                Duration = PublishDuration.Elapsed.TotalMilliseconds,
                InitialPublish = !IsRepublish,
                DefaultConfiguration = IsDefaultConfig,
                RecipeId = GetPublishRecipeId()
            };

            if (payload.InitialPublish)
            {
                payload.RecommendedTarget = Recommendation.IsRecommended;
            }

            if (!string.IsNullOrWhiteSpace(errorCode))
            {
                payload.ErrorCode = errorCode;
            }

            this._publishContext.TelemetryLogger.RecordPublishDeploy(payload);
        }

        public void RecordOptOutMetric(Result result)
        {
            this._publishContext.TelemetryLogger.RecordPublishOptOut(new PublishOptOut()
            {
                AwsAccount = _publishContext.ConnectionManager.ActiveAccountId,
                AwsRegion = _publishContext.ConnectionManager.ActiveRegion?.Id,
                Result = result
            });
        }
    }
}
