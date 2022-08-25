using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.DateTimeRangePicker;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.CloudWatchLogs;

using log4net;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Logs.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log events/messages
    /// </summary>
    public class LogEventsViewModel : BaseLogsViewModel
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogEventsViewModel));
        private readonly DateTimeRangePickerViewModel _dateTimeRange = new DateTimeRangePickerViewModel(null, null);
        private string _logGroup;
        private string _logStream;
        private LogEvent _logEvent;
        private ICollectionView _logEventsView;
        private bool _isWrapped = false;
        private bool _isTimeFilterEnabled = false;
        private ICommand _continueLoadingCommand;
        private ICommand _cancelLoadingCommand;
        private PaginatedLoadingStatus _paginatedLoadingStatus = PaginatedLoadingStatus.None;
        // store and update previous token
        // GetLogEvents API(called when no filter is applied) always returns a non-null next token
        // it returns the same token if end of stream is reached
        private string _previousLoadingToken = null;

        private ObservableCollection<LogEvent> _logEvents =
            new ObservableCollection<LogEvent>();

        public LogEventsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext) : base(
            repository, toolkitContext)
        {
        }

        public DateTimeRangePickerViewModel DateTimeRange => _dateTimeRange;

        public ObservableCollection<LogEvent> LogEvents
        {
            get => _logEvents;
            private set => SetProperty(ref _logEvents, value);
        }

        /// <summary>
        /// Represents the log events collection view
        /// </summary>
        public ICollectionView LogEventsView
        {
            get => _logEventsView;
            set => SetProperty(ref _logEventsView, value);
        }

        public string LogGroup
        {
            get => _logGroup;
            set => SetProperty(ref _logGroup, value);
        }

        public string LogStream
        {
            get => _logStream;
            set => SetProperty(ref _logStream, value);
        }

        public LogEvent LogEvent
        {
            get => _logEvent;
            set => SetProperty(ref _logEvent, value);
        }

        public bool IsWrapped
        {
            get => _isWrapped;
            set => SetProperty(ref _isWrapped, value);
        }

        public bool IsTimeFilterEnabled
        {
            get => _isTimeFilterEnabled;
            set => SetProperty(ref _isTimeFilterEnabled, value);
        }

        public DateTime? StartTime => _dateTimeRange.GetFullStartTime();

        public DateTime? EndTime => _dateTimeRange.GetFullEndTime();

        public PaginatedLoadingStatus PaginatedLoadingStatus
        {
            get => _paginatedLoadingStatus;
            set => SetProperty(ref _paginatedLoadingStatus, value);
        }

        public ICommand ContinueLoadingCommand => _continueLoadingCommand ?? (_continueLoadingCommand = CreateContinueLoadingCommand());

        public ICommand CancelLoadingCommand => _cancelLoadingCommand ?? (_cancelLoadingCommand = CreateCancelLoadingCommand());

        public override string GetLogTypeDisplayName() => "log events";

        public override async Task RefreshAsync()
        {
            ResetState();
            await LoadAsync().ConfigureAwait(false);
        }

        public override CloudWatchResourceType GetCloudWatchResourceType() => CloudWatchResourceType.LogStream;

        public override async Task LoadAsync()
        {
            var hasFilterText = IsFilteredByText();
            await GetLogEventsAsync(hasFilterText, CancellationToken).ConfigureAwait(false);
        }

        private void ResetState()
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogEvents = new ObservableCollection<LogEvent>();
                LogEvent = null;
                HasInitialized = false;
                ErrorMessage = string.Empty;
                PaginatedLoadingStatus = PaginatedLoadingStatus.None;
                _previousLoadingToken = null;
            });
        }

        private async Task GetLogEventsAsync(bool hasFilterText, CancellationToken cancelToken)
        {
            TaskStatus status = TaskStatus.Fail;
            var filterHash = CreateFilterHash();
            async Task Load()
            {
                status = await GetNextLogEventsPageAsync(hasFilterText, cancelToken);
            }

            void Record(ITelemetryLogger logger)
            {
                var filterByText = hasFilterText;
                var filterByTime = IsFilteredByTime();
                RecordFilterMetric(filterByText, filterByTime, status, logger, filterHash);
            }

            await ToolkitContext.TelemetryLogger.InvokeAndRecordAsync(Load, Record);
        }

        private async Task<TaskStatus> GetNextLogEventsPageAsync(bool hasFilterText, CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();

                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return TaskStatus.Success;
                }

                _previousLoadingToken = NextToken;
                SetLoadingLogs(true);

                //if filtered by text, query FilterEventsAPI
                if (hasFilterText)
                {
                    await QueryFilterEventsAsync(cancelToken);
                }
                else
                {
                    await QueryLogEventsAsync(cancelToken);
                }

                // if there are no more pages, stop loading
                if (!HasMorePages())
                {
                    SetLoadingLogs(false);
                }

                return TaskStatus.Success;
            }
            catch (Exception e)
            {
                SetLoadingLogs(false);
                if (e is OperationCanceledException)
                {
                    Logger.Error("Operation to load log events was cancelled", e);
                    return TaskStatus.Cancel;
                }

                throw;
            }
        }

        protected override bool IsLastPageLoaded()
        {
            if (IsFilteredByText())
            {
                return base.IsLastPageLoaded();
            }
            //next token returned by GetLogEvents is same as previous one if last page is reached
            return HasInitialized && string.Equals(_previousLoadingToken, NextToken);
        }

        /// <summary>
        /// Determines whether there are more pages left depending on the request
        /// </summary>
        /// <returns></returns>
        public bool HasMorePages()
        {
            return IsFilteredByText()
                ? !string.IsNullOrWhiteSpace(NextToken)
                : !string.Equals(_previousLoadingToken, NextToken);
        }

        protected override bool IsFiltered()
        {
            return base.IsFiltered() || IsFilteredByTime();
        }

        private bool IsFilteredByTime()
        {
            return _isTimeFilterEnabled && (StartTime != null || EndTime != null);
        }

        private async Task<PaginatedLogResponse<LogEvent>> FilterLogEventsAsync(CancellationToken cancelToken)
        {
            var request = CreateFilterRequest();
            return await Repository.FilterLogEventsAsync(request, cancelToken).ConfigureAwait(false);
        }

        private async Task<PaginatedLogResponse<LogEvent>> RetrieveLogEventsAsync(CancellationToken cancelToken)
        {
            var request = CreateGetRequest();
            return await Repository.GetLogEventsAsync(request, cancelToken).ConfigureAwait(false);
        }

        private void UpdateLogEventProperties(PaginatedLogResponse<LogEvent> response, string previousLogMessage, CancellationToken token)
        {
            var logEvents = LogEvents.ToList().Concat(response.Values).ToList();
            LogEvents = new ObservableCollection<LogEvent>(logEvents);
            NextToken = response.NextToken;

            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                if (token.IsCancellationRequested)
                {
                    return;
                }
                LogEvent = LogEvents.FirstOrDefault(x => x.Message == previousLogMessage) ??
                           LogEvents.FirstOrDefault();
                LogEventsView = CollectionViewSource.GetDefaultView(LogEvents);
                UpdateHasInitialized();
            });
        }

        private void SetPaginatedLoadingStatus(PaginatedLoadingStatus state)
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                PaginatedLoadingStatus = state;
            });
        }
        private ICommand CreateContinueLoadingCommand()
        {
            return new RelayCommand(QueryFilterEvents);
        }

        private void QueryFilterEvents(object arg)
        {
            ExecuteOnBackgroundThread(QueryFilterEventsAsync);
        }

        protected virtual void ExecuteOnBackgroundThread(Func<Task> function)
        {
            Task.Run(async () =>
            {
                await function().ConfigureAwait(false);
            }).LogExceptionAndForget();
        }

        private async Task QueryLogEventsAsync(CancellationToken cancelToken)
        {
            var selectedLogEvent = LogEvent?.Message;
            var response = await RetrieveLogEventsAsync(cancelToken);
            UpdateLogEventProperties(response, selectedLogEvent, cancelToken);
        }

        private async Task QueryFilterEventsAsync()
        {
            try
            {
                ResetCancellationToken();
                await LoadAsync().ConfigureAwait(false);
            }
            catch(Exception e)
            {
                Logger.Error("Error loading log events", e);
                SetErrorMessage($"Error loading log events:{Environment.NewLine}{e.Message}");
            }
        }


        private async Task QueryFilterEventsAsync(CancellationToken cancelToken)
        {
            try
            {
                var selectedLogEvent = LogEvent?.Message;

                PaginatedLogResponse<LogEvent> response;
                var request = CreateFilterRequest();
                do
                {
                    response = await Repository.FilterLogEventsAsync(request, cancelToken).ConfigureAwait(false);
                    NextToken = response.NextToken;
                    UpdateHasInitialized();
                    request.NextToken = NextToken;
                    // add delay to rate limit number of requests
                    // GetLogEvents has a minimum limit of 5 requests per second in some regions. See https://docs.aws.amazon.com/AmazonCloudWatch/latest/logs/cloudwatch_limits_cwl.html 
                    // To avoid throttling, a 1 request per second rate is applied here in addition to surfacing a retry options, on the occasion it does occur
                    await Task.Delay(1000, cancelToken);
                } while (HasPendingLogEvents(response));

                UpdateLogEventProperties(response, selectedLogEvent, cancelToken);
                SetPaginatedLoadingStatus(PaginatedLoadingStatus.None);
            }
            catch (Exception e)
            {
                DeterminePaginatedLoadingStatus(e);
                throw;
            }
        }

        private void DeterminePaginatedLoadingStatus(Exception e)
        {
            switch (e)
            {
                case OperationCanceledException _:
                    //if cancelled, present user option to continue querying again
                    SetPaginatedLoadingStatus(PaginatedLoadingStatus.LoadMore);
                    break;
                case AmazonCloudWatchLogsException ex when string.Equals(ex.ErrorCode, "ThrottlingException"):
                    //if throttled, present retry option to user
                    SetPaginatedLoadingStatus(PaginatedLoadingStatus.Retry);
                    break;
                default:
                    // if any other exception, set status to none
                    SetPaginatedLoadingStatus(PaginatedLoadingStatus.None);
                    break;
            }
        }

        private ICommand CreateCancelLoadingCommand()
        {
            return new RelayCommand(CancelFilterEventsQuery);
        }

        private void CancelFilterEventsQuery(object obj)
        {
            CancelExistingToken();
        }

        /// <summary>
        /// Checks if there are pending log events i.e when next token exists but no values are returned
        /// </summary>
        private bool HasPendingLogEvents(PaginatedLogResponse<LogEvent> response)
        {
            return !response.Values.Any() && !string.IsNullOrWhiteSpace(response.NextToken);
        }

        private GetLogEventsRequest CreateGetRequest()
        {
            var request = new GetLogEventsRequest { LogGroup = LogGroup, LogStream = LogStream };

            if (HasInitialized)
            {
                request.NextToken = NextToken;
            }

            if (_isTimeFilterEnabled)
            {
                request.StartTime = StartTime;
                request.EndTime = EndTime;
            }

            return request;
        }

        private FilterLogEventsRequest CreateFilterRequest()
        {
            var request = new FilterLogEventsRequest { LogGroup = LogGroup, LogStream = LogStream };

            if (HasInitialized)
            {
                request.NextToken = NextToken;
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                request.FilterText = FilterText;
            }

            if (_isTimeFilterEnabled)
            {
                request.StartTime = StartTime;
                request.EndTime = EndTime;
            }

            return request;
        }

        protected override int CreateFilterHash()
        {
            if (!IsFiltered())
            {
                return base.CreateFilterHash();
            }

            var filterComponents = new List<string>
            {
                FilterText 
            };

            if (_isTimeFilterEnabled)
            {
                filterComponents.Add(StartTime?.ToString() ?? string.Empty);
                filterComponents.Add(EndTime?.ToString() ?? string.Empty);
            }

            return string.Join("|", filterComponents).GetHashCode();
        }
    }
}
