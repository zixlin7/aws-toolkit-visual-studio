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

        public override string Title => "Open Port";

        public string IPAddress => this._ctlIPAddress.Text;
    }
}
