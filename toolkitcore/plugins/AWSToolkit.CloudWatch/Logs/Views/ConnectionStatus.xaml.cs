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

        public static readonly DependencyProperty ErrorMessageProperty =
    DependencyProperty.Register(
        nameof(ErrorMessage), typeof(string), typeof(ConnectionStatus),
        new PropertyMetadata(""));

        public string ErrorMessage
        {
            get => (string) GetValue(ErrorMessageProperty);
            set => SetValue(ErrorMessageProperty, value);
        }

        public static readonly DependencyProperty ShowErrorCommandProperty =
            DependencyProperty.Register(
                nameof(ShowErrorCommand), typeof(ICommand), typeof(ConnectionStatus));

        public ICommand ShowErrorCommand
        {
            get => (ICommand) GetValue(ShowErrorCommandProperty);
            set => SetValue(ShowErrorCommandProperty, value);
        }
    }
}
