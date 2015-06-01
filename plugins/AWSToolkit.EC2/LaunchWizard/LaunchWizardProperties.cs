using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.EC2.LaunchWizard
{
    public static class LaunchWizardProperties
    {
        public static class Global
        {
            /// <summary>
            /// Instance of an ec2 root view model bound to the current endpoint context in the
            /// navigator/toolkit window from which the wizard was launched
            /// </summary>
            public static readonly string propkey_EC2RootModel = "ec2RootModel";

            /// <summary>
            /// Optional, bool. Set true if we cannot load the toolkit's quicklaunch file or the
            /// set of amis it contains is not 100% valid
            /// </summary>
            public static readonly string propkey_QuickLaunchUnavailable = "quickLaunchUnavailable";

            /// <summary>
            /// Optional, ICollection<Amazon.IdentityManagement.Model.InstanceProfile>. Set by
            /// any page that needs to query these, to save following pages from needing to requery.
            /// </summary>
            public static readonly string propkey_CachedIAMInstanceProfiles = "cachedIAMProfiles";

            /// <summary>
            /// Set true if the user, or the current region, mandates use of a vpc
            /// </summary>
            public static readonly string propkey_VpcOnly = "vpcOnly";
        }

        public static class AMIOptions
        {
            /// <summary>
            /// Boolean; indicates if the user elect to do a one-page 'quick launch'
            /// propkey_SelectedAMI, propkey_InstanceType, propkey_InstanceCount (1) and
            /// propkey_InstanceName set. If not quick launch, all properties are
            /// potentially set.
            /// </summary>
            public static readonly string propkey_IsQuickLaunch = "isQuickLaunch";

            /// <summary>
            /// Amazon.EC2.Model.Image instance, optional, sets the ami to be pre-selected 
            /// in the wizard's quick launch page. The user will not be able to select any
            /// other ami if this property is set.
            /// </summary>
            public static readonly string propkey_SeedAMI = "seed-ami";

            /// <summary>
            /// ImageWrapper instance for the selected AMI
            /// </summary>
            public static readonly string propkey_SelectedAMI = "selected-ami";

            /// <summary>
            /// String, 'id' of the instance type to use (t1.micro etc)
            /// </summary>
            public static readonly string propkey_InstanceType = "instance-type";

            /// <summary>
            /// Int, the number of instances to launch
            /// </summary>
            public static readonly string propkey_InstanceCount = "instance-count";
        }

        public static class AdvancedAMIOptions
        {
            /// <summary>
            /// String, optional. Selected availability zone.
            /// </summary>
            public static readonly string propkey_AvailabilityZone = "availabilityZone";

            /// <summary>
            /// String, optional. Selected subnet when using VPC
            /// </summary>
            public static readonly string propkey_Subnet = "subnet";

            /// <summary>
            /// String, optional. launch the new instance into a VPC
            /// </summary>
            public static readonly string propkey_LaunchIntoVPC = "launchIntoVPC";

            /// <summary>
            /// String, optional.
            /// </summary>
            public static readonly string propkey_KernelID = "kernelID";

            /// <summary>
            /// String, optional.
            /// </summary>
            public static readonly string propkey_RamDiskID = "ramDiskID";

            /// <summary>
            /// Boolean, optional. If true then enable Cloudwatch monitoring.
            /// </summary>
            public static readonly string propkey_Monitoring = "monitoring";

            /// <summary>
            /// String, optional.
            /// </summary>
            public static readonly string propkey_UserData = "userData";

            /// <summary>
            /// Boolean, true if propkey_UserData refers to a file containing the data
            /// </summary>
            public static readonly string propkey_UserDataIsFile = "userDataIsFile";

            /// <summary>
            /// Boolean, optional. If true then the contents of propkey_UserData are
            /// base64 encoded.
            /// </summary>
            public static readonly string propkey_UserDataEncoded = "userDataEncoded";

            /// <summary>
            /// Boolean, optional
            /// </summary>
            public static readonly string propkey_TerminationProtection = "terminationProtection";

            /// <summary>
            /// String. One of 'stop' or 'terminate'.
            /// </summary>
            public static readonly string propkey_ShutdownBehavior = "shutdownBehavior";

            /// <summary>
            /// String. Arn for the Instance Profile. Optional.
            /// </summary>
            public static readonly string propkey_InstanceProfile = "instanceProfile";
        }

        public static class SecurityProperties
        {
            /// <summary>
            /// String, name of the key pair to use or create during deployment.
            /// Empty string if the user did not specify a key name.
            /// </summary>
            public static readonly string propkey_KeyPair = "key-pair";

            /// <summary>
            /// Boolean, if set create a keypair with the name held by propkey_KeyPair
            /// otherwise assume a keypair with that name already exists.
            /// Only present if propkey_KeyPair does not evaluate to an empty string.
            /// </summary>
            public static readonly string propkey_CreatePair = "create-key-pair";

            /// <summary>
            /// ICollection-compatible set of on or more SecurityGroupWrapper instances around 
            /// the security group(s) to control the firewall to the instances. Not set if 
            /// creating a group.
            /// </summary>
            public static readonly string propkey_Groups = "groups";

            /// <summary>
            /// String, optionally set when user elects to create a group.
            /// </summary>
            public static readonly string propkey_GroupName = "group-name";

            /// <summary>
            /// String, optionally set if propkey_CreateGroup true
            /// </summary>
            public static readonly string propkey_GroupDescription = "group-desc";

            /// <summary>
            /// Optional, set only on group creation.
            /// ICollection-compatible set if IPPermissionWrapper instances describing the
            /// ports/services to be opened
            /// </summary>
            public static readonly string propkey_GroupPermissions = "group-permissions";
        }

        public static class UserTagProperties
        {
            /// <summary>
            /// Optional, ICollection-compatible collection of Amazon.EC2.Model.Tag instances
            /// </summary>
            public static readonly string propkey_UserTags = "user-tags";
        }

        public static class StorageProperties
        {
            /// <summary>
            /// The EBS volume type code (when launching from quick launch only)
            /// </summary>
            public static readonly string propkey_QuickLaunchVolumeType = "volume-type";

            /// <summary>
            /// The requested size, in GB, of the volume (when launching from quick launch only)
            /// </summary>
            public static readonly string propkey_QuickLaunchVolumeSize = "volume-size";

            /// <summary>
            /// Collection of InstanceLaunchStorageVolume instances describing the storage
            /// to be attached to the new instance (advanced launch only)
            /// </summary>
            public static readonly string propkey_StorageVolumes = "storage-volumes";
        }
    }
}
