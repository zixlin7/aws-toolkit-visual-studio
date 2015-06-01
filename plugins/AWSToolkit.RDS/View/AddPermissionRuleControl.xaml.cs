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
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;

using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for AddPermissionRuleControl.xaml
    /// </summary>
    public partial class AddPermissionRuleControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(AddPermissionRuleControl));

        AddPermissionRuleController _controller;

        public AddPermissionRuleControl(AddPermissionRuleController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;

            if (!string.IsNullOrEmpty(this._controller.Model.CIDR))
            {
                this._ctlInfoMessage.Text = string.Format("Our best estimate for the CIDR of your current machine is {0}. However, if your machine is behind a proxy/firewall, this estimate may be inaccurate and you may need to contact your network administrator.", this._controller.Model.CIDR);
            }

            populateAccountIDs();
        }

        public override string Title
        {
            get { return "Add Permission"; }
        }

        public override bool Validated()
        {
            if (this._controller.Model.UseCidrIP)
            {
                if (string.IsNullOrEmpty(this._controller.Model.CIDR))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("CIDR/IP is a required field.");
                    return false;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(this._controller.Model.AWSUser) && string.IsNullOrEmpty(this._controller.SecurityGroup.VpcId))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("AWS account id is a required field.");
                    return false;
                }
                if (string.IsNullOrEmpty(this._controller.Model.EC2SecurityGroupName))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("EC2 security group is a required field.");
                    return false;
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.AuthorizeRule();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding permission rule", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding permission rule: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlCIDRValue.Focus();
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
                this._controller.Model.AWSUser = formatter(this._controller.CurrentAccount.AccountNumber, this._controller.CurrentAccount.AccountDisplayName);
                this._ctlAccountID.Text = this._controller.Model.AWSUser;
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
