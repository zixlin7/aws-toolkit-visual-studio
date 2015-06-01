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
    /// Interaction logic for CreateSecurityGroupControl.xaml
    /// </summary>
    public partial class CreateSecurityGroupControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        CreateSecurityGroupController _controller;

        public CreateSecurityGroupControl(CreateSecurityGroupController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get { return "Create Security Group";}
        }

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Name))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is a required field.");
                return false;
            }
            if (string.IsNullOrEmpty(this._controller.Model.Description))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Description is a required field.");
                return false;
            }
            // expect this to fire in vpc only environments where the user has killed their default vpc,
            // so we start up with no prior selection
            if (this._controller.Model.IsVpcOnlyEnvironment && this._controller.Model.SelectedVPC == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("A VPC is required for the current region.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateSecurityGroup();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating security group", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating security group: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            if (this._controller.Model.IsVpcOnlyEnvironment)
            {
                // try and find the default vpc and preselect if available
                foreach (var v in this._controller.Model.AvailableVPCs)
                {
                    if (v.NativeVPC.IsDefault)
                    {
                        this._ctlVPC.SelectedItem = v;
                        break;
                    }
                }
            }
            else
                this._ctlVPC.SelectedItem = this._controller.Model.AvailableVPCs.FirstOrDefault(); // selects 'not in vpc'

            this._ctlName.Focus();
        }
    }
}
