using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Tasks;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Views
{
    /// <summary>
    /// Interaction logic for LogEventsViewerControl.xaml
    /// </summary>
    public partial class LogEventsViewerControl : BaseAWSControl, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogEventsViewerControl));
        public static readonly string TimeZone = TimeZoneInfo.Local.Id;
        public static readonly string DefaultHintText = "Filter events e.g. Error, \"BUILD FAILED\", \"$event\" ";
        private const double DebounceInterval = 500;
        private readonly DebounceDispatcher _scrollViewChangedDispatcher = new DebounceDispatcher();
        private readonly DebounceDispatcher _refreshDispatcher = new DebounceDispatcher();

        private LogEventsViewModel _viewModel;

        public LogEventsViewerControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Hold onto the viewmodel whenever one is assigned as the DataContext
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel((LogEventsViewModel)e.NewValue);
        }

        private void SetViewModel(LogEventsViewModel viewModel)
        {
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
                _viewModel.DateTimeRange.RangeChanged -= DateTimeRange_RangeChanged;
            }

            _viewModel = viewModel;

            // Register and setup the viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
                _viewModel.DateTimeRange.RangeChanged += DateTimeRange_RangeChanged;
                DebounceAndLoadData();
            }
        }

        private void DateTimeRange_RangeChanged(object sender, EventArgs e)
        {
            DebounceAndRefreshData();
        }

        public override string Title => $"Stream: {_viewModel?.LogStream}";

        public override string UniqueId => $"Logs:Stream:{_viewModel?.LogStream}";

        private void ViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogEventsViewModel.FilterText))
            {
                DebounceAndRefreshData();
            }

            if (e.PropertyName == nameof(LogEventsViewModel.IsTimeFilterEnabled))
            {
                //if there is an active filter selection, refresh when filter button is toggled on/off
                if (_viewModel.StartTime != null || _viewModel.EndTime != null)
                {
                    DebounceAndRefreshData();
                }
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
                _viewModel.ResetCancellationToken();
                await _viewModel.RefreshAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing log events", e);
                _viewModel.SetErrorMessage($"Error refreshing log events:{Environment.NewLine}{e.Message}");
            }
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Get the Border of the DataGrid (first child of a DataGrid)
            var dataGrid = sender as DataGrid;
            if (dataGrid == null)
            {
                return;
            }

            Decorator border = VisualTreeHelper.GetChild(dataGrid, 0) as Decorator;
            if (border == null)
            {
                return;
            }

            // Get ScrollViewer
            ScrollViewer scrollViewer = border.Child as ScrollViewer;
            if (scrollViewer == null)
            {
                return;
            }

            // if scrollbar is not visible due to less entries in first page and there are more pages left, load more data
            if (scrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible && HasMorePages())
            {
                DebounceAndLoadData();
            }

            // load more entries if close to bottom
            else if(IsCloseToBottom(scrollViewer))
            {
                DebounceAndLoadData();
            }
        }

        private void DebounceAndLoadData()
        {
            _scrollViewChangedDispatcher.Debounce(DebounceInterval, _ => LoadData());
        }

        private void DebounceAndRefreshData()
        {
            _refreshDispatcher.Debounce(DebounceInterval, _ => Refresh());
        }

        /// <summary>
        /// Checks if there is no error and there are more entries/pages(next token is present) to be loaded
        /// </summary>
        private bool HasMorePages()
        {
            return _viewModel.HasMorePages() &&
                   string.IsNullOrWhiteSpace(_viewModel.ErrorMessage);
        }

        private bool IsCloseToBottom(ScrollViewer scrollViewer)
        {
            // evaluates to true when scroll thumb is 95% or more towards the bottom
            var isClose = (scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset) <=
                          (0.05 * scrollViewer.ScrollableHeight);
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
                _viewModel.ResetCancellationToken();
                await _viewModel.LoadAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error loading log events", e);
                _viewModel.SetErrorMessage($"Error loading log events:{Environment.NewLine}{e.Message}");
            }
        }

        public void Dispose()
        {
            DataContextChanged -= OnDataContextChanged;
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
                _viewModel.DateTimeRange.RangeChanged -= DateTimeRange_RangeChanged;
                _viewModel.Dispose();
            }
        }
    }
}
