using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Logs.Views
{
    /// <summary>
    /// Interaction logic for ConnectionStatus.xaml
    /// </summary>
    public partial class ConnectionStatus : UserControl
    {
        public ConnectionStatus()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty ConnectionSettingsProperty =
            DependencyProperty.Register(
                nameof(ConnectionSettings), typeof(AwsConnectionSettings), typeof(ConnectionStatus),
                new PropertyMetadata(null));

        public AwsConnectionSettings ConnectionSettings
        {
            get => (AwsConnectionSettings) GetValue(ConnectionSettingsProperty);
            set => SetValue(ConnectionSettingsProperty, value);
        }

        public static readonly DependencyProperty FeedbackSourceProperty =
            DependencyProperty.Register(
                nameof(FeedbackSource), typeof(string), typeof(ConnectionStatus),
                new PropertyMetadata(""));

        public string FeedbackSource
        {
            get => (string) GetValue(FeedbackSourceProperty);
            set => SetValue(FeedbackSourceProperty, value);
        }

        public static readonly DependencyProperty FeedbackCommandProperty =
            DependencyProperty.Register(
                nameof(FeedbackCommand), typeof(ICommand), typeof(ConnectionStatus));

        public ICommand FeedbackCommand
        {
            get => (ICommand) GetValue(FeedbackCommandProperty);
            set => SetValue(FeedbackCommandProperty, value);
        }
    }
}
