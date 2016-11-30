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
    /// Interaction logic for AllocateAddressControl.xaml
    /// </summary>
    public partial class AllocateAddressControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AllocateAddressControl));

        AllocateAddressController _controller;

        public AllocateAddressControl(AllocateAddressController controller)
        {
            InitializeComponent();
            this._controller = controller;
        }

        public override string Title
        {
            get { return "Allocate New Address";}
        }

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._ctlType.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("You must specify how the address will be used.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                string domain = AddressWrapper.DOMAIN_EC2;
                if (string.Equals("VPC", this._ctlType.Text, StringComparison.InvariantCultureIgnoreCase))
                    domain = AddressWrapper.DOMAIN_VPC;

                this._controller.AllocateAddress(domain);
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error allocating new address", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error allocating new address: " + e.Message);
                return false;
            }
        }
    }
}
