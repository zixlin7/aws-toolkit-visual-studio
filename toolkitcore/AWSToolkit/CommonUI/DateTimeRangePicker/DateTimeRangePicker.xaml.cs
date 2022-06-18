using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.DateTimeRangePicker
{
    /// <summary>
    /// Represents a Date Time Range Picker
    /// </summary>
    public partial class DateTimeRangePicker : UserControl
    {
        private DateTimeRangePickerViewModel _viewModel;
        public DateTimeRangePicker()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            SetViewModel((DateTimeRangePickerViewModel) e.NewValue);
        }

        private void SetViewModel(DateTimeRangePickerViewModel viewModel)
        {
            if (_viewModel != null)
            {
                _viewModel.StartTimeModel.PropertyChanged -= TimeModel_PropertyChanged;
                _viewModel.EndTimeModel.PropertyChanged -= TimeModel_PropertyChanged;
            }

            _viewModel = viewModel;

            // Register and setup the viewmodel/state
            if (_viewModel != null)
            {
                _viewModel.StartTimeModel.PropertyChanged += TimeModel_PropertyChanged;
                _viewModel.EndTimeModel.PropertyChanged += TimeModel_PropertyChanged;
            }
        }

        private void TimeModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(TimeInputViewModel.Time))
            {
                _viewModel.RefreshRange();
            }
        }
    }
}
