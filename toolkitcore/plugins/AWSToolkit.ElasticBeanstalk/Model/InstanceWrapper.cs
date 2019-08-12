using System;
using System.Collections.Generic;
using System.Text;
using Amazon.EC2.Model;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class InstanceWrapper
    {
        Amazon.EC2.Model.Instance _originalInstance;

        public InstanceWrapper(Amazon.EC2.Model.Instance originalInstance)
        {
            this._originalInstance = originalInstance;
        }

        public Amazon.EC2.Model.Instance NativeInstance => this._originalInstance;

        public string Id => this._originalInstance.InstanceId;

        public System.Windows.Media.ImageSource InstanceIcon
        {
            get
            {
                var iconPath = IsWindowsPlatform ? "instance-windows.gif" : "instance-generic.png";
                var icon = IconHelper.GetIcon(iconPath);
                return icon.Source;
            }
        }

        public string State => this._originalInstance.State.Name;

        public string KeyName => this._originalInstance.KeyName;

        public string AMI => this._originalInstance.ImageId;

        public string Platform => this._originalInstance.Platform;

        // ec2 api shows 'windows' as platform label but have seen 'Windows' used
        public bool IsWindowsPlatform => EC2Constants.PLATFORM_WINDOWS.Equals(this.NativeInstance.Platform, StringComparison.OrdinalIgnoreCase);

        public string Architecture => this._originalInstance.Architecture;

        public string Type => this._originalInstance.InstanceType;

        public string SecurityGroups
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (var group in this._originalInstance.SecurityGroups)
                {
                    if (string.IsNullOrEmpty(group.GroupName))
                        continue;

                    if (sb.Length != 0)
                        sb.Append(", ");
                    sb.Append(group.GroupName);
                }
                return sb.ToString(); 
            }
        }

        public string AvailabilityZone => this._originalInstance.Placement.AvailabilityZone;


        //Properties below here currently not displayed
        public int AMILaunchIndex => this._originalInstance.AmiLaunchIndex;

        public string ProductCodes
        {
            get 
            {
                StringBuilder sb = new StringBuilder();
                foreach (ProductCode pc in this._originalInstance.ProductCodes)
                {
                    if (sb.Length > 0)
                        sb.AppendFormat(", {0}", pc.ProductCodeId);
                    else
                        sb.Append(pc.ProductCodeId);
                }

                return sb.ToString();
            }
        }
        public string GroupName => this._originalInstance.Placement.GroupName;

        public string Tenancy => this._originalInstance.Placement.Tenancy;

        public string Monitoring => this._originalInstance.Monitoring.State;

        public string Subnet => this._originalInstance.SubnetId;

        public string VPC => this._originalInstance.VpcId;

        public string SourceDestCheck => this._originalInstance.SourceDestCheck.ToString();

        public string SecurityGroupsIds
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach (var group in this._originalInstance.SecurityGroups)
                {
                    if (string.IsNullOrEmpty(group.GroupId))
                        continue;

                    if (sb.Length != 0)
                        sb.Append(", ");
                    sb.Append(group.GroupId);
                }
                return sb.ToString();
            }
        }
        public string RootDeviceType => this._originalInstance.RootDeviceType;

        public string RootDeviceName => this._originalInstance.RootDeviceName;

        public string BlockDeviceMapping
        {
            get
            {
                List<string> mappings = new List<string>();
                foreach (InstanceBlockDeviceMapping mapping in this._originalInstance.BlockDeviceMappings)
                {
                    mappings.Add(String.Format("{0}:{1} with status {2} at {3}",mapping.DeviceName, mapping.Ebs.VolumeId, mapping.Ebs.Status, mapping.Ebs.AttachTime));
                }
                return String.Join("\n", mappings.ToArray());
            }
        }
        public string Lifecycle => this._originalInstance.InstanceLifecycle;

        public string SpotInstanceRequest => this._originalInstance.SpotInstanceRequestId;

        public string VirtualaizationType => this._originalInstance.VirtualizationType;

        public string ClientToken => this._originalInstance.ClientToken;

        public string Tags
        {
            get 
            {
                List<string> tags = new List<string>();
                foreach (var t in this._originalInstance.Tags)
                {
                    tags.Add(String.Format("{0}::{1}",t.Key,t.Value));
                }
                return String.Join(", ", tags.ToArray()); 
            }
        }
        public DateTime Launched => this._originalInstance.LaunchTime;

        public string Kernal => this._originalInstance.KernelId;

        public string RamDisk => this._originalInstance.RamdiskId;

        public string PrivateDNS => this._originalInstance.PrivateDnsName;

        public string PrivateIp => this._originalInstance.PrivateIpAddress;

        public string PublicDNS => this._originalInstance.PublicDnsName;

        public string PublicIp => this._originalInstance.PublicIpAddress;
    }
}
