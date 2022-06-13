using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.DateTimeRangePicker
{
    /// <summary>
    /// Represents a time input control
    /// </summary>
    public partial class TimeInputControl : UserControl
    {
        private TimeInputViewModel _viewModel;

        public TimeInputControl()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
           _viewModel = e.NewValue as TimeInputViewModel;
        }

        private void OnLostFocus(object sender, RoutedEventArgs e)
        {
            // update DateTime property on textbox lost focus if input is valid
            if (string.IsNullOrEmpty(_viewModel.Error))
            {
                _viewModel.SetTime();
            }
        }
    }
}
