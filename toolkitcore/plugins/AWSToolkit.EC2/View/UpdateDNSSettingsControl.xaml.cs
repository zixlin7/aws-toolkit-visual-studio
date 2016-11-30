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

        public override string Title
        {
            get { return "Update DNS Settings"; }
        }

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
