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
using System;

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

        private string extractSubnetIdsFromSelection(IEnumerable<SubnetWrapper> subnets)
        {
            var strs = subnets
                .Where(x => x.IsSelected)
                .Select(x => x.SubnetId);

            var str = string.Join(",", strs);

            return str.Length == 0 ? null : str;
        }

        public string SelectedInstanceSubnetId
        {
            get => extractSubnetIdsFromSelection(_instancesSubnets.ItemsSource as IEnumerable<SubnetWrapper>);
        }

        public string SelectedELBSubnetId
        {
            get => extractSubnetIdsFromSelection(_elbSubnets.ItemsSource as IEnumerable<SubnetWrapper>);
        }

        public void SetAvailableSubnets(IList<Subnet> subnets)
        {
            var instanceSubnets = new List<SubnetWrapper>();
            var elbSubnets = new List<SubnetWrapper>();

            if(subnets != null)
            {
                foreach (var subnet in subnets)
                {
                    instanceSubnets.Add(new SubnetWrapper(subnet));
                    elbSubnets.Add(new SubnetWrapper(subnet));
                }
            }

            _instancesSubnets.ItemsSource = instanceSubnets;
            _elbSubnets.ItemsSource = elbSubnets;

            _instancesSubnets.Cursor = Cursors.Arrow;
            _elbSubnets.Cursor = Cursors.Arrow;
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

            FormatMultiSelectDisplayValue(this._instancesSubnets);
            NotifyPropertyChanged("instance-subnets");
        }

        private void _instancesSubnets_DropDownClosed(object sender, EventArgs e)
        {
            FormatMultiSelectDisplayValue(this._instancesSubnets);
            NotifyPropertyChanged("instance-subnets");
        }

        private void _elbSubnets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsInitialized)
            {
                return;
            }

            FormatMultiSelectDisplayValue(this._elbSubnets);
            NotifyPropertyChanged("elb-subnets");
        }

        private void _elbSubnets_DropDownClosed(object sender, EventArgs e)
        {
            FormatMultiSelectDisplayValue(this._elbSubnets);
            NotifyPropertyChanged("elb-subnets");
        }

        private void FormatMultiSelectDisplayValue(ComboBox control)
        {
            var contentPresenter = control.Template.FindName("ContentSite", control) as ContentPresenter;
            if (contentPresenter == null)
            {
                return;    
            }

            var textBlock = contentPresenter.ContentTemplate.FindName("PART_ContentPresenter", contentPresenter) as TextBlock;
            if(textBlock == null)
            {
                return;
            }

            var subnets = control?.ItemsSource as IEnumerable<SubnetWrapper>;
            if (subnets == null)
            {
                textBlock.Text = string.Empty;
                return;
            }

            textBlock.Text = string.Join(", ", subnets.Where(x => x.IsSelected).Select(x => x.SubnetId).OrderBy(id => id));
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

        public class SubnetWrapper
        {
            Subnet _subnet;

            public SubnetWrapper(Subnet subnet)
            {
                this._subnet = subnet;
            }

            public bool IsSelected { get; set; }
            public string SubnetId => _subnet.SubnetId;
            public string AvailabilityZone => _subnet.AvailabilityZone;

            public string FormattedTags
            {
                get
                {
                    var formattedtags = string.Join(", ", this._subnet.Tags.Select(tag => $"{tag.Key}={tag.Value}"));

                    // Make sure we are stretching the combo box beyound reasonable.
                    if(formattedtags.Length > 100)
                    {
                        formattedtags = formattedtags.Substring(0, 96) + " ...";
                    }

                    return formattedtags;
                }
            }

            public string ToolTipTags
            {
                get
                {
                    return string.Join("\n", this._subnet.Tags.Select(tag => $"{tag.Key}={tag.Value}"));
                }
            }
        }
    }
}
