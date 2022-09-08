using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.CloudWatch.Logs.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Feedback;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Util;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Logs.ViewModels
{
    /// <summary>
    /// Base view model for viewing log resources such as groups, streams etc
    /// </summary>
    public abstract class BaseLogsViewModel : BaseModel, IDisposable
    {
        public static readonly string FeedbackSource = "CloudWatch Logs";
        protected static readonly TextSuggestion PrefixSearchSuggestion =
            new TextSuggestion { Message = "Searching is by prefix only. Check search prefix for any typos." };

        protected readonly ToolkitContext ToolkitContext;
        protected readonly ICloudWatchLogsRepository Repository;

        protected bool _loadingLogs = false;
        private bool _hasInitialized = false;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _filterText;
        private string _nextToken;
        private string _errorMessage = string.Empty;
        private ICommand _refreshCommand;
        private ICommand _copyArnCommand;
        private ICommand _feedbackCommand;
        private int _lastFilterHash;
        private ObservableCollection<Suggestion> _noResultSuggestions;

        protected BaseLogsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
            Repository = repository;
        }

        public bool HasInitialized
        {
            get => _hasInitialized;
            set => SetProperty(ref _hasInitialized, value);
        }

        public string NextToken
        {
            get => _nextToken;
            set => _nextToken = value;
        }

        public bool LoadingLogs
        {
            get => _loadingLogs;
            private set => SetProperty(ref _loadingLogs, value);
        }

        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand RefreshCommand
        {
            get => _refreshCommand;
            set => SetProperty(ref _refreshCommand, value);
        }

        public ObservableCollection<Suggestion> NoResultSuggestions =>
            _noResultSuggestions ?? (_noResultSuggestions = CreateSuggestions());

        protected virtual ObservableCollection<Suggestion> CreateSuggestions()
        {
            var suggestions = new ObservableCollection<Suggestion>
            {
                new TextSuggestion
                {
                    Message = "Searching is case-sensitive. Try a search term that matches case."
                }
            };
            return suggestions;
        }
        public void RecordRefreshMetric()
        {
            ToolkitContext.TelemetryLogger.RecordCloudwatchlogsRefresh(new CloudwatchlogsRefresh()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(
                    ConnectionSettings.GetAccountId(ToolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(ConnectionSettings.Region),
                CloudWatchResourceType = GetCloudWatchResourceType(),
            });
        }

        public ICommand CopyArnCommand => _copyArnCommand ?? (_copyArnCommand = CreateCopyArnCommand());

        /// <summary>
        /// Command that shows the feedback panel for the CloudWatch Logs integration
        /// </summary>
        public ICommand FeedbackCommand => _feedbackCommand ?? (_feedbackCommand = new SendFeedbackCommand(ToolkitContext));

        private ICommand CreateCopyArnCommand()
        {
            return Logs.Commands.CopyArnCommand.Create(this, ToolkitContext.ToolkitHost);
        }

        public void RecordCopyArnMetric(bool copyResult, CloudWatchResourceType resourceType)
        {
            ToolkitContext.TelemetryLogger.RecordCloudwatchlogsCopyArn(new CloudwatchlogsCopyArn()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(ConnectionSettings.GetAccountId(ToolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(ConnectionSettings.Region),
                CloudWatchResourceType = resourceType,
                Result = copyResult ? Result.Succeeded : Result.Failed,
            });
        }

        public AwsConnectionSettings ConnectionSettings => Repository?.ConnectionSettings;

        protected CancellationToken CancellationToken => _tokenSource.Token;

        public virtual string GetLogTypeDisplayName() => "log resources";

        public abstract Task RefreshAsync();

        public abstract CloudWatchResourceType GetCloudWatchResourceType();

        public abstract Task LoadAsync();

        public void SetErrorMessage(string errorMessage)
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                ErrorMessage = errorMessage;
            });
        }

        protected IDisposable CreateLoadingLogsScope()
        {
            SetLoadingLogs(true);
            return new DisposingAction(() =>
            {
                SetLoadingLogs(false);
            });
        }
        protected bool IsFilteredByText()
        {
            return !string.IsNullOrWhiteSpace(FilterText);
        }

        protected virtual bool IsFiltered()
        {
            return IsFilteredByText();
        }

        public void SetLoadingLogs(bool value)
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                LoadingLogs = value;
            });
        }
        public void ResetCancellationToken()
        {
            CancelExistingToken();
            _tokenSource = new CancellationTokenSource();
        }

        /// <summary>
        /// Indicates if the last page of log groups has already been loaded/retrieved
        /// </summary>
        protected virtual bool IsLastPageLoaded()
        {
            return HasInitialized && string.IsNullOrEmpty(NextToken);
        }

        protected void UpdateHasInitialized()
        {
            if (!HasInitialized)
            {
                ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
                {
                    HasInitialized = true;
                });
            }
        }

        protected void CancelExistingToken()
        {
            if (_tokenSource != null)
            {
                _tokenSource.Cancel();
                _tokenSource.Dispose();
                _tokenSource = null;
            }
        }

        public void Dispose()
        {
            CancelExistingToken();
            Repository?.Dispose();
        }

        protected void RecordFilterMetric(bool filterByText, bool filterByTime, TaskStatus taskStatus,
          ITelemetryLogger telemetryLogger, int suppliedFilterHash = -1)
        {
            // Only emit metric if the filter has changed since last time
            var filterHash = (suppliedFilterHash == -1) ?  CreateFilterHash() : suppliedFilterHash;
            if (filterHash == _lastFilterHash)
            {
                return;
            }

            _lastFilterHash = filterHash;
            if (!IsFiltered())
            {
                return;
            }

            RecordFilterMetricInternal(filterByText, filterByTime, taskStatus, telemetryLogger);
        }

        protected virtual int CreateFilterHash()
        {
            return FilterText?.GetHashCode() ?? 0;
        }

        private void RecordFilterMetricInternal(bool filterByText, bool filterByTime,
            TaskStatus taskStatus,
            ITelemetryLogger telemetryLogger)
        {
            CloudwatchlogsFilter metric = new CloudwatchlogsFilter()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(
                    ConnectionSettings.GetAccountId(ToolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(ConnectionSettings.Region),
                CloudWatchResourceType = GetCloudWatchResourceType(),
                Result = taskStatus.AsMetricsResult(),
            };

            if (filterByText)
            {
                metric.HasTextFilter = true;
            }

            if (filterByTime)
            {
                metric.HasTimeFilter = true;
            }

            telemetryLogger.RecordCloudwatchlogsFilter(metric);
        }
    }
}
