using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.Util;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Telemetry;

using log4net;

using TaskStatus = Amazon.AWSToolkit.CommonUI.Notifications.TaskStatus;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log groups
    /// </summary>
    public class LogGroupsViewModel : BaseLogsViewModel
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogGroupsViewModel));

        private LogGroup _logGroup;
        private ICommand _viewCommand;
        private ICommand _deleteCommand;

        private ObservableCollection<LogGroup> _logGroups =
            new ObservableCollection<LogGroup>();

        public LogGroupsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext) : base(repository, toolkitContext)
        {
        }

        public ObservableCollection<LogGroup> LogGroups
        {
            get => _logGroups;
            set => SetProperty(ref _logGroups, value);
        }

        public LogGroup LogGroup
        {
            get => _logGroup;
            set => SetProperty(ref _logGroup, value);
        }

        public ICommand ViewCommand
        {
            get => _viewCommand;
            set => SetProperty(ref _viewCommand, value);
        }

        public ICommand DeleteCommand
        {
            get => _deleteCommand;
            set => SetProperty(ref _deleteCommand, value);
        }

        public override string GetLogTypeDisplayName() => "log groups";

        public override async Task RefreshAsync()
        {
            ResetState();
            await LoadAsync().ConfigureAwait(false);
        }

        public override CloudWatchResourceType GetCloudWatchResourceType() => CloudWatchResourceType.LogGroupList;

        public override async Task LoadAsync()
        {
            await GetLogGroupsAsync(CancellationToken).ConfigureAwait(false);
        }

        public async Task<bool> DeleteAsync(LogGroup logGroup)
        {
            try
            {
                return await Repository.DeleteLogGroupAsync(logGroup.Name, CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to delete log group was cancelled", e);
                return false;
            }
        }

        private void ResetState()
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogGroups = new ObservableCollection<LogGroup>();
                LogGroup = null;
                _isInitialized = false;
                ErrorMessage = string.Empty;
            });
        }

        private async Task GetLogGroupsAsync(CancellationToken cancelToken)
        {
            TaskStatus status = TaskStatus.Fail;

            async Task Load()
            {
                status = await GetNextLogGroupsPageAsync(cancelToken);
            }

            void Record(ITelemetryLogger logger)
            {
                RecordFilterMetric(filterByText: true, filterByTime: false, status, logger);
            }

            await ToolkitContext.TelemetryLogger.InvokeAndRecordAsync(Load, Record);
        }

        private async Task<TaskStatus> GetNextLogGroupsPageAsync(CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();

                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return TaskStatus.Success;
                }

                var selectedLogGroup = LogGroup?.Arn;
                using (CreateLoadingLogsScope())
                {
                    var request = CreateGetRequest();
                    var response = await Repository.GetLogGroupsAsync(request, cancelToken).ConfigureAwait(false);

                    UpdateLogGroupProperties(response, selectedLogGroup);
                    return TaskStatus.Success;
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log groups was cancelled", e);
                return TaskStatus.Cancel;
            }
        }

        private void UpdateLogGroupProperties(PaginatedLogResponse<LogGroup> response, string previousLogGroupArn)
        {
            ToolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = response.NextToken;

                var logGroups = LogGroups.ToList();
                logGroups.AddRange(response.Values.ToList());
                LogGroups = new ObservableCollection<LogGroup>(logGroups);
                LogGroup = LogGroups.FirstOrDefault(x => x.Arn == previousLogGroupArn) ?? LogGroups.FirstOrDefault();

                UpdateIsInitialized();
            });
        }

        private GetLogGroupsRequest CreateGetRequest()
        {
            var request = new GetLogGroupsRequest() { FilterText = FilterText };
            if (_isInitialized)
            {
                request.NextToken = NextToken;
            }
            return request;
        }

        public void RecordDeleteMetric(TaskStatus deleteResult)
        {
            ToolkitContext.TelemetryLogger.RecordCloudwatchlogsDelete(new CloudwatchlogsDelete()
            {
                AwsAccount = MetricsMetadata.AccountIdOrDefault(ConnectionSettings.GetAccountId(ToolkitContext.ServiceClientManager)),
                AwsRegion = MetricsMetadata.RegionOrDefault(ConnectionSettings.Region),
                CloudWatchResourceType = CloudWatchResourceType.LogGroup,
                Result = deleteResult.AsMetricsResult(),
            });
        }
    }
}
