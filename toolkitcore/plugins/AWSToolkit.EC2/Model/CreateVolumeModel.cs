using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.ResourceTags;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class CreateVolumeModel : BaseModel
    {
        public class VolumeTypeOption
        {
            public string TypeCode { get; set; }
            public string TypeName { get; set; }

            public override string ToString()
            {
                return string.Format("{0} [{1}]", TypeName, TypeCode);
            }
        }

        public VolumeTypeOption[] _volumeTypeOptions =
        {
            new VolumeTypeOption
            {
                TypeCode = VolumeWrapper.GeneralPurposeTypeCode, 
                TypeName = VolumeWrapper.GeneralPurposeTypeDisplayName
            },
            new VolumeTypeOption
            {
                TypeCode = VolumeWrapper.ProvisionedIOPSTypeCode, 
                TypeName = VolumeWrapper.ProvisionedIOPSTypeDisplayName
            },
            new VolumeTypeOption
            {
                TypeCode = VolumeWrapper.StandardTypeCode, 
                TypeName = VolumeWrapper.StandardTypeDisplayName
            },
        };

        public VolumeTypeOption[] VolumeTypes
        {
            get => _volumeTypeOptions;
            set
            {
                _volumeTypeOptions = value;
                base.NotifyPropertyChanged("VolumeTypes");
            }
        }

        ResourceTagsModel _tags = new ResourceTagsModel();
        public ResourceTagsModel TagsModel
        {
            get => _tags;
            set
            {
                _tags = value;
                base.NotifyPropertyChanged("TagsModel");
            }
        }

        //public string Name
        //{
        //    get { return TagsModel.NameTag; }
        //    set
        //    {
        //        TagsModel.NameTag = value;
        //        //base.NotifyPropertyChanged(ResourceTagsModel.NameKey);
        //    }
        //}

        int _size = 1;
        public int Size
        {
            get => _size;
            set 
            {
                _size = value;
                base.NotifyPropertyChanged("Size");
            }
        }

        string _availabilityZone;
        public string AvailabilityZone
        {
            get => _availabilityZone;
            set 
            { 
                _availabilityZone = value;
                base.NotifyPropertyChanged("AvailabilityZone");
            }
        }

        string _snapshotId;
        public string SnapshotId
        {
            get => _snapshotId;
            set
            {
                _snapshotId = value;
                base.NotifyPropertyChanged("SnapshotId");
            }
        }

        string _volumeId;
        public string VolumeId
        {
            get => _volumeId;
            set
            {
                _volumeId = value;
                base.NotifyPropertyChanged("VolumeId");
            }
        }

        IList<SnapshotModel> _snapshots;
        IDictionary<string, SnapshotModel> _snapshotsById;
        public IList<SnapshotModel> AvailableSnapshots
        {
            get => _snapshots;
            set
            {
                _snapshots = value;
                _snapshotsById = new Dictionary<string, SnapshotModel>();
                foreach (var snap in _snapshots)
                {
                    if(snap.SnapshotId != null)
                        _snapshotsById.Add(snap.SnapshotId, snap);
                }
                base.NotifyPropertyChanged("AvailableSnapshots");
            }
        }

        public string SizeOfSnapshot(string snapshotId)
        {
            if (_snapshotsById != null && _snapshotsById.ContainsKey(snapshotId))
            {
                return _snapshotsById[snapshotId].Size;
            }
            return "0";
        }

        IList<string> _zones;
        public IList<string> AvailabilityZoneList
        {
            get => _zones;
            set
            {
                _zones = value;
                base.NotifyPropertyChanged("AvailabilityZoneList");
            }
        }

        RunningInstanceWrapper _instanceToAttach;
        public RunningInstanceWrapper InstanceToAttach
        {
            get => this._instanceToAttach;
            set
            {
                this._instanceToAttach = value;
                base.NotifyPropertyChanged("InstanceToAttach");
            }
        }

        string _device;
        public String Device
        {
            get => this._device;
            set
            {
                this._device = value;
                base.NotifyPropertyChanged("Device");
            }
        }

        int _iops = 100;
        public int Iops
        {
            get => this._iops;
            set
            {
                this._iops = value; 
                base.NotifyPropertyChanged("Iops");
            }
        }

        VolumeTypeOption _volumeType;
        public VolumeTypeOption VolumeType
        {
            get => this._volumeType;
            set
            {
                _volumeType = value;
                base.NotifyPropertyChanged("VolumeType");
            }
        }
    }
}
