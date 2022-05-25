using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Util;

using AWS.Deploy.ServerMode.Client;

using log4net;

using Microsoft.VisualStudio.Threading;

namespace Amazon.AWSToolkit.Publish.ViewModels
{
    /// <summary>
    /// The state relating to actually publishing a project
    /// </summary>
    public class PublishProjectViewModel : BaseModel
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(PublishProjectViewModel));

        /// <summary>
        /// Stack name for the deployed application
        /// </summary>
        public string StackName
        {
            get => _stackName;
            set => SetProperty(ref _stackName, value);
        }

        public string RecipeName
        {
            get => _recipeName;
            set => SetProperty(ref _recipeName, value);
        }

        public string RegionName
        {
            get => _regionName;
            set => SetProperty(ref _regionName, value);
        }

        public string SessionId
        {
            get => _sessionId;
            set => SetProperty(ref _sessionId, value);
        }

        public DeploymentArtifact ArtifactType
        {
            get => _artifactType;
            set => SetProperty(ref _artifactType, value);
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
        /// Deployment progress messages
        /// </summary>
        public ObservableCollection<DeploymentMessageGroup> DeploymentMessages { get; } = new ObservableCollection<DeploymentMessageGroup>();

        /// <summary>
        /// The Id of the published artifact.
        ///
        /// Correlates with the PublishDestination's DeploymentArtifact (<see cref="DeploymentArtifact"/>)
        /// Eg: For CloudFormation Stacks: stack Id, Beanstalk Environments: environment name
        /// </summary>
        public string PublishedArtifactId
        {
            get => _publishedArtifactId;
            set => SetProperty(ref _publishedArtifactId, value);
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
        /// Whether or not the UI should show the publish failure banner
        /// </summary>
        public bool IsFailureBannerEnabled
        {
            get => _isFailureBannerEnabled;
            set => SetProperty(ref _isFailureBannerEnabled, value);
        }

        public ObservableCollection<PublishResource> PublishResources { get; } =
            new ObservableCollection<PublishResource>();

        /// <summary>
        /// Command that closes publish failure banner
        /// </summary>
        public ICommand CloseFailureBannerCommand
        {
            get => _closeFailureBannerCommand;
            set => SetProperty(ref _closeFailureBannerCommand, value);
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
        /// Command that copies to clipboard the published resources details
        /// </summary>
        public ICommand OpenUrlCommand => new RelayCommand(url =>
        {
            try
            {
                _publishContext.ToolkitShellProvider.OpenInBrowser(url as string, true);
            }
            catch (Exception ex)
            {
                Logger.Error($"Failed to open url", ex);
                _publishContext.ToolkitShellProvider.OutputToHostConsole("Error opening url in browser");
            }
        });

        /// <summary>
        /// Command that resets the overall state and takes the user back to the starting (Target selection) screen
        /// </summary>
        public ICommand StartOverCommand
        {
            get => _startOverCommand;
            set => SetProperty(ref _startOverCommand, value);
        }

        /// <summary>
        /// Command that allows viewing the deployment artifact
        /// 
        /// Correlates with the PublishDestination's DeploymentArtifact (<see cref="PublishDestinationBase.DeploymentArtifact"/>)
        /// Eg: CloudFormation Stacks, Beanstalk Environments, ...
        /// </summary>
        public ICommand ViewPublishedArtifactCommand
        {
            get => _viewPublishedArtifactCommand;
            set => SetProperty(ref _viewPublishedArtifactCommand, value);
        }

        private bool _isPublishing;
        private string _stackName;
        private string _recipeName;
        private string _regionName;
        private string _sessionId;
        private DeploymentArtifact _artifactType;

        private DeploymentMessageGroup _currentMessageGroup;
        private bool _isFailureBannerEnabled;
        private ProgressStatus _progressStatus;
        private string _publishedArtifactId;

        private ICommand _closeFailureBannerCommand;
        private ICommand _copyToClipboardCommand;
        private ICommand _startOverCommand;
        private ICommand _viewPublishedArtifactCommand;

        private readonly PublishApplicationContext _publishContext;
        private IDeployToolController _deployToolController;

        public PublishProjectViewModel(PublishApplicationContext publishContext)
        {
            _publishContext = publishContext;
        }

        public void AppendLineDeploymentMessage(string text)
        {
            if (_currentMessageGroup == null)
            {
                // If there was no group created before the first message arrived, make one
                CreateMessageGroup("Publish to AWS", "");
            }

            _currentMessageGroup.AppendLine(text);
        }

        public void CreateMessageGroup(string groupName, string description)
        {
            if (_currentMessageGroup != null)
            {
                _currentMessageGroup.IsExpanded = false;
            }

            _currentMessageGroup = new DeploymentMessageGroup()
            {
                Name = groupName,
                Description = description,
            };

            DeploymentMessages.Add(_currentMessageGroup);
        }

        public void Clear()
        {
            StackName = string.Empty;
            RecipeName = string.Empty;
            RegionName = string.Empty;
            SessionId = string.Empty;
            IsPublishing = false;
            DeploymentMessages.Clear();
            _currentMessageGroup = null;
            PublishedArtifactId = string.Empty;
            ProgressStatus = ProgressStatus.Loading;
            IsFailureBannerEnabled = false;
            PublishResources.Clear();
        }

        public IDisposable IsPublishedScope()
        {
            _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
            {
                IsPublishing = true;
            });

            return new DisposingAction(() =>
            {
                _publishContext.ToolkitShellProvider.ExecuteOnUIThread(() =>
                {
                    IsPublishing = false;
                });
            });
        }

        public void SetDeployToolController(IDeployToolController deployToolController)
        {
            _deployToolController = deployToolController;
        }

        /// <summary>
        /// Loads the list of resources created with this deployment session, then
        /// updates <see cref="PublishResources"/> and <see cref="PublishedArtifactId"/>
        /// </summary>
        public async Task UpdatePublishedResourcesAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                throw new Exception("No deployment session available");
            }

            await _publishContext.PublishPackage.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            SetPublishResources(string.Empty, Enumerable.Empty<PublishResource>());

            await TaskScheduler.Default;

            var details = await _deployToolController.GetDeploymentDetailsAsync(SessionId, cancellationToken);
            if (details == null) { return; }

            var publishedArtifactId = GetPublishedArtifactId(details, ArtifactType);

            IEnumerable<PublishResource> publishedResources = details.DisplayedResources?
                                                                  .Select(x => x.AsPublishResource())
                                                              ?? Enumerable.Empty<PublishResource>();

            await _publishContext.PublishPackage.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            SetPublishResources(publishedArtifactId, publishedResources);
        }

        private void SetPublishResources(string publishedArtifactId, IEnumerable<PublishResource> publishedResources)
        {
            PublishedArtifactId = publishedArtifactId;
            PublishResources.Clear();
            PublishResources.AddAll(publishedResources);
        }

        private static string GetPublishedArtifactId(GetDeploymentDetailsOutput details,
            DeploymentArtifact artifactType)
        {
            // TODO : remove this once deploy API includes ECR Repo id as top level data
            if (artifactType == DeploymentArtifact.ElasticContainerRegistry)
            {
                return WorkaroundGetEcrRepoArtifactName(details);
            }

            return details.CloudApplicationName;
        }

        /// <summary>
        /// This is a workaround until the deploy API can surface the Id of the ECR repo as first-class data.
        /// </summary>
        private static string WorkaroundGetEcrRepoArtifactName(GetDeploymentDetailsOutput details)
        {
            return details?.DisplayedResources?.FirstOrDefault(x => x.Type == "Elastic Container Registry Repository")
                ?.Id;
        }

        /// <summary>
        /// Publishes this project to AWS.
        /// State is expected to be valid prior to calling this function.
        /// </summary>
        public async Task<PublishProjectResult> PublishProjectAsync()
        {
            PublishProjectResult publishResult = new PublishProjectResult();

            try
            {
                await TaskScheduler.Default;
                await _deployToolController.StartDeploymentAsync(SessionId).ConfigureAwait(false);

                // Wait for the deployment to finish
                var deployStatusOutput = await WaitForDeploymentAsync(SessionId).ConfigureAwait(false);

                if (deployStatusOutput.Status == DeploymentStatus.Success)
                {
                    publishResult.IsSuccess = true;
                }
                else
                {
                    publishResult.IsSuccess = false;
                    if (deployStatusOutput.Exception != null)
                    {
                        publishResult.ErrorCode = deployStatusOutput.Exception.ErrorCode;
                        publishResult.ErrorMessage = deployStatusOutput.Exception.Message;
                    }
                }
            }
            catch (Exception e)
            {
                publishResult.IsSuccess = false;

                // Take the exception message if we didn't already have a failure message
                if (string.IsNullOrWhiteSpace(publishResult.ErrorMessage))
                {
                    publishResult.ErrorMessage = e.GetExceptionInnerMessage();
                }
            }

            return publishResult;
        }


        /// <summary>
        /// Keep polling for deployment status and wait for it to finish until timeout
        /// </summary>
        /// <param name="sessionId"></param>
        private async Task<GetDeploymentStatusOutput> WaitForDeploymentAsync(string sessionId)
        {
            await WaitUntilAsync(async () =>
            {
                var status = (await _deployToolController.GetDeploymentStatusAsync(sessionId))?.Status;
                return status != null && status != DeploymentStatus.Executing;
            }, TimeSpan.FromSeconds(1));

            return await _deployToolController.GetDeploymentStatusAsync(sessionId);
        }

        /// <summary>
        /// Helper method for waiting until a task is finished
        /// </summary>
        /// <param name="predicate">Termination condition for breaking the wait loop</param>
        /// <param name="frequency">Interval between the two executions of the task</param>
        private async Task WaitUntilAsync(Func<Task<bool>> predicate, TimeSpan frequency)
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
    }
}
