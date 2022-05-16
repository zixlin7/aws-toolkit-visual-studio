using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CloudWatch.Views
{
    /// <summary>
    /// Indicates status of results eg. error, no results
    /// </summary>
    public partial class ResultsStatus : UserControl
    {
        public ResultsStatus()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ErrorMessageProperty =
            DependencyProperty.Register(
                nameof(ErrorMessage), typeof(string), typeof(ResultsStatus),
                new PropertyMetadata(null));


        public static readonly DependencyProperty LoadingLogsProperty =
            DependencyProperty.Register(
                nameof(LoadingLogs), typeof(bool), typeof(ResultsStatus),
                new PropertyMetadata(null));


        public string ErrorMessage
        {
            get => (string) GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public bool LoadingLogs
        {
            get => (bool) GetValue(LoadingLogsProperty);
            set => SetValue(LoadingLogsProperty, value);
        }
    }
}
