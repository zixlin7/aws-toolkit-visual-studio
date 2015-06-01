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
    /// Interaction logic for AssociateAddressControl.xaml
    /// </summary>
    public partial class AssociateAddressControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AssociateAddressControl));

        AssociateAddressController _controller;

        public AssociateAddressControl(AssociateAddressController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Associate Address";}
        }

        public override bool Validated()
        {
            if (this._controller.Model.Instance == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Instance is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AssociateAddress();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error associating address", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error associating address: " + e.Message);
                return false;
            }
        }
    }
}
