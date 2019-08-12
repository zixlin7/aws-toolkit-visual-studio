using System;
using System.Collections.Generic;
using Amazon.AutoScaling.Model;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Model
{
    public class LaunchConfigurationWrapper
    {
        Amazon.AutoScaling.Model.LaunchConfiguration  _originalLaunchConfiguration;

        public LaunchConfigurationWrapper(Amazon.AutoScaling.Model.LaunchConfiguration originalLaunchConfiguration)
        {
            this._originalLaunchConfiguration = originalLaunchConfiguration;
        }

        public string Name => this._originalLaunchConfiguration.LaunchConfigurationName;

        public string AMI => this._originalLaunchConfiguration.ImageId;

        public string KeyName => this._originalLaunchConfiguration.KeyName;

        public string InstanceType => this._originalLaunchConfiguration.InstanceType;

        public string SecurityGroups => String.Join(", ", this._originalLaunchConfiguration.SecurityGroups.ToArray());

        public string Kernel => this._originalLaunchConfiguration.KernelId;

        public string RamDisk => this._originalLaunchConfiguration.RamdiskId;

        public string ARN => this._originalLaunchConfiguration.LaunchConfigurationARN;

        public string CreatedTime => this._originalLaunchConfiguration.CreatedTime.ToString();

        public string InstanceMonitoring => this._originalLaunchConfiguration.InstanceMonitoring.Enabled.ToString();


        //Properties below here are not shown.
        public string BlockDeviceMappings
        {
            get
            {
                List<string> blockDeviceMappings = new List<string>();
                foreach (BlockDeviceMapping mapping in this._originalLaunchConfiguration.BlockDeviceMappings)
                {
                    blockDeviceMappings.Add(String.Format("Virtual Name: {0}, Device Name: {1}, {2}",
                        mapping.VirtualName,
                        mapping.DeviceName,
                        String.Format("Snapshot ID: {0}, Size: {1}",
                            mapping.Ebs.SnapshotId,
                            mapping.Ebs.VolumeSize.ToString())));
                }
                return String.Join("\n", blockDeviceMappings.ToArray());
            }
        }
        public string UserData => this._originalLaunchConfiguration.UserData;
    }
}
