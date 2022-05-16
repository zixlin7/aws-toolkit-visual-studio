using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.CloudWatch.Core;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.CloudWatch.ViewModels
{
    /// <summary>
    /// Base view model for viewing log resources such as groups, streams etc
    /// </summary>
    public abstract class BaseLogsViewModel : BaseModel, ILogSearchProperties, IDisposable
    {
        protected readonly ToolkitContext ToolkitContext;
        protected readonly ICloudWatchLogsRepository Repository;

        protected bool _loadingLogs = false;
        protected bool _isInitialized = false;

        private CancellationTokenSource _tokenSource = new CancellationTokenSource();
        private string _filterText;
        private string _nextToken;
        private string _errorMessage = string.Empty;
        private ICommand _refreshCommand;
        private ICommand _copyArnCommand;

        protected BaseLogsViewModel(ICloudWatchLogsRepository repository, ToolkitContext toolkitContext)
        {
            ToolkitContext = toolkitContext;
            Repository = repository;
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

        public ICommand CopyArnCommand => _copyArnCommand ?? (_copyArnCommand = CreateCopyArnCommand());

        private ICommand CreateCopyArnCommand()
        {
            return Commands.CopyArnCommand.Create(GetLogTypeDisplayName(), ToolkitContext.ToolkitHost);
        }

        public AwsConnectionSettings ConnectionSettings => Repository?.ConnectionSettings;

        protected CancellationToken CancellationToken => _tokenSource.Token;

        public virtual string GetLogTypeDisplayName() => "log resources";

        public abstract Task RefreshAsync();

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

        private void SetLoadingLogs(bool value)
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
        protected bool IsLastPageLoaded()
        {
            return _isInitialized && string.IsNullOrEmpty(NextToken);
        }

        protected void UpdateIsInitialized()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
            }
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
