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
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageWorkers;
using Amazon.AWSToolkit.EC2;

namespace Amazon.AWSToolkit.ElasticBeanstalk.View.Components
{
    /// <summary>
    /// Confirmation dialog body for changing an environment type. For non-VPC
    /// environments, this is a simple message but for VPC-enabled environments,
    /// the user must do some configuration of VPC settings to change type. The
    /// VPC associated with the environment however cannot be changed.
    /// </summary>
    public partial class EnvTypeChangeControl : BaseAWSControl
    {
        string _vpcID;
        QueryVPCPropertiesWorker.VPCPropertyData _vpcPropertyData;

        public EnvTypeChangeControl()
        {
            DataContext = this;
            InitializeComponent();
        }

        public string ConfirmationMessage 
        {
            set { _confirmationMessage.Text = value; }
        }

        string _newEnvironmentType;
        public string NewEnvironmentType 
        {
            set { _newEnvironmentType = value; }
        }

        bool IsSingleInstanceEnvironment
        {
            get 
            { 
                return !string.IsNullOrEmpty(_newEnvironmentType) 
                    && _newEnvironmentType.Equals(BeanstalkConstants.EnvType_SingleInstance, StringComparison.Ordinal); 
            }
        }

        public void SetVPCData(string VPCId, QueryVPCPropertiesWorker.VPCPropertyData vpcPropertyData)
        {
            this._vpcID = VPCId;
            this._vpcPropertyData = vpcPropertyData;

            if (string.IsNullOrEmpty(_vpcID))
                _vpcSetupPanel.Visibility = Visibility.Collapsed;
            else
            {
                VPCSecurityGroups = _vpcPropertyData.SecurityGroups;

                if (IsSingleInstanceEnvironment)
                {
                    InstanceSubnets = _vpcPropertyData.Subnets;

                    SetHidden(_elbScheme, _elbSubnets);
                    SetVisible(_elbSchemeNA, _elbSubnetsNA);
                }
                else
                {
                    InstanceSubnets = _vpcPropertyData.Subnets;
                    ELBSubnets = _vpcPropertyData.Subnets;

                    SetHidden(_elbSchemeNA, _elbSubnetsNA);
                    SetVisible(_elbScheme, _elbSubnets);
                }

                _vpcSetupPanel.Visibility = Visibility.Visible;
            }
        }

        static void SetHidden(params UIElement[] controls)
        {
            foreach(var c in controls)
            {
                c.Visibility = Visibility.Hidden;
            }
        }

        static void SetVisible(params UIElement[] controls)
        {
            foreach (var c in controls)
            {
                c.Visibility = Visibility.Visible;
            }
        }

        public override string Title
        {
            get { return "Change Environment Type"; }
        }

        public string SelectedInstanceSubnetId
        {
            get
            {
                if (!(this._instancesSubnets.SelectedItem is KeyValuePair<string, Amazon.EC2.Model.Subnet>))
                    return null;

                var kvp = (KeyValuePair<string, Amazon.EC2.Model.Subnet>)this._instancesSubnets.SelectedItem;
                return kvp.Value.SubnetId;
            }
        }

        public string SelectedELBSubnetId
        {
            get
            {
                if (!(this._elbSubnets.SelectedItem is KeyValuePair<string, Amazon.EC2.Model.Subnet>))
                    return null;

                var kvp = (KeyValuePair<string, Amazon.EC2.Model.Subnet>)this._elbSubnets.SelectedItem;
                return kvp.Value.SubnetId;
            }
        }

        public string SelectedELBScheme
        {
            get
            {
                if (!(this._elbScheme.SelectedItem is ComboBoxItem))
                    return null;

                var item = this._elbScheme.SelectedItem as ComboBoxItem;
                return item.Tag as string;
            }
        }

        public string SelectedVPCSecurityGroupId
        {
            get
            {
                if (!(this._vpcSecurityGroup.SelectedItem is KeyValuePair<string, Amazon.EC2.Model.SecurityGroup>))
                    return null;

                var kvp = (KeyValuePair<string, Amazon.EC2.Model.SecurityGroup>)this._vpcSecurityGroup.SelectedItem;
                return kvp.Value.GroupId;
            }
        }

        public IEnumerable<KeyValuePair<string, Amazon.EC2.Model.Subnet>> InstanceSubnets
        {
            set
            {
                if (value == null)
                {
                    _instancesSubnets.ItemsSource = null;
                    _instancesSubnets.Cursor = Cursors.Wait;
                    return;
                }

                this._instancesSubnets.ItemsSource = value;
                _instancesSubnets.Cursor = Cursors.Arrow;

                bool isSingleInstanceEnvironment = IsSingleInstanceEnvironment;
                foreach (var subnet in value)
                {
                    if (isSingleInstanceEnvironment)
                    {
                        if (subnet.Key.StartsWith(EC2Constants.VPC_LAUNCH_PUBLIC_SUBNET_NAME))
                        {
                            this._instancesSubnets.SelectedItem = subnet;
                            break;
                        }
                    }
                    else if (subnet.Key.StartsWith(EC2Constants.VPC_LAUNCH_PRIVATE_SUBNET_NAME))
                    {
                        this._instancesSubnets.SelectedItem = subnet;
                        break;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<string, Amazon.EC2.Model.Subnet>> ELBSubnets
        {
            set
            {
                if (value == null)
                {
                    _elbSubnets.ItemsSource = null;
                    _elbSubnets.Cursor = Cursors.Wait;
                    return;
                }

                this._elbSubnets.ItemsSource = value;
                _elbSubnets.Cursor = Cursors.Arrow;

                foreach (var subnet in value)
                {
                    if (subnet.Key.StartsWith(EC2Constants.VPC_LAUNCH_PUBLIC_SUBNET_NAME))
                    {
                        this._elbSubnets.SelectedItem = subnet;
                        break;
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<string, Amazon.EC2.Model.SecurityGroup>> VPCSecurityGroups
        {
            set
            {
                if (value == null)
                {
                    _vpcSecurityGroup.ItemsSource = null;
                    _vpcSecurityGroup.Cursor = Cursors.Wait;
                    return;
                }

                this._vpcSecurityGroup.ItemsSource = value;
                if (_vpcSecurityGroup.Items.Count > 0)
                    _vpcSecurityGroup.SelectedIndex = 0;
                _vpcSecurityGroup.Cursor = Cursors.Arrow;

                foreach (var group in value)
                {
                    if (group.Key.StartsWith(EC2Constants.VPC_LAUNCH_NAT_GROUP))
                    {
                        this._vpcSecurityGroup.SelectedItem = group;
                        break;
                    }
                }
            }
        }
    }
}
