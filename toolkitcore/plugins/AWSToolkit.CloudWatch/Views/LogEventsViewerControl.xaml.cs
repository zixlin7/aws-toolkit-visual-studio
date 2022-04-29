﻿using System;
using System.ComponentModel;
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
    /// Interaction logic for LogEventsViewerControl.xaml
    /// </summary>
    public partial class LogEventsViewerControl : BaseAWSControl, IDisposable
    {
        private static readonly ILog Logger = LogManager.GetLogger(typeof(LogEventsViewerControl));
        public static readonly string TimeZone = TimeZoneInfo.Local.Id;

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
            }

            _viewModel = viewModel;

            // Register and setup the viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged += ViewModel_OnPropertyChanged;
                LoadData();
            }
        }

        public override string Title => $"Stream: {_viewModel?.LogStream.Name}";

        public override string UniqueId => $"Logs:Stream:{_viewModel?.LogStream.Name}";

        private void ViewModel_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LogEventsViewModel.FilterText))
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

            // load more when close to bottom
            if (IsCloseToBottom(scrollViewer))
            {
                LoadData();
            }
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
                _viewModel.Dispose();
            }
        }
    }
}
