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

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;

using log4net;
namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for PromptAddCurrentCIDRControl.xaml
    /// </summary>
    public partial class PromptAddCurrentCIDRControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(PromptAddCurrentCIDRControl));
        PromptAddCurrentCIDRController _controller;
        public PromptAddCurrentCIDRControl(PromptAddCurrentCIDRController controller)
        {
            InitializeComponent();

            var currentCidr = IPAddressUtil.DetermineIPFromExternalSource() + "/32";
            this._controller = controller;
            this._ctlInfoMessage.Text = string.Format("Our best estimate for the CIDR of your current machine is {0}. However, if your machine is behind a proxy/firewall, this estimate may be inaccurate and you may need to contact your network administrator.", currentCidr);
            this._ctlCIDRValue.Text = currentCidr;
        }

        public override string Title
        {
            get
            {
                return "Unable to Connect";
            }
        }

        public override bool Validated()
        {
            return base.Validated();
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AddPermission(this._ctlCIDRValue.Text);
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding permission for " + this._ctlCIDRValue.Text, e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding permission: " + e.Message);
                return false;
            }
        }
    }
}
