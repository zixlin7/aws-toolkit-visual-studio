using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AttachToVPCControl.xaml
    /// </summary>
    public partial class AttachToVPCControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AttachVolumeControl));

        AttachToVPCController _controller;

        public AttachToVPCControl(AttachToVPCController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Attach to VPC";

        public override bool Validated()
        {
            if (this._controller.Model.VPC == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("VPC is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AttachToVPC();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error attaching to vpc", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error attaching to vpc: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            if (this._ctlVPC.Items.Count == 0)
                return;

            this._ctlVPC.SelectedIndex = 0;
        }
    }
}
