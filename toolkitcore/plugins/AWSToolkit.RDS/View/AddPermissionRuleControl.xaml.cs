using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
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

        public override string Title => "Add Permission";

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

            if (ToolkitFactory.Instance.AwsConnectionManager != null && !string.IsNullOrEmpty(ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId))
            {
                this._controller.Model.AWSUser = formatter(ToolkitFactory.Instance.AwsConnectionManager.ActiveAccountId, ToolkitFactory.Instance.AwsConnectionManager.ActiveCredentialIdentifier.DisplayName);
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
