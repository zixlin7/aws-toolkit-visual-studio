using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;

using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.EC2.LaunchWizard;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Model;

using Amazon.AWSToolkit.Util;
using Amazon.AWSToolkit.CommonUI;
using Amazon.EC2;
using Amazon.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.Controller
{
    public class LaunchController : BaseContextCommand
    {
        FeatureViewModel _rootModel;
        static ILog LOGGER = LogManager.GetLogger(typeof(LaunchController));

        public override ActionResults Execute(IViewModel model)
        {
            return Execute(model, null);
        }

        public ActionResults Execute(IViewModel model, Image image)
        {
            if (model is EC2RootViewModel)
            {
                var root = model as EC2RootViewModel;
                this._rootModel = model.FindSingleChild<EC2InstancesViewModel>(false);
            }
            else
            {
                this._rootModel = model as FeatureViewModel;
            }

            if (_rootModel == null)
                return new ActionResults().WithSuccess(false);

            // page groups in console: AMI, Instance Type, Instance Details, Storage, Tags, Security Group, Review
            var seedProperties = new Dictionary<string, object>
            {
                {CommonWizardProperties.AccountSelection.propkey_SelectedAccount, this._rootModel.AccountViewModel},
                {LaunchWizardProperties.Global.propkey_EC2RootModel, this._rootModel},
                {LaunchWizardProperties.Global.propkey_VpcOnly, EC2Utilities.CheckForVpcOnlyMode(this._rootModel.EC2Client) },
                {LaunchWizardProperties.AMIOptions.propkey_SeedAMI, image},
                {
                    CommonWizardProperties.AccountSelection.propkey_SelectedRegion,
                    RegionEndPointsManager.GetInstance().GetRegion(this._rootModel.RegionSystemName)
                }
            };

            IAWSWizard wizard = AWSWizardFactory.CreateStandardWizard("Amazon.AWSToolkit.EC2.View.LaunchEC2Instance", seedProperties);
            wizard.Title = "Launch new Amazon EC2 Instance";

            var defaultPages = new IAWSWizardPageController[]
            {
                new QuickLaunchPageController(),
                new AMIPageController(),
                new AMIOptionsPageController(),
                new StoragePageController(), 
                new InstanceTagsPageController(),
                new SecurityOptionsPageController(),
                new ReviewPageController()
            };

            wizard.RegisterPageControllers(defaultPages, 0);

            wizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Launch");
            if (wizard.Run())
            {
                var newIds = createInstance(wizard.CollectedProperties);
                return new ActionResults().WithParameter(EC2Constants.RESULTS_PARAMS_NEWIDS, newIds).WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);
        }

        string[] createInstance(Dictionary<string, object> properties)
        {
            var keyName = getValue<string>(properties, LaunchWizardProperties.SecurityProperties.propkey_KeyPair);
            if (getValue<bool>(properties, LaunchWizardProperties.SecurityProperties.propkey_CreatePair))
            {
                var keyRequest = new CreateKeyPairRequest() { KeyName = keyName };

                var keyResponse = this._rootModel.EC2Client.CreateKeyPair(keyRequest);

                KeyPairLocalStoreManager.Instance.SavePrivateKey(
                    this._rootModel.AccountViewModel, 
                    this._rootModel.RegionSystemName, 
                    keyName,
                    keyResponse.KeyPair.KeyMaterial);
            }

            var ami = properties[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] as ImageWrapper;
            var runRequest = new RunInstancesRequest()
            {
                ImageId = ami.ImageId,
                MinCount = getValue<int>(properties, LaunchWizardProperties.AMIOptions.propkey_InstanceCount),
                MaxCount = getValue<int>(properties, LaunchWizardProperties.AMIOptions.propkey_InstanceCount),
                InstanceType = getValue<string>(properties, LaunchWizardProperties.AMIOptions.propkey_InstanceType),
                ClientToken = "Instances" + DateTime.Now.Ticks.ToString()
            };

            if (!string.IsNullOrEmpty(keyName))
                runRequest.KeyName = keyName;

            if (properties.ContainsKey(LaunchWizardProperties.SecurityProperties.propkey_Groups))
            {
                var securityGroups
                    = properties[LaunchWizardProperties.SecurityProperties.propkey_Groups] as ICollection<SecurityGroupWrapper>;
                if (securityGroups.Count > 0)
                {
                    var groupIds = new string[securityGroups.Count];
                    var i = 0;
                    foreach (var wrapper in securityGroups)
                    {
                        groupIds[i] = wrapper.GroupId;
                        i++;
                    }
                    runRequest.SecurityGroupIds = groupIds.ToList();
                }
            }
            else
            {
                var newGroupID = CreateRequestedSecurityGroup(properties);
                if (!string.IsNullOrEmpty(newGroupID))
                    runRequest.SecurityGroupIds = new List<string>() { newGroupID };
                else
                {
                    LOGGER.Error("Failed to create new security group, abandoning launch.");
                    return new string[0];
                }
            }

            AddOptionalRunParameters(runRequest, ami, properties);
            AddStorageParameters(runRequest, ami, properties);

            ICollection<Tag> userTags = null;
            if (properties.ContainsKey(LaunchWizardProperties.UserTagProperties.propkey_UserTags))
                userTags = properties[LaunchWizardProperties.UserTagProperties.propkey_UserTags] as ICollection<Tag>;

            try
            {
                var response = this._rootModel.EC2Client.RunInstances(runRequest);

                var newIds = response.Reservation.Instances.Select(instance => instance.InstanceId).ToList();
                AddUserTags(newIds, userTags);

                return newIds.ToArray();
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Instance Launch Failure", e.Message);
            }

            return new string[0];
        }

        // returns the id of the newly created group
        string CreateRequestedSecurityGroup(Dictionary<string, object> properties)
        {
            var name = getValue<string>(properties, LaunchWizardProperties.SecurityProperties.propkey_GroupName);
            var description = getValue<string>(properties, LaunchWizardProperties.SecurityProperties.propkey_GroupDescription);

            var request = new CreateSecurityGroupRequest() { GroupName = name };
            if (!string.IsNullOrEmpty(description))
                request.Description = description;

            var subnet = getValue<SubnetWrapper>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet);
            if (subnet != null)
                request.VpcId = subnet.VpcId;

            var response = this._rootModel.EC2Client.CreateSecurityGroup(request);
            var groupID = string.Empty;
            if (response.GroupId != null)
            {
                groupID = response.GroupId;
                var permissions = properties[LaunchWizardProperties.SecurityProperties.propkey_GroupPermissions] as ICollection<IPPermissionWrapper>;
                var ingressRequest = new AuthorizeSecurityGroupIngressRequest() { GroupId = groupID };

                foreach (var permission in permissions)
                {
                    var ipPermSpec = new IpPermission()
                    {
                        IpProtocol = permission.UnderlyingProtocol.ToLower(),
                        FromPort = permission.FromPort,
                        ToPort = permission.ToPort
                    };
                    if (!string.IsNullOrEmpty(permission.Source))
                        ipPermSpec.Ipv4Ranges = new List<IpRange> { new IpRange { CidrIp = permission.Source } };

                    ingressRequest.IpPermissions.Add(ipPermSpec);
                }

                this._rootModel.EC2Client.AuthorizeSecurityGroupIngress(ingressRequest);
            }

            return groupID;
        }

        void AddOptionalRunParameters(RunInstancesRequest runRequest, ImageWrapper ami, Dictionary<string, object> properties)
        {
            var launchIntoVPC = properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC) 
                && getValue<bool>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC);

            if (launchIntoVPC)
            {
                var subnet = getValue<SubnetWrapper>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet);
                runRequest.SubnetId = subnet.SubnetId;
            }
            else
            {
                if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone))
                    runRequest.Placement = new Placement(){AvailabilityZone = getValue<string>(properties,LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone)};
            }

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData))
            {
                var userDataRef = getValue<string>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData);
                var isFile = getValue<bool>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataIsFile);
                var preEncoded = getValue<bool>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataEncoded);

                string userData;
                if (isFile)
                {
                    using (TextReader tr = new StreamReader(userDataRef))
                    {
                        userData = tr.ReadToEnd();
                    }
                }
                else
                    userData = userDataRef;

                runRequest.UserData = preEncoded ? userData : Convert.ToBase64String(Encoding.UTF8.GetBytes(userData.ToCharArray()));
            }

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID))
                runRequest.KernelId = getValue<string>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID);

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID))
                runRequest.RamdiskId = getValue<string>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID);

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_Monitoring))
                runRequest.Monitoring = getValue<bool>(properties,  LaunchWizardProperties.AdvancedAMIOptions.propkey_Monitoring);

            if (String.Compare(ami.RootDeviceType, "ebs", System.StringComparison.OrdinalIgnoreCase) == 0)
            {
                if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_ShutdownBehavior))
                    runRequest.InstanceInitiatedShutdownBehavior = getValue<string>(properties,
                                                                                  LaunchWizardProperties.AdvancedAMIOptions.propkey_ShutdownBehavior);
            }

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_TerminationProtection))
                runRequest.DisableApiTermination = getValue<bool>(properties,
                                                                    LaunchWizardProperties.AdvancedAMIOptions.propkey_TerminationProtection);

            if (properties.ContainsKey(LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile))
                runRequest.IamInstanceProfile = new IamInstanceProfileSpecification() { Arn = getValue<string>(properties, LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile) };
        }

        void AddStorageParameters(RunInstancesRequest runRequest, ImageWrapper ami, Dictionary<string, object> properties)
        {
            // Storage added in the advanced mode of the wizard has a lot more options. For quick launch,
            // we auto-assign the device name based on the image virtualization type, so handle these two
            // scenarios separately
            if (properties.ContainsKey(LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch) 
                && (bool)properties[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch])
            {
                if (properties.ContainsKey(LaunchWizardProperties.StorageProperties.propkey_QuickLaunchVolumeType))
                {
                    // Windows images seem to be HVM type but prefer the device name to be /dev/sda1, based on
                    // examination
                    runRequest.BlockDeviceMappings = new List<BlockDeviceMapping>
                    {
                        new BlockDeviceMapping
                        {
                            DeviceName = ami.IsWindowsPlatform 
                                            || ami.VirtualizationType.Equals(VirtualizationType.Paravirtual, 
                                                                             StringComparison.OrdinalIgnoreCase)
                                ? "/dev/sda1"
                                : "/dev/xvda",
                            Ebs = new EbsBlockDevice
                            {
                                VolumeType =
                                    properties[LaunchWizardProperties.StorageProperties.propkey_QuickLaunchVolumeType] as string,
                                VolumeSize =
                                    (int) properties[LaunchWizardProperties.StorageProperties.propkey_QuickLaunchVolumeSize]
                            }
                        }
                    };
                }

                return;
            }

            // the advanced mode of the wizard creates a collection of storage volumes to process
            if (!properties.ContainsKey(LaunchWizardProperties.StorageProperties.propkey_StorageVolumes)) 
                return;

            var storageVolumes = properties[LaunchWizardProperties.StorageProperties.propkey_StorageVolumes] as ICollection<InstanceLaunchStorageVolume>;
            if (storageVolumes == null)
            {
                LOGGER.ErrorFormat("Found wizard setting for storage volumes but null data; skipping storage config on launch");
                return;
            }
            
            var deviceMappings = new List<BlockDeviceMapping>();
            foreach (var sv in storageVolumes)
            {
                var ebs = new EbsBlockDevice
                {
                    VolumeSize = sv.Size,
                    DeleteOnTermination = sv.DeleteOnTermination,
                    VolumeType = sv.VolumeType.TypeCode
                };

                // even if we pass false, if a snapshot is involved ec2 will throw error
                if (sv.Encrypted) 
                    ebs.Encrypted = true;

                if (ebs.VolumeType == VolumeWrapper.ProvisionedIOPSTypeCode)
                    ebs.Iops = sv.Iops;

                // the console shows a pre-calc'd snapshot id for the root device but it appears
                // we can't do the same -- if we pass one, we get a 'snapshot doesn't exist'
                // error
                if (!sv.IsRootDevice && (sv.Snapshot != null && sv.Snapshot.SnapshotId != SnapshotModel.NO_SNAPSHOT_ID))
                    ebs.SnapshotId = sv.Snapshot.SnapshotId;

                var bdm = new BlockDeviceMapping
                {
                    DeviceName = sv.Device, 
                    Ebs = ebs
                };

                deviceMappings.Add(bdm);
            }

            runRequest.BlockDeviceMappings = deviceMappings;
        }

        void AddUserTags(List<string> instanceIds, ICollection<Tag> userTags)
        {
            if (userTags == null || userTags.Count == 0)
                return;

            CreateTagsRequest tagsRequest = new CreateTagsRequest() { Resources = instanceIds };
            Tag[] tags = new Tag[userTags.Count];
            userTags.CopyTo(tags, 0);
            tagsRequest.Tags = tags.ToList();

            // It is possible EC2 won't recognize the machine yet to be tag so we need to add some 
            // retry logic.
            var ec2Client = this._rootModel.EC2Client;
            for (int i = 0; i <= 3; i++)
            {
                Thread.Sleep(1000 * i);
                try
                {
                    ec2Client.CreateTags(tagsRequest);
                }
                catch (Exception e)
                {
                    LOGGER.Info("Error adding name tags, going to wait and try again.", e);
                }
            }
        }

        T getValue<T>(Dictionary<string, object> properties, string key)
        {
            object value;
            if (properties.TryGetValue(key, out value))
            {
                T convertedValue = (T)Convert.ChangeType(value, typeof(T));
                return convertedValue;
            }

            return default(T);
        }
    }
}
