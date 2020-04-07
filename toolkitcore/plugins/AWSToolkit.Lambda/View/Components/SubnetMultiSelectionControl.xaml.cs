using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace Amazon.AWSToolkit.Lambda.View.Components
{
    /// <summary>
    /// Interaction logic for SubnetMultiSelectionControl.xaml
    /// </summary>
    public partial class SubnetMultiSelectionControl : UserControl
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private TextBlock _ctlVpcSubnetsSelectionDisplay;

        public SubnetMultiSelectionControl()
        {
            DataContext = this;

            AvailableVpcSubnets = new ObservableCollection<SelectableItem<VpcAndSubnetWrapper>>();

            InitializeComponent();
            this._ctlVpcSubnets.Loaded += _ctlVpcSubnets_Loaded;

            var vpcSubnetsView = (CollectionView)CollectionViewSource.GetDefaultView(_ctlVpcSubnets.ItemsSource);
            var vpcGroupDescription = new PropertyGroupDescription("InnerObject.VpcGroupingHeader");
            vpcSubnetsView.GroupDescriptions.Add(vpcGroupDescription);
        }

        public ObservableCollection<SelectableItem<VpcAndSubnetWrapper>> AvailableVpcSubnets { get; set; }

        public VpcAndSubnetWrapper SetAvailableVpcSubnets(IEnumerable<Vpc> vpcs, IEnumerable<Subnet> subnets, IEnumerable<string> selectedSubnetIds)
        {
            AvailableVpcSubnets.Clear();
            if (vpcs == null || vpcs.Count() == 0)
            {
                _ctlVpcSubnets.IsEnabled = false;
                return null;
            }

            VpcAndSubnetWrapper defaultSelection = null;

            // 'no vpc usage' default is to simply not select a subnet
            //defaultSelection = new VpcAndSubnetWrapper(VPCWrapper.NotInVpcPseudoId, SubnetWrapper.NoVpcSubnetPseudoId);
            //AvailableVpcSubnets.Add(defaultSelection);

            foreach (var v in vpcs)
            {
                var sortedSubnets = new SortedList<string, List<Subnet>>();
                foreach (var s in subnets)
                {
                    if (s.VpcId == v.VpcId)
                    {
                        // it's possible to have multiple subnets in same availability zone, if the user
                        // has set up public/private nets
                        var zone = s.AvailabilityZone;
                        List<Subnet> zoneSubnets;
                        if (sortedSubnets.ContainsKey(zone))
                            zoneSubnets = sortedSubnets[zone];
                        else
                        {
                            zoneSubnets = new List<Subnet>();
                            sortedSubnets.Add(zone, zoneSubnets);
                        }

                        zoneSubnets.Add(s);
                    }
                }

                foreach (var k in sortedSubnets.Keys)
                {
                    var zoneSubnets = sortedSubnets[k];
                    foreach (var s in zoneSubnets)
                    {
                        var item = new SelectableItem<VpcAndSubnetWrapper>(new VpcAndSubnetWrapper(v, s));
                        if (selectedSubnetIds != null && selectedSubnetIds.Contains(s.SubnetId))
                        {
                            item.IsSelected = true;
                            defaultSelection = item.InnerObject;
                        }
                        AvailableVpcSubnets.Add(item);
                    }
                }
            }

            if (defaultSelection != null)
            {
                _ctlVpcSubnets.SelectedItem = defaultSelection;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VpcSubnets"));
            }

            _ctlVpcSubnets.IsEnabled = true;
            _ctlVpcSubnets.Cursor = Cursors.Arrow;

            FormatDisplayValue();
            return defaultSelection;
        }

        public IEnumerable<SubnetWrapper> SelectedSubnets
        {
            get
            {
                var selections = new List<SubnetWrapper>();

                foreach (var item in AvailableVpcSubnets)
                {
                    if (item.IsSelected)
                        selections.Add(item.InnerObject.Subnet);
                }

                return selections;
            }
        }

        private void _ctlVpcSubnets_Loaded(object sender, RoutedEventArgs e)
        {
            var contentPresenter = this._ctlVpcSubnets.Template.FindName("ContentSite", this._ctlVpcSubnets) as ContentPresenter;
            if (contentPresenter != null)
            {
                this._ctlVpcSubnetsSelectionDisplay = contentPresenter.ContentTemplate.FindName("PART_ContentPresenter", contentPresenter) as TextBlock;

                // Since this is the first time the control is being loaded make sure if a value is already set that the text block has the display value for the selected value.
                FormatDisplayValue();
            }
        }

        private void SubnetsItemCheckBox_Clicked(object sender, RoutedEventArgs e)
        {
            FormatDisplayValue();
        }

        private void _ctlVpcSubnets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FormatDisplayValue();
        }

        /// <summary>
        /// This tracks selections between dropdown open/closes; if the selection
        /// remains static we avoid sending a change notification, as this would trigger
        /// a refresh of the security groups - losing any current selections
        /// </summary>
        private HashSet<string> PreviousSelections { get; set; }

        private void _ctlVpcSubnets_DropDownOpened(object sender, EventArgs e)
        {
            PreviousSelections = new HashSet<string>();
            foreach (var subnet in SelectedSubnets)
            {
                PreviousSelections.Add(subnet.SubnetId);
            }
        }

        private void _ctlVpcSubnets_DropDownClosed(object sender, EventArgs e)
        {
            if (SubnetsSpanVPCs)
            {
                MessageBox.Show("The selected subnets must belong to the same VPC.",
                                "Multiple VPC Selections",
                                MessageBoxButton.OK, MessageBoxImage.Error);
                // fire this so the host page can disable forwards nav
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VpcSubnets"));
            }
            else
            {
                // if the selection(s) remained constant, skip sending a change notification
                // so any security group selections remain valid and not reset
                var selectionChanged = false;
                foreach (var subnet in SelectedSubnets)
                {
                    if (PreviousSelections.Contains(subnet.SubnetId))
                        PreviousSelections.Remove(subnet.SubnetId);
                    else
                    {
                        selectionChanged = true;
                        break;
                    }
                }

                if (!selectionChanged && PreviousSelections.Count > 0)
                    selectionChanged = true;

                if (selectionChanged)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("VpcSubnets"));
            }

            PreviousSelections = null;
        }

        /// <summary>
        /// Returns true if the user has selected subnets that span multiple vpcs,
        /// which is not permitted.
        /// </summary>
        public bool SubnetsSpanVPCs
        {
            get
            {
                var vpcIds = new HashSet<string>();
                foreach (var item in AvailableVpcSubnets)
                {
                    if (item.IsSelected)
                        vpcIds.Add(item.InnerObject.Vpc.VpcId);
                }

                return vpcIds.Count > 1;
            }
        }

        private void FormatDisplayValue()
        {
            if (this._ctlVpcSubnetsSelectionDisplay == null) return;

            var sb = new StringBuilder();
            foreach (var VpcSubnet in AvailableVpcSubnets.OrderBy((x) => x.InnerObject.Subnet.DisplayName))
            {
                if (!VpcSubnet.IsSelected)
                    continue;

                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(VpcSubnet.InnerObject.Subnet.SubnetId);
            }

            this._ctlVpcSubnetsSelectionDisplay.Text = sb.ToString();
        }
    }
}
