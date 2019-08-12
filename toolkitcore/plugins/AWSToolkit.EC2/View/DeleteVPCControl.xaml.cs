using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
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

        public override string Title => "Delete VPC";


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
            ToolkitFactory.Instance.ShellProvider.ExecuteOnUIThread((Action)(() =>
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
            ToolkitFactory.Instance.ShellProvider.BeginExecuteOnUIThread((Action)(() =>
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
