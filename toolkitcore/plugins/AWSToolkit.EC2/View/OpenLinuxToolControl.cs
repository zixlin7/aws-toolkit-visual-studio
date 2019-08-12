using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Win32;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;


namespace Amazon.AWSToolkit.EC2.View
{
    public class OpenLinuxToolControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(OpenSSHSessionControl));
        OpenLinuxToolController _controller;

        public OpenLinuxToolControl()
        {
        }

        public OpenLinuxToolControl(OpenLinuxToolController controller)
        {
            this._controller = controller;
        }


        public override bool Validated()
        {
            try
            {
                string erg = null;
                if (string.IsNullOrEmpty(this._controller.Model.EnteredUsername))
                {
                    erg = "User name is a required field.";
                }
                else if (string.IsNullOrEmpty(this._controller.Model.ToolLocation))
                {
                    erg = string.Format("Location of {0} is a required field.", getToolName());
                }
                else if (!File.Exists(this._controller.Model.ToolLocation))
                {
                    erg = string.Format("{0} is not found in the entered location.", getToolName());
                }


                if (erg != null)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError(erg);
                    return false;
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError(
                    string.Format("Error {0}: {1}", this.Title.ToLower(), e.Message ));
                return false;
            }

            return true;
        }

        protected void onRequestToNavigate(object sender, RequestNavigateEventArgs evnt)
        {
            try
            {
                Process.Start(new ProcessStartInfo(evnt.Uri.ToString()));
                evnt.Handled = true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error opening link to tool website: " + e.Message);
            }
        }

        protected void onToolBrowse(object sender, RoutedEventArgs evnt)
        {
            try
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.Title = "Select " + this._controller.Executable;
                dlg.CheckPathExists = true;
                dlg.DefaultExt = "exe";
                dlg.Filter = "Application (*.exe)|*.exe";

                if (dlg.ShowDialog().GetValueOrDefault())
                {
                    this._controller.Model.ToolLocation = dlg.FileName;
                }
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error browsing to tool: " + e.Message);
            }
        }

        private string getToolName()
        {
            int pos = this._controller.Executable.LastIndexOf(".");
            return this._controller.Executable.Substring(0, pos);

        }
    }
}
