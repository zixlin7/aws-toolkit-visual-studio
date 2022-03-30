using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Tasks;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Views
{
    /// <summary>
    /// Represents the control that displays log groups for selected credential settings
    /// </summary>
    public partial class LogGroupsViewerControl : BaseAWSControl, IDisposable
    {
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogGroupsViewerControl));

        private LogGroupsViewModel _viewModel;
        private CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public LogGroupsViewerControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Hold onto the viewmodel whenever one is assigned as the DataContext
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel(e.NewValue as LogGroupsViewModel);
        }

        private void SetViewModel(LogGroupsViewModel viewModel)
        {
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
            }

            _viewModel = viewModel;

            // Register and setup the viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
                LoadData();
            }
        }

        public ICredentialIdentifier CredentialIdentifier => _viewModel?.CredentialIdentifier;

        public ToolkitRegion Region => _viewModel?.Region;


        private void ViewModel_OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogGroupsViewModel.FilterText))
            {
                Refresh();
            }
        }

        private void Refresh()
        {
            Task.Run(async () =>
            {
                await RefreshAsync().ConfigureAwait(false);
            }).LogExceptionAndForget();
        }

        private async Task RefreshAsync()
        {
            try
            {
                ResetCancellationToken();
                await _viewModel.RefreshAsync(_tokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing log groups", e);
                _viewModel.SetErrorMessage($"Error refreshing log groups:{Environment.NewLine}{e.Message}");
            }
        }

        private void ResetCancellationToken()
        {
            CancelExistingToken();
            _tokenSource = new CancellationTokenSource();
        }


        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Get the Border of the ListBox (first child of a ListBox)
            var listBox = sender as ListBox;
            if (listBox == null) return;

            Decorator border = VisualTreeHelper.GetChild(listBox, 0) as Decorator;
            if (border == null) return;

            // Get ScrollViewer
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (scrollViewer == null) return;

            //load more entries if close to bottom
            if (IsCloseToBottom(scrollViewer))
            {
                LoadData();
            }
        }

        private bool IsCloseToBottom(ScrollViewer scrollViewer)
        {
            var isClose =  (scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset) <= (0.05 * scrollViewer.ScrollableHeight);
            return scrollViewer.ScrollableHeight != 0 && scrollViewer.VerticalOffset != 0 && isClose;
        }

        private void LoadData()
        {
            Task.Run(async () =>
            {
                await LoadAsync().ConfigureAwait(false);
            }).LogExceptionAndForget();
        }

        private async Task LoadAsync()
        {
            try
            {
                ResetCancellationToken();
                await _viewModel.LoadAsync(_tokenSource.Token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error loading log groups", e);
                _viewModel.SetErrorMessage($"Error loading log groups:{Environment.NewLine}{e.Message}");
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
            DataContextChanged -= OnDataContextChanged;
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
            }
        }
    }
}
