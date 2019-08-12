using System.Collections.Generic;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2;

namespace Amazon.AWSToolkit.EC2.View.DesignData
{
    public class QuickLaunchDesignTimeData : List<EC2QuickLaunchImage>
    {
        public QuickLaunchDesignTimeData()
        {
            Add(new EC2QuickLaunchImage
            {
                Title = "Windows Server 2012",
                Description = "A Windows Server",
                FreeTier = false,
                Is32BitSelected = false,
                Is64BitSelected = true,
                Platform = "Windows",
            });
            Add(new EC2QuickLaunchImage
            {
                Title = "Linux",
                Description = "A Linux Server",
                FreeTier = true,
                Is32BitSelected = true,
                Is64BitSelected = false,
                Platform = "Linux",
                VirtualizationType = VirtualizationType.Hvm
            });
        }
    }

    public class StorageDesignTimeData : List<InstanceLaunchStorageVolume>
    {
        public StorageDesignTimeData()
        {
            var device1 = new InstanceLaunchStorageVolume
            {
                StorageType = "Root",
                Device = "/dev/sda1",
                Size = 8,
                DeleteOnTermination = true,
                VolumeType = InstanceLaunchStorageVolume.VolumeTypeFromCode(VolumeWrapper.GeneralPurposeTypeCode)
            };

            var device2 = new InstanceLaunchStorageVolume
            {
                StorageType = "EBS",
                Device = "/dev/sdb",
                Size = 8,
                Iops = 2000,
                DeleteOnTermination = false,
                VolumeType = InstanceLaunchStorageVolume.VolumeTypeFromCode(VolumeWrapper.ProvisionedIOPSTypeCode)
            };

            this.Add(device1);
            this.Add(device2);
        }

        public ICollection<InstanceLaunchStorageVolume> StorageVolumes => this;

        public InstanceLaunchStorageVolume SelectedVolume { get; set; }
    }
}
