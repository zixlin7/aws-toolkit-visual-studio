using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log groups
    /// </summary>
    public class LogGroupsViewModel : BaseModel, ILogSearchProperties
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogGroupsViewModel));

        private readonly ToolkitContext _toolkitContext;
        private readonly ICloudWatchLogsRepository _repository;

        private bool _isInitialized = false;

        private LogGroup _logGroup;
        private string _filterText;
        private string _nextToken;

        private ObservableCollection<LogGroup> _logGroups =
            new ObservableCollection<LogGroup>();

        public LogGroupsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _repository = repository;
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

        public string NextToken
        {
            get => _nextToken;
            set => _nextToken = value;
        }

        public string FilterText
        {
            get => _filterText;
            set => SetProperty(ref _filterText, value);
        }

        public ICredentialIdentifier CredentialIdentifier => _repository?.CredentialIdentifier;

        public ToolkitRegion Region => _repository?.Region;

        public string CredentialDisplayName => CredentialIdentifier?.DisplayName;

        public string RegionDisplayName => Region?.DisplayName;

        public ToolkitContext ToolkitContext => _toolkitContext;

        public async Task RefreshAsync(CancellationToken cancelToken)
        {
            ResetState();
            await LoadAsync(cancelToken).ConfigureAwait(false);
        }

        public async Task LoadAsync(CancellationToken cancelToken)
        {
            await GetLogGroupsAsync(cancelToken).ConfigureAwait(false);
        }

        private void ResetState()
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogGroups.Clear();
                LogGroup = null;
                _isInitialized = false;
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

                var request = CreateGetRequest();
                var response = await _repository.GetLogGroupsAsync(request, cancelToken).ConfigureAwait(false);

                UpdateLogGroupProperties(response, selectedLogGroup);

            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log groups was cancelled", e);
            }
        }

        /// <summary>
        /// Indicates if the last page of log groups has already been loaded/retrieved
        /// </summary>
        private bool IsLastPageLoaded()
        {
            return _isInitialized && string.IsNullOrEmpty(NextToken);
        }

        private void UpdateLogGroupProperties(PaginatedLogResponse<LogGroup> response, string previousLogGroupArn)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = response.NextToken;

                var logGroups = LogGroups.ToList();
                logGroups.AddRange(response.Values.ToList());
                LogGroups = new ObservableCollection<LogGroup>(logGroups);
                LogGroup = LogGroups.FirstOrDefault(x => x.Arn == previousLogGroupArn) ?? LogGroups.FirstOrDefault();

                UpdateIsInitialized();
            });
        }

        private void UpdateIsInitialized()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
            }
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
