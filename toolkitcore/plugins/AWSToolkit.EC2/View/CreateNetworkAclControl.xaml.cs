using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateNetworkAclControl.xaml
    /// </summary>
    public partial class CreateNetworkAclControl : BaseAWSControl
    {
       static ILog LOGGER = LogManager.GetLogger(typeof(CreateNetworkAclControl));

        CreateNetworkAclController _controller;

        public CreateNetworkAclControl(CreateNetworkAclController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Create Network ACL";

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
                this._controller.CreateNetworkAcl();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating network acl", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating network acl: " + e.Message);
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
