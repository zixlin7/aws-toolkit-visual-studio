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
            if (MinimumSize.HasValue)
                return MinimumSize.Value;

            if (platform.Equals("windows", StringComparison.OrdinalIgnoreCase))
                return EC2ServiceMeta.Instance.DefaultWindowsRootVolumeSize;

            return EC2ServiceMeta.Instance.DefaultLinuxRootVolumeSize;
        }
    }

    public static class EBSVolumeTypes
    {
        public static EBSVolumeType[] AllVolumeTypes =
        {
            new EBSVolumeType { Description = "General Purpose (SSD)", TypeCode = "gp2" },
            new EBSVolumeType { Description = "ProvisionedIOPS (SSD)", TypeCode = "io1", IsPiopsVolume = true },
            new EBSVolumeType { Description = "Magnetic", TypeCode = "standard" },
        };
    }
}
