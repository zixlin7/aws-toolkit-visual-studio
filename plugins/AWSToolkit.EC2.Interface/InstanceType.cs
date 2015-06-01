using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.EC2;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2
{
    public class InstanceType
    {
        /// <summary>
        /// The name of this instance type.
        /// </summary>
        public string Name
        {
            get;
            private set;
        }

        /// <summary>
        /// The EC2 ID for this instance type.
        /// </summary>
        public string Id
        {
            get;
            private set;
        }

        public string DisplayName
        {
            get { return string.Format("{0} ({1})", Name, Id); }    
        }

        /// <summary>
        /// The RAM (measured in Gigabytes) available on this instance type.
        /// </summary>
        public string MemoryWithUnits
        {
            get;
            private set;
        }

        /// <summary>
        /// The disk space available on this instance type.
        /// </summary>
        public string DiskSpaceWithUnits
        {
            get;
            private set;
        }

        /// <summary>
        /// The number of virtual cores available on this instance type.
        /// </summary>
        public int NumberOfVirtualCores
        {
            get;
            private set;
        }

        /// <summary>
        /// The architecture bits (32bit or 64bit) on this instance type.
        /// </summary>
        public string ArchitectureBits
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type supports 32-bit amis
        /// </summary>
        public bool Supports32Bit
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type supports 64-bit amis
        /// </summary>
        public bool Supports64Bit
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type requires an EBS-backed image
        /// </summary>
        public bool RequiresEbsVolume
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type requires images using hardware virtualization
        /// </summary>
        public bool RequiresHvmImage
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type must be launced into a VPC.
        /// </summary>
        public bool RequiresVPC
        {
            get;
            set;
        }

        /// <summary>
        /// The maximum number of instance store volumes that can be attached.
        /// </summary>
        public int MaxInstanceStoreVolumes
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether this instance type is current generation or not.
        /// </summary>
        public bool IsCurrentGeneration
        {
            get;
            private set;
        }

        /// <summary>
        /// The hardware family of this instance type.
        /// </summary>
        public string HardwareFamily
        {
            get;
            private set;
        }

        public InstanceType(string id, string name, string memoryInGigabytes,
                            string diskSpaceInGigabytes, int numberOfVirtualCores,
                            string architectureBits, bool requiresEbsVolume,
                            bool requiresHvmImage, int maxInstanceStoreVolumes,
                            bool currentGeneration, string hardwareFamily)
        {
            this.Id = id;
            this.Name = name;
            this.DiskSpaceWithUnits = diskSpaceInGigabytes;
            this.MemoryWithUnits = memoryInGigabytes;
            this.NumberOfVirtualCores = numberOfVirtualCores;
            this.ArchitectureBits = architectureBits;
            this.RequiresEbsVolume = requiresEbsVolume;
            this.RequiresHvmImage = requiresHvmImage;
            this.MaxInstanceStoreVolumes = maxInstanceStoreVolumes;
            this.IsCurrentGeneration = currentGeneration;
            this.HardwareFamily = hardwareFamily;

            string[] architectures = architectureBits.Split('/');
            foreach (string arch in architectures)
            {
                if (arch == "32")
                    this.Supports32Bit = true;
                else if (arch == "64")
                    this.Supports64Bit = true;
            }
        }

        public static IList<InstanceType> GetValidTypes(Image image)
        {
            return EC2ServiceMeta.GetValidTypes(image);
        }

        public static InstanceType FindById(string id)
        {
            return EC2ServiceMeta.FindById(id);
        }

        /// <summary>
        /// Returns whether a new instance of this type can legally be launched with the image given.
        /// </summary>    
        public bool CanLaunch(Image image)
        {

            if ( image == null )
                return false;

            int requiredArchitectureBits = 32;
            if (image.Architecture.Value.ToLower().Equals("x86_64"))
            {
                requiredArchitectureBits = 64;
            }

            if ( (requiredArchitectureBits == 64 && !Supports64Bit) || (requiredArchitectureBits == 32 && !Supports32Bit) )
                return false;
            if (RequiresEbsVolume && !image.RootDeviceType.Value.ToLower().Equals("ebs"))
                return false;
            if (RequiresHvmImage && image.VirtualizationType != VirtualizationType.Hvm)
                return false;

            return true;
        }
    }
}
