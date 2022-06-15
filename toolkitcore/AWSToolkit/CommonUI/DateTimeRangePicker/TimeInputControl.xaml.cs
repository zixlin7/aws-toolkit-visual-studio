using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            // update DateTime property on textbox lost focus
            UpdateTime();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                // update DateTime property when enter key is pressed in textbox
                UpdateTime();
            }
        }

        /// <summary>
        /// Updates time if the input is valid
        /// </summary>
        private void UpdateTime()
        {
            if (string.IsNullOrEmpty(_viewModel.Error))
            {
                _viewModel.SetTime();
            }
        }
    }
}
