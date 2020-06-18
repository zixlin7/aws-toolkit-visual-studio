using System.Diagnostics;
using System.Windows.Documents;
using System.Windows.Navigation;
using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.VisualStudio.Telemetry
{
    /// <summary>
    /// Interaction logic for TelemetryInformationDialog.xaml
    /// </summary>
    public partial class TelemetryInformationDialog : DialogWindow
    {
        public TelemetryInformationDialog()
        {
            InitializeComponent();
        }

        void AWSPrivacyPolicyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (sender is Hyperlink link)
            {
                Process.Start(new ProcessStartInfo(link.NavigateUri.ToString()));
                e.Handled = true;
            }
        }
    }
}
