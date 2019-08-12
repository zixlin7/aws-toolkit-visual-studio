using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Navigation;

namespace Amazon.AWSToolkit.VisualStudio.ShellOptions
{
    /// <summary>
    /// Interaction logic for GeneralOptionsPageControl.xaml
    /// </summary>
    public partial class GeneralOptionsPageControl
    {
        public GeneralOptionsPageControl()
        {
            InitializeComponent();
        }

        private static string filePrefix = Uri.UriSchemeFile + Uri.SchemeDelimiter;

        public string AnalyticsPermission
        {
            get
            {
                if (permissionCheckBox.IsChecked.GetValueOrDefault())
                    return "true";
                else
                    return "false";
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                    permissionCheckBox.IsChecked = false;
                else if (value.Equals("true"))
                    permissionCheckBox.IsChecked = true;
                else
                    permissionCheckBox.IsChecked = false;
            }
        }

        public string HostedFilesLocation
        {
            get
            {
                if (btnDefault.IsChecked.GetValueOrDefault())
                    return string.Empty;

                if (btnCNNorth1.IsChecked.GetValueOrDefault())
                    return "region://cn-north-1";

                string fileSystemLocation = txtFileSystemLocation.Text ?? string.Empty;
                fileSystemLocation = fileSystemLocation.Trim();
                if (string.IsNullOrEmpty(fileSystemLocation))
                    return string.Empty;

                return filePrefix + fileSystemLocation;
            }
            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    btnDefault.IsChecked = true;
                }
                else if (value.Equals("region://cn-north-1", StringComparison.OrdinalIgnoreCase))
                {
                    btnCNNorth1.IsChecked = true;
                }
                else
                {
                    btnFileSystem.IsChecked = true;
                    txtFileSystemLocation.Text
                        = value.StartsWith(filePrefix, StringComparison.OrdinalIgnoreCase)
                                ? value.Substring(filePrefix.Length)
                                : value;
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new FolderBrowserDialog {ShowNewFolderButton = true};
            if (!string.IsNullOrEmpty(txtFileSystemLocation.Text))
                dlg.SelectedPath = txtFileSystemLocation.Text;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                txtFileSystemLocation.Text = dlg.SelectedPath;
            }
        }

        void AWSPrivacyPolicyLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo((sender as Hyperlink).NavigateUri.ToString()));
            e.Handled = true;
        }

        private void CheckBox_Checked(object sender, EventArgs e)
        {

        }
    }
}
