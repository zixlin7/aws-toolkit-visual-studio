using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.Model
{
    public class WindowsDeviceMapping
    {
        static WindowsDeviceMapping[] _mappings;

        static WindowsDeviceMapping()
        {
            _mappings = new WindowsDeviceMapping[]
            {
                new WindowsDeviceMapping(@"xvdf", @"H:\"),
                new WindowsDeviceMapping(@"xvdg", @"I:\"),
                new WindowsDeviceMapping(@"xvdh", @"J:\"),
                new WindowsDeviceMapping(@"xvdi", @"K:\"),
                new WindowsDeviceMapping(@"xvdj", @"L:\"),
                new WindowsDeviceMapping(@"xvdk", @"M:\"),
                new WindowsDeviceMapping(@"xvdl", @"N:\"),
                new WindowsDeviceMapping(@"xvdm", @"O:\"),
                new WindowsDeviceMapping(@"xvdn", @"P:\"),
                new WindowsDeviceMapping(@"xvdo", @"Q:\"),
                new WindowsDeviceMapping(@"xvdp", @"R:\")
            };
        }

        public static WindowsDeviceMapping[] Mappings
        {
            get { return _mappings; }
        }


        private WindowsDeviceMapping(string ec2Name, string displayName)
        {
            this.EC2Name = ec2Name;
            this.DisplayName = displayName;
        }

        public string EC2Name
        {
            get;
            private set;
        }

        public string DisplayName
        {
            get;
            private set;
        }
    }
}
