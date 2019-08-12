using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for UpdateDNSSettingsControl.xaml
    /// </summary>
    public partial class UpdateDNSSettingsControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CreateVPCControl));

        readonly UpdateDNSSettingsController _controller;

        public UpdateDNSSettingsControl(UpdateDNSSettingsController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Update DNS Settings";

        public override bool Validated()
        {
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                return this._controller.UpdateVPCAttributes();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating vpc", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating VPC: " + e.Message);
            }

            return false;
        }
    }
}
