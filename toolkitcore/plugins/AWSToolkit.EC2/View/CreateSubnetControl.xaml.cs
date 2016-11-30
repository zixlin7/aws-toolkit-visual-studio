using System;
using System.Collections.Generic;
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

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.Util;

using Amazon.EC2;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateSubnetControl.xaml
    /// </summary>
    public partial class CreateSubnetControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSubnetControl));

        CreateSubnetController _controller;

        public CreateSubnetControl(CreateSubnetController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Create Subnet";}
        }

        public override bool Validated()
        {
            if (this._controller.Model.VPC == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("VPC is a required field.");
                return false;
            }

            if (string.IsNullOrEmpty(this._controller.Model.AvailabilityZone))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Availability Zone is a required field.");
                return false;
            }
            
            if (string.IsNullOrEmpty(this._controller.Model.CIDRBlock))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("CIDR Block is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateSubnet();
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
            if (this._ctlVPC.Items.Count != 0)
                this._ctlVPC.SelectedIndex = 0;

            if (this._ctlZones.Items.Count != 0)
                this._ctlZones.SelectedIndex = 0;            
        }
    }
}
