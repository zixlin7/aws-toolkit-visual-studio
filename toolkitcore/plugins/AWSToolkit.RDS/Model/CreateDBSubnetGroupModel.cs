using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.RDS.Model
{
    public class CreateDBSubnetGroupModel : BaseModel
    {
        string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                base.NotifyPropertyChanged("Name");
            }
        }


        string _description;
        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                base.NotifyPropertyChanged("Description");
            }
        }

        public VPCWrapper SelectedVPC { get; set; }

        readonly ObservableCollection<VPCWrapper> _vpcList = new ObservableCollection<VPCWrapper>(); 
        public ObservableCollection<VPCWrapper> VPCList
        {
            get => _vpcList;
            set
            {
                _vpcList.Clear();
                if (value != null)
                {
                    foreach (var v in value)
                    {
                        _vpcList.Add(v);
                    }
                }
                base.NotifyPropertyChanged("VPCList");
            }
        }

        public void LoadVPCList(IEnumerable<Vpc> vpcList)
        {
            _vpcList.Clear();
            if (vpcList != null)
            {
                foreach (var v in vpcList)
                {
                    _vpcList.Add(new VPCWrapper(v));
                }
            }
            base.NotifyPropertyChanged("VPCList");

            if (_vpcList.Count == 1)
            {
                SelectedVPC = _vpcList[0];
                base.NotifyPropertyChanged("SelectedVPC");
            }
        }

        private AvailabilityZone _selectedZone;
        public AvailabilityZone SelectedZone
        {
            get => _selectedZone;
            set
            {
                _selectedZone = value; 
                base.NotifyPropertyChanged("SelectedZone");
            }
        }

        readonly ObservableCollection<AvailabilityZone> _allAvailabilityZones = new ObservableCollection<AvailabilityZone>();

        public ObservableCollection<AvailabilityZone> AllAvailabilityZones
        {
            get => _allAvailabilityZones;
            set
            {
                _allAvailabilityZones.Clear();
                if (value != null)
                {
                    foreach (var v in value)
                    {
                        _allAvailabilityZones.Add(v);                        
                    }
                }
                base.NotifyPropertyChanged("AllAvailabilityZones");
            }
        }

        public void LoadAvailabilityZones(IEnumerable<AvailabilityZone> zones)
        {
            _allAvailabilityZones.Clear();
            if (zones != null)
            {
                foreach (var az in zones)
                {
                    _allAvailabilityZones.Add(az);
                }
            }
            base.NotifyPropertyChanged("AllAvailabilityZones");
        }

        public SubnetWrapper SelectedSubnet { get; set; }

        internal IEnumerable<SubnetWrapper> AllSubnets { get; set; }

        public void LoadAllSubnets(IEnumerable<Subnet> subnets)
        {
            var allSubnets = new List<SubnetWrapper>();

            if (subnets != null)
                allSubnets.AddRange(subnets.Select(subnet => new SubnetWrapper(subnet, null, null)));

            AllSubnets = allSubnets;
        }

        readonly ObservableCollection<SubnetWrapper> _subnetsForVPCZone = new ObservableCollection<SubnetWrapper>();

        /// <summary>
        /// The collection of subnets belonging to the currently selected VPC and
        /// availability zone.
        /// </summary>
        public IEnumerable<SubnetWrapper> SubnetsForVPCZone
        {
            get => _subnetsForVPCZone;
            set
            {
                _subnetsForVPCZone.Clear();
                if (value != null)
                {
                    foreach (var v in value)
                    {
                        _subnetsForVPCZone.Add(v);
                    }
                }

                base.NotifyPropertyChanged("SubnetsForVPC");

                if (_subnetsForVPCZone.Count == 1)
                {
                    SelectedSubnet = _subnetsForVPCZone[0];
                    base.NotifyPropertyChanged("SelectedSubnet");
                }
            }
        }

        readonly ObservableCollection<AssignedSubnet> _assignedSubnets = new ObservableCollection<AssignedSubnet>();

        /// <summary>
        /// The collection of subnets the user has selected to use in the subnet group.
        /// </summary>
        public ObservableCollection<AssignedSubnet> AssignedSubnets
        {
            get => _assignedSubnets;
            set
            {
                _assignedSubnets.Clear();
                if (value != null)
                {
                    foreach (var v in value)
                    {
                        _assignedSubnets.Add(v);
                    }
                }
                base.NotifyPropertyChanged("AssignedSubnets");
            }
        }

        internal void AddAssignedSubnet(AssignedSubnet assignedSubnet)
        {
            _assignedSubnets.Add(assignedSubnet);
            base.NotifyPropertyChanged("AssignedSubnets");

            foreach (var subnet in SubnetsForVPCZone)
            {
                if (subnet.SubnetId.Equals(assignedSubnet.SubnetId, StringComparison.OrdinalIgnoreCase))
                {
                    _subnetsForVPCZone.Remove(subnet);
                    base.NotifyPropertyChanged("SubnetsForVPCZone");

                    SelectedSubnet = null;
                    base.NotifyPropertyChanged("SelectedSubnet");
                    break;
                }
            }
        }

        internal void RemoveAssignedSubnet(AssignedSubnet assignedSubnet)
        {
            _assignedSubnets.Remove(assignedSubnet);
            base.NotifyPropertyChanged("AssignedSubnets");
        }

        internal void SetAssignedSubnets(IEnumerable<AssignedSubnet> assignedSubnets)
        {
            _assignedSubnets.Clear();
            if (assignedSubnets != null)
            {
                foreach (var v in assignedSubnets)
                {
                    _assignedSubnets.Add(v);
                }
            }
            base.NotifyPropertyChanged("AssignedSubnets");
        }

    }

    public class AssignedSubnet
    {
        public string AvailabilityZone { get; set; }
        public string SubnetId { get; set; }
        public string CidrBlock { get; set; }
    }
}
