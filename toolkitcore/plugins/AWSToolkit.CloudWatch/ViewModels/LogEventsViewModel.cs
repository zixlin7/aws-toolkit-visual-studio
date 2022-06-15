using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CommonUI.DateTimeRangePicker;
using Amazon.AWSToolkit.Context;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log events/messages
    /// </summary>
    public class LogEventsViewModel : BaseLogsViewModel
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogEventsViewModel));
        private string _logGroup;
        private string _logStream;
        private LogEvent _logEvent;
        private ICollectionView _logEventsView;
        private bool _isWrapped = false;
        private readonly DateTimeRangePickerViewModel _dateTimeRange = new DateTimeRangePickerViewModel(null , null);
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

        public DateTime? StartTime => _dateTimeRange.GetFullStartTime();

        public DateTime? EndTime => _dateTimeRange.GetFullEndTime();

        public override string GetLogTypeDisplayName() => "log events";

        public override async Task RefreshAsync()
        {
            ResetState();
            await LoadAsync().ConfigureAwait(false);
        }

        public override CloudWatchResourceType GetCloudWatchResourceType() => CloudWatchResourceType.LogStream;

        public override async Task LoadAsync()
        {
            await GetLogEventsAsync(CancellationToken).ConfigureAwait(false);
        }

        private void ResetState()
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogEvents = new ObservableCollection<LogEvent>();
                LogEvent = null;
                _isInitialized = false;
                ErrorMessage = string.Empty;
            });
        }

        private async Task GetLogEventsAsync(CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();

                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return;
                }

                var selectedLogEvent = LogEvent?.Message;

                using (CreateLoadingLogsScope())
                {
                    var request = CreateGetRequest();
                    var response = await Repository.GetLogEventsAsync(request, cancelToken).ConfigureAwait(false);

                    UpdateLogEventProperties(response, selectedLogEvent);
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log events was cancelled", e);
            }
        }

        private void UpdateLogEventProperties(PaginatedLogResponse<LogEvent> response, string previousLogMessage)
        {
            var logEvents = LogEvents.ToList().Concat(response.Values).ToList();
            LogEvents = new ObservableCollection<LogEvent>(logEvents);

            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = response.NextToken;

                LogEvent = LogEvents.FirstOrDefault(x => x.Message == previousLogMessage) ??
                           LogEvents.FirstOrDefault();
                LogEventsView = CollectionViewSource.GetDefaultView(LogEvents);
                UpdateIsInitialized();
            });
        }

        private GetLogEventsRequest CreateGetRequest()
        {
            var request = new GetLogEventsRequest
            {
                LogGroup = LogGroup, LogStream = LogStream, StartTime = StartTime, EndTime = EndTime
            };
            if (_isInitialized)
            {
                request.NextToken = NextToken;
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                request.FilterText = FilterText;
            }

            return request;
        }
    }
}
