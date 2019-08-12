using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for TemplateOuputControl.xaml
    /// </summary>
    public partial class TemplateOutputControl
    {
        public TemplateOutputControl(Output output)
        {
            InitializeComponent();
            this.DataContext = output;

            if (!string.IsNullOrEmpty(output.OutputValue))
            {
                if (output.OutputValue.ToLower().StartsWith("http"))
                {
                    this._ctlValue.Visibility = Visibility.Hidden;
                    this._ctlLinkInner.Text = output.OutputValue;
                }
                else
                {
                    this._ctlLinkOuter.Visibility = Visibility.Hidden;
                    this._ctlValue.Text = output.OutputValue;
                }
            }
            else
            {
                this._ctlLinkOuter.Visibility = Visibility.Hidden;
            }
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo(this._ctlLinkInner.Text));
                e.Handled = true;
            }
            catch (Exception ex)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error navigating to object: " + ex.Message);
            }
        }
    }
}
