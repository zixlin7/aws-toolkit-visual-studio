using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for AddIPPermissionControl.xaml
    /// </summary>
    public partial class AddIPPermissionControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AddIPPermissionControl));

        AddIPPermissionController _controller;

        public AddIPPermissionControl(AddIPPermissionController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
            this._ctlProtocol.ItemsSource = NetworkProtocol.AllProtocols;

            var ipAddress = IPAddressUtil.DetermineIPFromExternalSource();
            if (!string.IsNullOrEmpty(ipAddress))
            {
                this._ctlInfo.Text = string.Format("Our best estimate for the CIDR of your current machine is {0}. However, if your machine is behind a proxy/firewall, this estimate may be inaccurate and you may need to contact your network administrator.", ipAddress);
            }

            populateAccountIDs();
        }

        public override string Title => "Add IP Permission";

        public override bool Validated()
        {
            var model = this._controller.Model;

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

            if (model.IsPortAndIpChecked)
            {
                if (string.IsNullOrEmpty(model.SourceCIDR))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Source CIDR is a required field.");
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(model.UserId))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("User ID is a required field.");
                    return false;
                }
                if (string.IsNullOrEmpty(model.GroupName))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Security Group Name is a required field.");
                    return false;
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateIPPermission();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error authorizing permission: " + e.Message);
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
            if(string.IsNullOrEmpty(this._controller.Model.PortRangeStart) || !string.IsNullOrEmpty(this._controller.Model.PortRangeEnd))
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

        void populateAccountIDs()
        {
            Func<string, string, string> formatter = (accountNumber, accountDisplayName) => { return string.Format("{0} ({1})", accountNumber.Replace("-", "").Trim(), accountDisplayName); };

            Dictionary<string, string> accountIds = new Dictionary<string, string>();
            foreach (var account in ToolkitFactory.Instance.RootViewModel.RegisteredAccounts)
            {
                if (!string.IsNullOrEmpty(account.AccountNumber))
                {
                    var num = account.AccountNumber.Replace("-", "").Trim();
                    if (!accountIds.ContainsKey(num))
                    {
                        accountIds.Add(num, formatter(account.AccountNumber, account.AccountDisplayName));
                    }
                }
            }

            foreach (var value in accountIds.Values.OrderBy(x => x))
            {
                this._ctlAccountID.Items.Add(value);
            }

            if (this._controller.CurrentAccount != null && !string.IsNullOrEmpty(this._controller.CurrentAccount.AccountNumber))
            {
                this._controller.Model.UserId = formatter(this._controller.CurrentAccount.AccountNumber, this._controller.CurrentAccount.AccountDisplayName);
                this._ctlAccountID.Text = this._controller.Model.UserId;
                updateAvailableSecurityGroups();
            }
        }

        private void _ctlAccountID_LostFocus(object sender, RoutedEventArgs e)
        {
            updateAvailableSecurityGroups();
        }

        void updateAvailableSecurityGroups()
        {
            this._ctlSecurityGroups.Items.Clear();
            try
            {
                var securityGroups = this._controller.GetEC2SecurityGroups(this._ctlAccountID.Text);


                foreach (var group in securityGroups.OrderBy(x => x))
                    this._ctlSecurityGroups.Items.Add(group);
            }
            catch (Exception e)
            {
                LOGGER.Info("Error attempting to get security groups for " + this._ctlSecurityGroups.Text, e);
            }
        }
    }
}
