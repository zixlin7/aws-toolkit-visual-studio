﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Logs.Core;
using Amazon.AWSToolkit.CloudWatch.Logs.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;

using log4net;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.Logs.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log streams
    /// </summary>
    public class LogStreamsViewModel : BaseLogsViewModel
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogStreamsViewModel));
        private bool _isDescending = true;
        private string _logGroup;
        private LogStream _logStream;
        private ICollectionView _logStreamsView;
        private OrderBy _orderBy = OrderBy.LastEventTime;
        private ICommand _viewCommand;
        private ICommand _exportCommand;

        private ObservableCollection<LogStream> _logStreams =
            new ObservableCollection<LogStream>();

        public LogStreamsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext) : base(
            repository, toolkitContext)
        {
        }

        public ObservableCollection<LogStream> LogStreams
        {
            get => _logStreams;
            private set => SetProperty(ref _logStreams, value);
        }

        /// <summary>
        /// Represents the log streams collection view
        /// </summary>
        public ICollectionView LogStreamsView
        {
            get => _logStreamsView;
            set => SetProperty(ref _logStreamsView, value);
        }

        public string LogGroup
        {
            get => _logGroup;
            set => SetProperty(ref _logGroup, value);
        }

        public LogStream LogStream
        {
            get => _logStream;
            set => SetProperty(ref _logStream, value);
        }

        /// <summary>
        /// Indicates the order in which results are sorted
        /// </summary>
        public bool IsDescending
        {
            get => _isDescending;
            set => SetProperty(ref _isDescending, value);
        }

        public OrderBy OrderBy
        {
            get => _orderBy;
            set => SetProperty(ref _orderBy, value);
        }

        public ICommand ViewCommand
        {
            get => _viewCommand;
            set => SetProperty(ref _viewCommand, value);
        }

        public ICommand ExportStreamCommand
        {
            get => _exportCommand;
            set => SetProperty(ref _exportCommand, value);
        }
        public override string GetLogTypeDisplayName() => "log streams";

        public override async Task RefreshAsync()
        {
            ResetState();
            await LoadAsync().ConfigureAwait(false);
        }

        public override CloudWatchResourceType GetCloudWatchResourceType() => CloudWatchResourceType.LogGroup;

        public override async Task LoadAsync()
        {
            await GetLogStreamsAsync(CancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Updates <see cref="OrderBy"/> depending on the value of filter text
        /// </summary>
        /// <returns>true or false depending on if the property was updated</returns>
        public bool UpdateOrderBy()
        {
            var orderBy = string.IsNullOrWhiteSpace(FilterText) ? OrderBy.LastEventTime : OrderBy.LogStreamName;
            if (orderBy != OrderBy)
            {
                OrderBy = orderBy;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates <see cref="IsDescending"/> to have default value of true
        /// </summary>
        /// <returns>true or false depending on if the property was updated</returns>
        public bool UpdateIsDescendingToDefault()
        {
            if (!IsDescending)
            {
                IsDescending = true;
                return true;
            }

            return false;
        }

        private void ResetState()
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogStreams = new ObservableCollection<LogStream>();
                LogStream = null;
                HasInitialized = false;
                ErrorMessage = string.Empty;
            });
        }

        private async Task GetLogStreamsAsync(CancellationToken cancelToken)
        {
            TaskStatus status = TaskStatus.Fail;

            async Task Load()
            {
                status = await GetNextLogStreamsPageAsync(cancelToken);
            }

            void Record(ITelemetryLogger logger)
            {
                RecordFilterMetric(filterByText: true, filterByTime: false, status, logger);
            }

            await ToolkitContext.TelemetryLogger.InvokeAndRecordAsync(Load, Record);
        }

        private async Task<TaskStatus> GetNextLogStreamsPageAsync(CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();

                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return TaskStatus.Success;
                }

                var selectedLogStream = LogStream?.Arn;
                using (CreateLoadingLogsScope())
                {
                    var request = CreateGetRequest();
                    var response = await Repository.GetLogStreamsAsync(request, cancelToken).ConfigureAwait(false);

                    UpdateLogStreamProperties(response, selectedLogStream);
                    return TaskStatus.Success;
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log groups was cancelled", e);
                return TaskStatus.Cancel;
            }
        }

        private void UpdateLogStreamProperties(PaginatedLogResponse<LogStream> response, string previousLogStreamArn)
        {
            var logStreams = LogStreams.ToList().Concat(response.Values).ToList();
            LogStreams = new ObservableCollection<LogStream>(logStreams);

            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = response.NextToken;

                LogStream = LogStreams.FirstOrDefault(x => x.Arn == previousLogStreamArn) ??
                            LogStreams.FirstOrDefault();
                LogStreamsView = CollectionViewSource.GetDefaultView(LogStreams);
                UpdateHasInitialized();
            });
        }

        private GetLogStreamsRequest CreateGetRequest()
        {
            var request = new GetLogStreamsRequest()
            {
                LogGroup = LogGroup, OrderBy = OrderBy, IsDescending = IsDescending
            };
            if (HasInitialized)
            {
                request.NextToken = NextToken;
            }

            if (!string.IsNullOrWhiteSpace(FilterText))
            {
                request.FilterText = FilterText;
                request.OrderBy = OrderBy.LogStreamName;
            }

            return request;
        }
    }
}
