using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

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
        public static readonly ILog Logger = LogManager.GetLogger(typeof(LogStreamsViewerControl));

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

        private void ViewModel_OnPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogStreamsViewModel.FilterText))
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
