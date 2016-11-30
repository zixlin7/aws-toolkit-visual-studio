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

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AskToOpenPort.xaml
    /// </summary>
    public partial class AskToOpenPort : BaseAWSControl
    {
        public AskToOpenPort(string message, string initialIp)
        {
            InitializeComponent();

            this._ctlMessage.Text = message;
            this._ctlIPAddress.Text = initialIp;

            if (!string.IsNullOrEmpty(initialIp))
            {
                this._ctlInfo.Text = string.Format("Our best estimate for the CIDR of your current machine is {0}. However, if your machine is behind a proxy/firewall, this estimate may be inaccurate and you may need to contact your network administrator.", initialIp);
            }
        }

        public override string Title
        {
            get
            {
                return "Open Port";
            }
        }

        public string IPAddress
        {
            get { return this._ctlIPAddress.Text; }
        }
    }
}
