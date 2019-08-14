using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Navigation;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2;
using Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageControllers;
using Amazon.EC2.Model;
using log4net;

namespace Amazon.AWSToolkit.ElasticBeanstalk.WizardPages.PageUI.Deployment
{
    /// <summary>
    /// Interaction logic for VpcOptionsPage.xaml
    /// </summary>
    public partial class VpcOptionsPage : INotifyPropertyChanged
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(VpcOptionsPage));

        public VpcOptionsPage()
        {
            InitializeComponent();
        }

        public VpcOptionsPage(IAWSWizardPageController controller)
            : this()
        {
            this.PageController = controller;
        }

        public IAWSWizardPageController PageController { get; set; }

        public string SelectedVPCId
        {
            get
            {
                if (!(this._vpcs.SelectedItem is KeyValuePair<string, Amazon.EC2.Model.Vpc>))
                {
                    return null;
                }

                var kvp = (KeyValuePair<string, Amazon.EC2.Model.Vpc>)this._vpcs.SelectedItem;
                return kvp.Value.VpcId;
            }
        }

        private string extractSubnetIdsFromSelection(IList selections)
        {
            var strs = selections
                .OfType<KeyValuePair<string, Subnet>?>()
                .Select(item => item?.Value.SubnetId)
                .Where(item => item != null);

            var str = string.Join(",", strs);

            return str.Length == 0 ? null : str;
        }

        public string SelectedInstanceSubnetId
        {
            get => extractSubnetIdsFromSelection(_instancesSubnets.SelectedItems);
        }

        public string SelectedELBSubnetId
        {
            get => extractSubnetIdsFromSelection(_elbSubnets.SelectedItems);
        }


        public string SelectedELBScheme
        {
            get
            {
                var item = _elbScheme.SelectedItem as ComboBoxItem;
                return item?.Tag as string;
            }
        }

        public string SelectedVPCSecurityGroupId
        {
            get
            {
                var kvp = _vpcSecurityGroup.SelectedItem as KeyValuePair<string, SecurityGroup>?;
                return kvp?.Value.GroupId;
            }
        }

        public IEnumerable<KeyValuePair<string, Amazon.EC2.Model.Vpc>> VPCs
        {
            set
            {
                if (value == null)
                {
                    _vpcs.ItemsSource = null;
                    _vpcs.Cursor = Cursors.Wait;
                    return;
                }

                this._vpcs.ItemsSource = value;
                if (_vpcs.Items.Count > 0)
                    _vpcs.SelectedIndex = 0;
                _vpcs.Cursor = Cursors.Arrow;
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

                bool isSingleInstanceEnvironment
                    = (this.PageController as VpcOptionsPageController).IsSingleInstanceEnvironmentType;
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
                {
                    _vpcSecurityGroup.SelectedIndex = 0;
                }

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

        public void ConfigureForEnvironmentType()
        {
            bool elbFieldsEnabled = !((this.PageController as VpcOptionsPageController).IsSingleInstanceEnvironmentType);

            this._elbScheme.Visibility = elbFieldsEnabled ? Visibility.Visible : Visibility.Hidden;
            this._elbSchemeNA.Visibility = elbFieldsEnabled ? Visibility.Hidden : Visibility.Visible;
            this._elbSubnets.Visibility = elbFieldsEnabled ? Visibility.Visible : Visibility.Hidden;
            this._elbSubnetsNA.Visibility = elbFieldsEnabled ? Visibility.Hidden : Visibility.Visible;
        }

        private void _vpc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            NotifyPropertyChanged("vpc");
        }

        private void _elbScheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            NotifyPropertyChanged("elb-scheme");
        }

        private void _instancesSubnets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            NotifyPropertyChanged("instance-subnets");
        }

        private void _elbSubnets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            NotifyPropertyChanged("elb-subnets");
        }

        private void _vpcSecurityGroup_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            NotifyPropertyChanged("vpc-security-group");
        }

        private void onVPCDocumentationRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.OriginalString));
            e.Handled = true;
        }
    }
}
