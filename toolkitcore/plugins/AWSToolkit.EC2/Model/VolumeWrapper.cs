using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.RightsManagement;
using System.Text;

using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2.Model;
using Amazon.AWSToolkit.EC2.View.DataGrid;
using Amazon.Runtime.Internal.Util;
using log4net;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class VolumeWrapper : PropertiesModel, IWrapper, ITagSupport
    {
        readonly Volume _volume;

        public const string ProvisionedIOPSTypeDisplayName = "Provisioned IOPS (SSD)";
        public const string ProvisionedIOPSTypeCode = "io1";

        public const string GeneralPurposeTypeDisplayName = "General Purpose (SSD)";
        public const string GeneralPurposeTypeCode = "gp2";
        
        public const string StandardTypeDisplayName = "Magnetic";
        public const string StandardTypeCode = "standard";

        public const string EBSVolumeType = "EBS";
        public const string InstanceStoreVolumeType = "Instance Store";

        public VolumeWrapper(Volume volume)
        {
            this._volume = volume;
        }

        public override void GetPropertyNames(out string className, out string componentName)
        {
            className = "Volume";
            componentName = this.VolumeId;
        }

        [Browsable(false)]
        public Volume NativeVolume
        {
            get { return _volume; }
        }

        [DisplayName("Volume ID")]
        [AssociatedIconAttribute(false, "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.volume.png")]
        public string VolumeId
        {
            get { return NativeVolume.VolumeId; }
        }

        [DisplayName("Name")]
        public string Name
        {
            get
            {
                string name = string.Empty;
                var tag = this.NativeVolume.Tags.Find(item => item.Key.Equals(EC2Constants.TAG_NAME));
                if (tag != null && !string.IsNullOrEmpty(tag.Value))
                {
                    name = tag.Value;
                }
                return name;
            }
        }

        [Browsable(false)]
        public string DisplayName
        {
            get { return this.NativeVolume.VolumeId; }
        }

        [Browsable(false)]
        public string TypeName
        {
            get { return "Volume"; }
        }

        [DisplayName("Created")]
        public DateTime Created
        {
            get { return Convert.ToDateTime(NativeVolume.CreateTime); }
        }

        [DisplayName("Zone")]
        public string AvailabilityZone
        {
            get { return this.NativeVolume.AvailabilityZone; }
        }

        [DisplayName("Snapshot ID")]
        public string SnapshotId
        {
            get { return NativeVolume.SnapshotId; }
        }

        [DisplayName("Capacity")]
        public string Capacity
        {
            get { return NativeVolume.Size + " GiB"; }
        }

        [DisplayName("Size")]
        public string FormattedSize
        {
            get { return Convert.ToInt32(NativeVolume.Size) + " GiB"; }
        }

        public string VolumeType
        {
            get { return NativeVolume.VolumeType; }
        }

        [DisplayName("Volume Type")]
        public string VolumeTypeDisplayName 
        {
            get
            {
                if (ProvisionedIOPSTypeCode.Equals(VolumeType, StringComparison.OrdinalIgnoreCase))
                    return string.Format("{0} [{1}]", ProvisionedIOPSTypeDisplayName, VolumeType);

                return string.Format("{0} [{1}]",
                                     GeneralPurposeTypeCode.Equals(VolumeType, StringComparison.OrdinalIgnoreCase)
                                         ? GeneralPurposeTypeDisplayName
                                         : StandardTypeDisplayName,
                                     VolumeType);
            }
        }

        [DisplayName("IOPS")]
        public string Iops
        {
            get
            {
                return NativeVolume.VolumeType.Equals(Amazon.EC2.VolumeType.Io1)
                           ? NativeVolume.Iops.ToString()
                           : string.Empty;
            }
        }

        [Browsable(false)]
        public int Size
        {
            get { return Convert.ToInt32(NativeVolume.Size); }
        }

        [DisplayName("Attachment Information")]
        public string Attachments
        {
            get
            {
                string[] atts = NativeVolume.Attachments.Select(i => String.Format("{0}:{1} ({2})", i.InstanceId, i.Device, i.State)).ToArray();
                return String.Join(", ", atts);
            }
        }

        [Browsable(false)]
        public bool IsSnapshotsReady
        {
            get { return this.Snapshots != null; }
        }

        [Browsable(false)]
        public bool CanAddSnapshots
        {
            get{return this.NativeVolume.State == EC2Constants.VOLUME_STATE_AVAILABLE || this.NativeVolume.State == EC2Constants.VOLUME_STATE_IN_USE;}
        }

        ObservableCollection<SnapshotWrapper> _snapshots;
        [Browsable(false)]
        public ObservableCollection<SnapshotWrapper> Snapshots
        {
            get { return _snapshots; }
            set
            {
                _snapshots = value;
                base.NotifyPropertyChanged("IsSnapshotsReady");
                base.NotifyPropertyChanged("Snapshots");
            }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource InstanceIcon
        {
            get
            {
                string iconPath= "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.volume.png";

                var icon = IconHelper.GetIcon(this.GetType().Assembly, iconPath);
                return icon.Source;
            }
        }

        [Browsable(false)]
        public bool CanDelete
        {
            get
            {
                return (NativeVolume.State.Equals("available"));
            }
        }

        [DisplayName("Status")]
        [AssociatedIcon(true, "StatusIcon")]
        public string Status
        {
            get { return NativeVolume.State; }
        }

        [Browsable(false)]
        public System.Windows.Media.ImageSource StatusIcon
        {
            get
            {
                string iconPath;
                Assembly assembly = null;
                switch (NativeVolume.State)
                {
                    case EC2Constants.VOLUME_STATE_IN_USE:
                        iconPath = "green-circle.png";
                        break;
                    case EC2Constants.VOLUME_STATE_DELETING:
                        iconPath = "red-circle.png";
                        break;
                    case EC2Constants.VOLUME_STATE_CREATING:
                        iconPath = "yellow-circle.png";
                        break;
                    default:
                        iconPath = "Amazon.AWSToolkit.EC2.Resources.EmbeddedImages.volume.png";
                        assembly = this.GetType().Assembly;
                        break;
                }

                System.Windows.Controls.Image icon;
                if(assembly == null)
                    icon = IconHelper.GetIcon(iconPath);
                else
                    icon = IconHelper.GetIcon(assembly, iconPath);

                return icon.Source;
            }
        }


        public Tag FindTag(string name)
        {
            if (this._volume.Tags == null)
                return null;

            return this._volume.Tags.FirstOrDefault(x => string.Equals(x.Key, name));
        }

        public void SetTag(string name, string value)
        {
            var tag = FindTag(name);
            if (tag == null)
            {
                tag = new Tag();
                tag.Key = name;
                tag.Value = value;
                this._volume.Tags.Add(tag);
            }
            else
            {
                tag.Value = value;
            }
        }

        [Browsable(false)]
        public List<Tag> Tags
        {
            get { return this.NativeVolume.Tags; }
        }

        public string[] ListSnapshotsAvailableTags
        {
            get
            {
                return EC2ColumnDefinition.GetListAvailableTags(this.Snapshots);
            }
        }
    }

    /// <summary>
    /// Used in the launch wizard to wrap a proposed volume to be added to
    /// the newly launched instance.
    /// </summary>
    public class InstanceLaunchStorageVolume
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(InstanceLaunchStorageVolume));

        private readonly string _id = Guid.NewGuid().ToString("N");  
        
        public InstanceLaunchStorageVolume()
        {
            Iops = EC2ServiceMeta.Instance.MinIops;
            // always default to EBS, since not all instance types
            // allow instance store volumes (and if they do, the number
            // can be limited)
            StorageType = VolumeWrapper.EBSVolumeType; 
        }

        public string StorageType { get; set; }

        public string[] AllStorageTypes =
        {
            VolumeWrapper.EBSVolumeType,
            VolumeWrapper.InstanceStoreVolumeType
        };

        // this is used solely to get use from control events on
        // a template instance to 'this' (eg the delete button)
        public string ID
        {
            get { return _id; }    
        }

        public string[] AvailableStorageTypes
        {
            get;
            protected set;
        }

        public string Device { get; set; }
        public int Size { get; set; }
        public int Iops { get; set; }
        public bool DeleteOnTermination { get; set; }
        public bool Encrypted { get; set; }

        public static string IopsRangeTooltip
        {
            get
            {
                return "The number of I/O operations per second (IOPS) to provision for the volume. Range is 100 to 4000";
            }
        }

        private static readonly CreateVolumeModel.VolumeTypeOption[] VolumeTypeOptions =
        {
            new CreateVolumeModel.VolumeTypeOption
            {
                TypeCode = VolumeWrapper.GeneralPurposeTypeCode, 
                TypeName = VolumeWrapper.GeneralPurposeTypeDisplayName
            },
            new CreateVolumeModel.VolumeTypeOption
            {
                TypeCode = VolumeWrapper.ProvisionedIOPSTypeCode, 
                TypeName = VolumeWrapper.ProvisionedIOPSTypeDisplayName
            },
            new CreateVolumeModel.VolumeTypeOption
            {
                TypeCode = VolumeWrapper.StandardTypeCode, 
                TypeName = VolumeWrapper.StandardTypeDisplayName
            },
        };

        public static CreateVolumeModel.VolumeTypeOption VolumeTypeFromCode(string typeCode)
        {
            foreach (var v in VolumeTypes.Where(v => v.TypeCode.Equals(typeCode, StringComparison.Ordinal)))
            {
                return v;
            }

            throw new ArgumentException("Unknown volume type code: " + typeCode);
        }

        public static CreateVolumeModel.VolumeTypeOption[] VolumeTypes
        {
            get { return VolumeTypeOptions; }
        }

        public CreateVolumeModel.VolumeTypeOption VolumeType 
        { 
            get; 
            set; 
        }

        ObservableCollection<SnapshotModel> _snapshots = new ObservableCollection<SnapshotModel>();

        public ObservableCollection<SnapshotModel> Snapshots
        {
            get { return _snapshots; }
            set { _snapshots = value; }
        }

        public SnapshotModel Snapshot { get; set; }

        public bool IsRootDevice
        {
            get
            {
                return "/dev/sda1".Equals(Device, StringComparison.OrdinalIgnoreCase)
                       || "/dev/xvda".Equals(Device, StringComparison.OrdinalIgnoreCase);
            }
        }

        // simpler xaml binding than needing a 'bool inverter' converter
        public bool IsNonRootDevice
        {
            get { return !IsRootDevice; }            
        }

        public bool IsIopsCompatibleDevice
        {
            get
            {
                if (VolumeType == null)
                    return false;

                return VolumeWrapper.ProvisionedIOPSTypeCode.Equals(VolumeType.TypeCode, StringComparison.OrdinalIgnoreCase);
            }
        }

        // simpler xaml binding than needing a 'bool inverter' converter
        public bool IsNonIopsCompatibleDevice
        {
            get
            {
                if (VolumeType == null)
                    return false;

                return !VolumeWrapper.ProvisionedIOPSTypeCode.Equals(VolumeType.TypeCode, StringComparison.OrdinalIgnoreCase);
            }
        }

        public string StaticIops
        {
            get
            {
                if (VolumeType == null)
                    return string.Empty;

                // rather than return sample values for non-provisionable types (as
                // the console does), return 'auto' so that when the service increases
                // the values we don't get out of date
                return VolumeType.TypeCode.Equals(VolumeWrapper.StandardTypeCode, StringComparison.Ordinal) 
                    ? "N/A" : "Auto";
            }
        }

        /// <summary>
        /// Whether instance store volume types are allowed, and if so how many, is instance type
        /// dependent. This routine sets up the selector based on type and how many instance store
        /// volumes have been requested so far.
        /// </summary>
        /// <param name="instanceTypeId"></param>
        /// <param name="instanceStoresAssigned"></param>
        public void SetAvailableStorageTypesForInstanceType(string instanceTypeId, int instanceStoresAssigned)
        {
            var instanceTypeMeta = InstanceType.FindById(instanceTypeId);
            if (instanceTypeMeta == null)
            {
                LOGGER.ErrorFormat("Unable to find InstanceType with id {0} in EC2ServiceMeta file. Assuming all volume types valid.", instanceTypeId);
                AvailableStorageTypes = AllStorageTypes;
                return;
            }

            var allowInstanceStore = instanceTypeMeta.MaxInstanceStoreVolumes > 0 && instanceStoresAssigned < instanceTypeMeta.MaxInstanceStoreVolumes;
            // if this is the root device, only ebs may be allowed for this volume
            if (allowInstanceStore && IsRootDevice && instanceTypeMeta.RequiresEbsVolume)
                allowInstanceStore = false;

            AvailableStorageTypes = allowInstanceStore ? AllStorageTypes : new[] { VolumeWrapper.EBSVolumeType };
            if (!allowInstanceStore)
                StorageType = VolumeWrapper.EBSVolumeType;
        }
    }
}
