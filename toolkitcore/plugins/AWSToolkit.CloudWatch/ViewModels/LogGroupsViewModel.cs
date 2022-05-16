﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Shared;

using log4net;

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
                LogGroups.Clear();
                LogGroup = null;
                _isInitialized = false;
                ErrorMessage = string.Empty;
            });
        }

        private async Task GetLogGroupsAsync(CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();
                
                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return;
                }

                var selectedLogGroup = LogGroup?.Arn;
                using (CreateLoadingLogsScope())
                {
                    var request = CreateGetRequest();
                    var response = await Repository.GetLogGroupsAsync(request, cancelToken).ConfigureAwait(false);

                    UpdateLogGroupProperties(response, selectedLogGroup);
                }
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log groups was cancelled", e);
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
    }
}
