using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Backing view model for viewing log streams
    /// </summary>
    public class LogStreamsViewModel : BaseModel, ILogSearchProperties, IDisposable
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogStreamsViewModel));
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private readonly ToolkitContext _toolkitContext;
        private readonly ICloudWatchLogsRepository _repository;

        private bool _isInitialized = false;
        private bool _isDescending = true;
        private LogGroup _logGroup;
        private LogStream _logStream;
        private string _filterText;
        private string _nextToken;
        private string _errorMessage = string.Empty;
        private ICommand _refreshCommand;
        private ICollectionView _logStreamsView;
        private OrderBy _orderBy = OrderBy.LastEventTime;

        private ObservableCollection<LogStream> _logStreams =
            new ObservableCollection<LogStream>();

        public LogStreamsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
            _repository = repository;
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

        public LogGroup LogGroup
        {
            get => _logGroup;
            set => SetProperty(ref _logGroup, value);
        }

        public LogStream LogStream
        {
            get => _logStream;
            set => SetProperty(ref _logStream, value);
        }

        public string NextToken
        {
            get => _nextToken;
            set => SetProperty(ref _nextToken, value);
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

        public AwsConnectionSettings ConnectionSettings => _repository?.ConnectionSettings;

        private CancellationToken CancellationToken => _tokenSource.Token;

        public async Task RefreshAsync()
        {
            ResetState();
            await LoadAsync().ConfigureAwait(false);
        }

        public async Task LoadAsync()
        {
            await GetLogStreamsAsync(CancellationToken).ConfigureAwait(false);
        }

        public void SetErrorMessage(string errorMessage)
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                ErrorMessage = errorMessage;
            });
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

        public void ResetCancellationToken()
        {
            CancelExistingToken();
            _tokenSource = new CancellationTokenSource();
        }

        private void ResetState()
        {
            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = null;
                LogStreams.Clear();
                LogStream = null;
                _isInitialized = false;
                ErrorMessage = string.Empty;
            });
        }

        private async Task GetLogStreamsAsync(CancellationToken cancelToken)
        {
            try
            {
                cancelToken.ThrowIfCancellationRequested();

                //if no more entries(last page retrieved), do not make additional calls
                if (IsLastPageLoaded())
                {
                    return;
                }

                var selectedLogStream = LogStream?.Arn;

                var request = CreateGetRequest();
                var response = await _repository.GetLogStreamsAsync(request, cancelToken).ConfigureAwait(false);

                UpdateLogStreamProperties(response, selectedLogStream);
            }
            catch (OperationCanceledException e)
            {
                Logger.Error("Operation to load log groups was cancelled", e);
            }
        }

        /// <summary>
        /// Indicates if the last page of log streams has already been loaded/retrieved
        /// </summary>
        private bool IsLastPageLoaded()
        {
            return _isInitialized && string.IsNullOrEmpty(NextToken);
        }

        private void UpdateLogStreamProperties(PaginatedLogResponse<LogStream> response, string previousLogStreamArn)
        {
            var logStreams = LogStreams.ToList().Concat(response.Values).ToList();
            LogStreams = new ObservableCollection<LogStream>(logStreams);

            _toolkitContext.ToolkitHost.ExecuteOnUIThread(() =>
            {
                NextToken = response.NextToken;

                LogStream = LogStreams.FirstOrDefault(x => x.Arn == previousLogStreamArn) ??
                            LogStreams.FirstOrDefault();
                LogStreamsView = CollectionViewSource.GetDefaultView(LogStreams);
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

        private GetLogStreamsRequest CreateGetRequest()
        {
            var request = new GetLogStreamsRequest()
            {
                LogGroup = LogGroup.Name, OrderBy = OrderBy, IsDescending = IsDescending
            };
            if (_isInitialized)
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

        private void CancelExistingToken()
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
        }
    }
}
