using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AssociateSubnetControl.xaml
    /// </summary>
    public partial class AssociateSubnetControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AssociateSubnetControl));

        IAssociateSubnetController _controller;

        public AssociateSubnetControl(IAssociateSubnetController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Associate Subnet";

        public override bool Validated()
        {
            if (this._controller.Model.SelectedSubnet == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Subnet is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AssociateSubnet();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associating subnet", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error associating subnet: " + e.Message);
                return false;
            }
        }
    }
}
