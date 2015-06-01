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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AddNetworkAclRuleControl.xaml
    /// </summary>
    public partial class AddNetworkAclRuleControl : BaseAWSControl
    {
        AddNetworkAclRuleController _controller;

        public AddNetworkAclRuleControl(AddNetworkAclRuleController controller)
        {
            InitializeComponent(); 
            this._controller = controller;
            this.DataContext = this._controller.Model;
            this._ctlProtocol.ItemsSource = NetworkProtocol.AllProtocolsWithWildCard;
        }

        public override string Title
        {
            get
            {
                return "Add Network Acl Rule";
            }
        }

        public override bool Validated()
        {
            var model = this._controller.Model;

            int ruleNumber;
            if (!int.TryParse(model.RuleNumber, out ruleNumber))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Value for rule number not a valid.");
                return false;
            }

            int start;
            if (!int.TryParse(model.PortRangeStart, out start))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Value for port range start is not a valid port number.");
                return false;
            }

            int end;
            if (!int.TryParse(model.PortRangeEnd, out end))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Value for port range end is not a valid port number.");
                return false;
            }

            if (end < start)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Port range end must be less then port range start.");
                return false;
            }

            if (string.IsNullOrEmpty(model.SourceCIDR))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Source CIDR is a required field.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateRule();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding rule: " + e.Message);
                return false;
            }
        }

        void onProtocolSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            NetworkProtocol newProtocol = e.AddedItems[0] as NetworkProtocol;
            NetworkProtocol oldProtocol = e.RemovedItems[0] as NetworkProtocol;

            this._ctlPortStart.IsEnabled = newProtocol.SupportsPortRange;
            this._ctlPortEnd.IsEnabled = newProtocol.SupportsPortRange;

            if (newProtocol.DefaultPort != null)
            {
                this._controller.Model.PortRangeStart = newProtocol.DefaultPort.Value.ToString();
                this._controller.Model.PortRangeEnd = newProtocol.DefaultPort.Value.ToString();
            }
            else if (oldProtocol.DefaultPort != null)
            {
                this._controller.Model.PortRangeStart = null;
                this._controller.Model.PortRangeEnd = null;
            }
        }

        void onPortStartFocusLost(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(this._controller.Model.PortRangeStart) || !string.IsNullOrEmpty(this._controller.Model.PortRangeEnd))
                return;

            int start;
            if (int.TryParse(this._controller.Model.PortRangeStart, out start))
            {
                this._controller.Model.PortRangeEnd = start.ToString();
                if (this._ctlPortEnd == e.NewFocus)
                {
                    this._ctlPortEnd.SelectAll();
                }
            }
        }
    }
}
