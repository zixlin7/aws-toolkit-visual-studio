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
    /// Interaction logic for CreateRouteTableControl.xaml
    /// </summary>
    public partial class CreateRouteTableControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateRouteTableControl));

        CreateRouteTableController _controller;

        public CreateRouteTableControl(CreateRouteTableController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Create Route Table";}
        }

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
                this._controller.CreateRouteTable();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating route table", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating route table: " + e.Message);
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
