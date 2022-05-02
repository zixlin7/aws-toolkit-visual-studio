using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Tasks;

using log4net;

namespace Amazon.AWSToolkit.CloudWatch.Views
{
    /// <summary>
    /// Interaction logic for LogStreamsViewerControl.xaml
    /// </summary>
    public partial class LogStreamsViewerControl :  BaseAWSControl, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogStreamsViewerControl));
        public static readonly string StreamNameHeader = "Log Stream";
        public static readonly string EventTimeHeader = "Last Event Time";
        public static readonly string TimeZone = TimeZoneInfo.Local.Id;

        private readonly Dictionary<string, OrderBy> _orderByColumnMap = new Dictionary<string, OrderBy>
        {
            { StreamNameHeader, OrderBy.LogStreamName }, { EventTimeHeader, OrderBy.LastEventTime }
        };

        private LogStreamsViewModel _viewModel;
      
        public LogStreamsViewerControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// Hold onto the viewmodel whenever one is assigned as the DataContext
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel(e.NewValue as LogStreamsViewModel);
        }

        private void SetViewModel(LogStreamsViewModel viewModel)
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

        public override string Title => $"Group: {_viewModel?.LogGroup.Name}";

        public override string UniqueId => $"Logs:Group:{_viewModel?.LogGroup.Name}";

        private void ViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogStreamsViewModel.FilterText))
            {
                // when filter text changes, update order by property considering the API limitation and refresh
                // (https://docs.aws.amazon.com/AmazonCloudWatchLogs/latest/APIReference/API_DescribeLogStreams.html#API_DescribeLogStreams_RequestSyntax)
                var isUpdated = _viewModel.UpdateOrderBy();
                if (!isUpdated)
                {
                    Refresh();
                }
            }

            if (e.PropertyName == nameof(LogStreamsViewModel.OrderBy))
            {
                // when order by property changes, ensure sorting direction is set to default value and refresh
                var isUpdated = _viewModel.UpdateIsDescendingToDefault();
                if (!isUpdated)
                {
                    Refresh();
                }
            }

            if (e.PropertyName == nameof(LogStreamsViewModel.IsDescending))
            {
                // when sort direction changes, refresh and retrieve streams sorted in that order (i.e. ascending or descending)
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
                _viewModel.ResetCancellationToken();
                await _viewModel.RefreshAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing log streams", e);
                _viewModel.SetErrorMessage($"Error refreshing log streams:{Environment.NewLine}{e.Message}");
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

            // load more when close to bottom
            if (IsCloseToBottom(scrollViewer))
            {
                LoadData();
            }
        }

        private bool IsCloseToBottom(ScrollViewer scrollViewer)
        {
            var isClose = (scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset) <= (0.05 * scrollViewer.ScrollableHeight);
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
                Logger.Error("Error loading log streams", e);
                _viewModel.SetErrorMessage($"Error loading log streams:{Environment.NewLine}{e.Message}");
            }
        }

        private void LogStreamsList_OnSorting(object sender, DataGridSortingEventArgs e)
        {
            e.Handled = true;
            var column = e.Column;
            var isDescending = (column.SortDirection == ListSortDirection.Ascending);

            var columnName = e.Column.Header.ToString();

            if (string.IsNullOrWhiteSpace(_viewModel.FilterText))
            {
                HandleSortingWithoutFilter(columnName, isDescending);
            }
            else
            {
                HandleSortingWithFilter(columnName, isDescending);
            }
        }

        private void HandleSortingWithoutFilter(string columnName, bool isDescending)
        {
            if (_viewModel.OrderBy != _orderByColumnMap[columnName])
            {
                _viewModel.OrderBy = _orderByColumnMap[columnName];
            }
            else
            {
                _viewModel.IsDescending = isDescending;
            }
        }

        private void HandleSortingWithFilter(string columnName, bool isDescending)
        {
            // due to API limitation, with a filter value, only ordering by log stream name is allowed
            // sorting by last event time is ignored
            if (columnName.Equals(EventTimeHeader))
            {
                // clicking on headers clears out previous sort indicator
                // re-apply sort indicator
                ApplySortIndicator();
                return;
            }

            _viewModel.IsDescending = isDescending;
        }

        private void LogStreamsList_OnTargetUpdated(object sender, DataTransferEventArgs e)
        {
            // when DataGrid item collection changes/updates, previous sort settings(including sort icon/indicator) are cleared out
            // re-apply sort settings
            ApplySortIndicator();
        }

        private void ApplySortIndicator()
        {
            var sortDirection = _viewModel.IsDescending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            var column = _viewModel.OrderBy == OrderBy.LastEventTime
                ? EventTimeColumn
                : StreamNameColumn;
            column.SortDirection = sortDirection;
        }

        private void Row_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            var dataGridRow = sender as DataGridRow;
            if (dataGridRow == null)
            {
                return;
            }

            var selectedLogStream = dataGridRow.DataContext as LogStream;
            if (selectedLogStream == null)
            {
                return;
            }

            var parameters = new object[] {_viewModel.LogGroup.Name, selectedLogStream.Name };
            if (_viewModel.ViewCommand.CanExecute(parameters))
            {
                _viewModel.ViewCommand.Execute(parameters);
            }
        }

        public void Dispose()
        {
            DataContextChanged -= OnDataContextChanged;
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
                _viewModel.Dispose();
            }
        }
    }
}
