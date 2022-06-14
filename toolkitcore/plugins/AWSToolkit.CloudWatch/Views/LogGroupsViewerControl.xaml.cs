using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using Amazon.AWSToolkit.CloudWatch.Models;
using Amazon.AWSToolkit.CloudWatch.ViewModels;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Credentials.Core;
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

        public AwsConnectionSettings ConnectionSettings => _viewModel?.ConnectionSettings;

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
                _viewModel.ResetCancellationToken();
                await _viewModel.RefreshAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error refreshing log groups", e);
                _viewModel.SetErrorMessage($"Error refreshing log groups:{Environment.NewLine}{e.Message}");
            }
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

            // if scrollbar is not visible due to less entries in first page and there are more pages left, load more data
            if (scrollViewer.ComputedVerticalScrollBarVisibility != Visibility.Visible && HasMorePages())
            {
                LoadData();
            }
            // load more entries if close to bottom
            else if (IsCloseToBottom(scrollViewer))
            {
                LoadData();
            }
        }

        /// <summary>
        /// Checks if there is no error and there are more entries/pages(next token is present) to be loaded
        /// </summary>
        private bool HasMorePages()
        {
            return !string.IsNullOrWhiteSpace(_viewModel.NextToken) &&
                   string.IsNullOrWhiteSpace(_viewModel.ErrorMessage);
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
                _viewModel.ResetCancellationToken();
                await _viewModel.LoadAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Logger.Error("Error loading log groups", e);
                _viewModel.SetErrorMessage($"Error loading log groups:{Environment.NewLine}{e.Message}");
            }
        }

        private void ListBoxItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var listBoxItem = sender as ListBoxItem;
            if (listBoxItem == null)
            {
                return;
            }

            var selectedLogGroup = listBoxItem.Content as LogGroup;
            if (selectedLogGroup == null)
            {
                return;
            }

            if (_viewModel.ViewCommand.CanExecute(selectedLogGroup))
            {
                _viewModel.ViewCommand.Execute(selectedLogGroup);
            }
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
            DataContextChanged -= OnDataContextChanged;
            // Un-register the existing viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_OnPropertyChanged;
            }
        }
    }
}
