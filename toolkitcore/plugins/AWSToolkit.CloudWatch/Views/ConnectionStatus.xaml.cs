using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.CloudWatch.Views
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
    }
}
