using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.View.DataGrid;

using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class RunningInstanceWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        Reservation _reservation;
        Instance _instance;
        AddressWrapper _address;

        public RunningInstanceWrapper(Reservation reservation, Instance instance)
            : this(reservation, instance, null) { }

        public RunningInstanceWrapper(Reservation reservation, Instance instance, AddressWrapper address)
        {
            this._reservation = reservation;
            this._instance = instance;
            this._address = address;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Instance";
            componentName = this.InstanceId;
        }

        [DisplayName("Instance ID")]
        [AssociatedIcon(true, "InstanceIcon")]
        public string InstanceId
        {
            get { return this._instance.InstanceId; }
        }

        [DisplayName("AMI ID")]
        public string ImageId
        {
            get { return this._instance.ImageId; }
        }

        [DisplayName("Status")]
        [AssociatedIcon(true, "StatusIcon")]
        public string Status
        {
            get { return this._instance.State.Name; }
        }

        [DisplayName("VPC ID")]
        public string VpcId
        {
            get { return this._instance.VpcId; }
        }

        [DisplayName("Instance Profile")]
        public string FormattedInstanceProfile
        {
            get 
            {
                IamInstanceProfile instanceProfile = this._instance.IamInstanceProfile;
                if (instanceProfile != null && !string.IsNullOrEmpty(instanceProfile.Arn))
                {
                    string fullArn = instanceProfile.Arn;
                    int arnNamePosition = fullArn.LastIndexOf('/');
                    if (arnNamePosition >= 0)
                    {
                        string arnName = fullArn.Substring(arnNamePosition + 1);
                        return arnName;
                    }
                    else
                    {
                        return fullArn;
                    }
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        [DisplayName("Source/Dest. Check")]
        public bool SourceDestCheck
        {
            get { return this._instance.SourceDestCheck; }
        }

        [DisplayName("Placement Group")]
        public string PlacementGroup
        {
            get { return this._instance.Placement.GroupName; }
        }

        [DisplayName("RAM Disk ID")]
        public string RAMDiskId
        {
            get { return this._instance.RamdiskId; }
        }

        [DisplayName("Key Pair Name")]
        public string KeyPairName
        {
            get { return this._instance.KeyName; }
        }

        [DisplayName("Root Device Type")]
        public string RootDeviceType
        {
            get { return this._instance.RootDeviceType; }
        }

        [DisplayName("Root Device")]
        public string RootDeviceName
        {
            get { return this._instance.RootDeviceName; }
        }

        [DisplayName("Lifecycle")]
        public string InstanceLifecycle
        {
            get { return this._instance.InstanceLifecycle; }
        }

        [DisplayName("Security Groups")]
        public string FormattedSecurityGroups
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var group in this._instance.SecurityGroups)
                {
                    if (string.IsNullOrEmpty(group.GroupName))
                        continue;

                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(group.GroupName);
                }
                return sb.ToString();
            }
        }

        [DisplayName("Block Devices")]
        public string BlockDevices
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (var dev in this._instance.BlockDeviceMappings)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(dev.DeviceName);
                }
                return sb.ToString(); 
            }
        }

        [DisplayName("Public DNS")]
        public string PublicDnsName
        {
            get { return this._instance.PublicDnsName; }
        }

        [DisplayName("Private DNS")]
        public string PrivateDnsName
        {
            get { return this._instance.PrivateDnsName; }
        }

        [DisplayName("Elastic IP")]
        public string ElasticIPAddress
        {
            get 
            {
                if (this._address == null)
                    return null;

                return this._address.PublicIp; 
            }
        }

        [DisplayName("Private IP Address")]
        public string PrivateIPAddress
        {
            get { return this._instance.PrivateIpAddress; }
        }

        [DisplayName("State Transition Reason")]
        public string StateTransitionReason
        {
            get 
            {
                if (this._instance.StateReason == null)
                    return null;

                return this._instance.StateReason.Message; 
            }
        }

        [Browsable(false)]
        public string ConnectName
        {
            get
            {
                string lookupName = null;
                if (!string.IsNullOrEmpty(this.NativeInstance.PublicDnsName))
                {
                    lookupName = this.NativeInstance.PublicDnsName;
                }
                else if (!string.IsNullOrEmpty(this.NativeInstance.PublicIpAddress))
                {
                    lookupName = this.NativeInstance.PublicIpAddress;
                }
                return lookupName;
            }
        }

        [DisplayName("Zone")]
        public string AvailabilityZone
        {
            get { return this._instance.Placement.AvailabilityZone; }
        }

        [DisplayName("Subnet ID")]
        public string SubnetId
        {
            get { return this._instance.SubnetId; }
        }

        [DisplayName("Owner")]
        public string Owner
        {
            get { return this.NativeReservation.OwnerId; }
        }

        [DisplayName("Virtualization")]
        public string Virtualization
        {
            get { return this._instance.VirtualizationType; }
        }

        [DisplayName("Reservation")]
        public string ReservationId
        {
            get { return this._reservation.ReservationId; }
        }

        [DisplayName("Platform")]
        public string Platform
        {
            get { return this._instance.Platform; }
        }

        internal bool IsWindowsPlatform
        {
            // have seen EC2 use both 'windows' and 'Windows'
            get { return EC2Constants.PLATFORM_WINDOWS.Equals(this.NativeInstance.Platform, StringComparison.OrdinalIgnoreCase); }            
        }

        [DisplayName("Kernel ID")]
        public string KernelId
        {
            get { return this._instance.KernelId; }
        }

        public string Name
        {
            get 
            {
                string name = string.Empty;
                var tag = this._instance.Tags.Find(item => item.Key.Equals(EC2Constants.TAG_NAME));
                if (tag != null && !string.IsNullOrEmpty(tag.Value))
                {
                    name = tag.Value;
                }
                return name;
            }
        }

        [DisplayName("Type")]
        [ReadOnly(true)]
        public string InstanceType
        {
            get { return this.NativeInstance.InstanceType; }
            set
            {
                this.NativeInstance.InstanceType = value;
                base.NotifyPropertyChanged("InstanceType");
            }
        }

        [Browsable(false)]
        public Reservation NativeReservation
        {
            get { return this._reservation; }
        }

        [Browsable(false)]
        public Instance NativeInstance
        {
            get { return this._instance; }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get 
            {
                if (Name != null && Name.Length > 0)
                    return String.Format("{0} ({1})", Name, NativeInstance.InstanceId);

                return NativeInstance.InstanceId; 
            }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Instance"; }          
        }

        [DisplayName("Launch Time")]
        public DateTime LaunchTime
        {
            get { return this._instance.LaunchTime; }
        }

        [Browsable(false)]
        public string SecurityGroups
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (var group in this._reservation.GroupNames)
                {
                    if (sb.Length > 0)
                        sb.Append(", ");
                    sb.Append(group);
                }
                return sb.ToString(); 
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource InstanceIcon
        {
            get
            {
                var iconPath = IsWindowsPlatform ? "instance-windows.gif" : "instance-generic.png";
                var icon  = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                switch (this.NativeInstance.State.Name)
                {
                    case EC2Constants.INSTANCE_STATE_RUNNING:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.INSTANCE_STATE_TERMINATED:
                    case EC2Constants.INSTANCE_STATE_STOPPED:
                        iconPath = "red-circle.png";
                        break;
                    default:
                        iconPath = "yellow-circle.png";
                        break;
                }

                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        [Browsable(false)]
        public bool IsVolumesReady
        {
            get { return this.Volumes != null; }
        }

        ObservableCollection<VolumeWrapper> _volumes;
        [Browsable(false)]
        public ObservableCollection<VolumeWrapper> Volumes
        {
            get { return this._volumes; }
            set
            {
                this._volumes = value;
                base.NotifyPropertyChanged("Volumes");
                base.NotifyPropertyChanged("IsVolumesReady");
            }
        }

        [Browsable(false)]
        public string[] ListVolumeAvailableTags
        {
            get
            {
                return EC2ColumnDefinition.GetListAvailableTags(this.Volumes);
            }
        }

        [Browsable(false)]
        public IList<string> UnmappedDeviceSlots
        {
            get
            {
                List<string> _unmappedDeviceSlots = new List<string>();
                HashSet<string> mapped = new HashSet<string>();

                foreach (var mapping in NativeInstance.BlockDeviceMappings)
                {
                    mapped.Add(mapping.DeviceName);
                }

                string path = IsWindowsPlatform ? "xvd" : "/dev/sd";
                for (char l = 'f'; l < 'q'; l++)
                {
                    string slot = path + l;
                    if (!mapped.Contains(slot))
                    {
                        _unmappedDeviceSlots.Add(slot);
                    }
                }

                return _unmappedDeviceSlots;
            }
        }

        public Tag FindTag(string name)
        {
            if (this._instance.Tags == null)
                return null;

            return this._instance.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this._instance.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeInstance.Tags; }
        }
    }
}
