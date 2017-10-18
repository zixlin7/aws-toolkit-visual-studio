using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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


using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;
namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for DeleteVPCControl.xaml
    /// </summary>
    public partial class DeleteVPCControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(DeleteVPCControl));

        DeleteVPCController _controller;

        public DeleteVPCControl(DeleteVPCController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title
        {
            get { return "Delete VPC";}
        }


        public override bool OnCommit()
        {
            try
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host != null)
                    host.IsOkEnabled = false;
                this._controller.DeleteVPC();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting vpc", e);
                AppendOutputMessage("Error deleting vpc", e.Message);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting vpc: " + e.Message);
            }

            return false;
        }

        public void DeleteAsyncComplete(bool success)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.Invoke((Action)(() =>
            {
                var host = FindHost<OkCancelDialogHost>();
                if (host == null)
                    return;

                if (!success)
                    host.IsOkEnabled = true;
                else
                    host.Close(true);
            }));
        }

        public void AppendOutputMessage(string message, params object[] args)
        {
            ToolkitFactory.Instance.ShellProvider.ShellDispatcher.BeginInvoke((Action)(() =>
            {
                string line = string.Format(message, args);
                var body = this._ctlOutputLog.Text;
                if (!string.IsNullOrEmpty(body))
                    body += "\r\n";

                body += line;
                this._ctlOutputLog.Text = body;
                this._ctlOutputLog.ScrollToEnd();
            }));
        }

    }
}
