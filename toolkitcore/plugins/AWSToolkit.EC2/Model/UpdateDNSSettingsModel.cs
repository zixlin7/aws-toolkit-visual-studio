﻿using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class UpdateDNSSettingsModel : BaseModel
    {
        public string VpcId { get; set; }

        public bool EnableDnsSupport { get; set; }

        public bool EnableDnsHostnames { get; set; }
    }
}
