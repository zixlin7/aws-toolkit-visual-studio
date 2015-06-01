using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.CloudFormation.Model;

namespace Amazon.AWSToolkit.CloudFormation.View.Components
{
    /// <summary>
    /// Interaction logic for TemplateOuputControl.xaml
    /// </summary>
    public partial class TemplateOuputControl
    {
        public TemplateOuputControl(Output output)
        {
            InitializeComponent();
            this.DataContext = output;

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
