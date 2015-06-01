using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2
{
    public class EBSVolumeType
    {
        public string Description { get; internal set; }
        public string TypeCode { get; internal set; }
        public bool IsPiopsVolume { get; internal set; }
        public int? MinimumSize { get; internal set; }

        public int MinimumSizeForPlatform(string platform)
        {
            if (platform.Equals("windows", StringComparison.OrdinalIgnoreCase))
                return EBSVolumeTypes.DefaultWindowsPlatformSize;

            return MinimumSize.HasValue ? MinimumSize.Value : EBSVolumeTypes.DefaultLinuxVolumeSize;
        }
    }

    public static class EBSVolumeTypes
    {
        public static EBSVolumeType[] AllVolumeTypes =
        {
            new EBSVolumeType { Description = "General Purpose (SSD)", TypeCode = "gp2" },
            new EBSVolumeType { Description = "ProvisionedIOPS (SSD)", TypeCode = "io1", MinimumSize = 10, IsPiopsVolume = true },
            new EBSVolumeType { Description = "Magnetic", TypeCode = "standard" },
        };

        // these seem to be the bare minimums based on using the web console, don't think you can
        // query these values
        public static int DefaultLinuxVolumeSize = 8;
        public static int DefaultWindowsPlatformSize = 30;
    }
}
