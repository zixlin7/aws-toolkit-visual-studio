using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.EC2.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    class ReviewPageController : IAWSWizardPageController
    {
        ReviewPage _pageUI;

        #region IAWSWizardPageController Members

        public string PageID
        {
            get { return GetType().FullName; }
        }

        public IAWSWizard HostingWizard { get; set; }

        public string PageGroup
        {
            get { return AWSWizardConstants.DefaultPageGroup; }
        }

        public string PageTitle
        {
            get { return "Review"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Review your selections and click Launch to start the instance(s)."; }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            return _pageUI ?? (_pageUI = new ReviewPage(this));
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            _pageUI.ClearPanels();

            AddAMISelectionPanel();
            AddAMIOptionsPanel();
            AddStoragePanel();
            AddSecurityPanel(); // putting ahead of tags, as more important
            AddTagsPanel();

            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Launch");
            HostingWizard.RequestFinishEnablement(this);
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingBack)
            {
                HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Finish, "Finish");
                HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);
            }

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return true;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, false);
        }

        public bool AllowShortCircuit()
        {
            return true;
        }

        #endregion

        void AddAMISelectionPanel()
        {
            var iw = HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] as ImageWrapper;

            var sb = new StringBuilder();
            sb.AppendFormat("Launch {0}, {1} sized instance(s) of AMI '{2}' ({3})", 
                            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceCount],
                            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceType] as string,
                            iw.ImageId,
                            iw.DisplayName);

            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap, 
                Text = sb.ToString()
            };

            _pageUI.AddReviewPanel("Amazon Machine Image (AMI)", tb);
        }

        void AddAMIOptionsPanel()
        {
            var sb = new StringBuilder();

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone))
            {
                sb.AppendFormat("Place the new instances into availability zone '{0}'.",
                                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone] as string);
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet))
            {
                var subnet = HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] as SubnetWrapper;
                sb.AppendFormat("Place the new instance into vpc {0} using subnet {1}", subnet.VpcId, subnet.SubnetId);
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.AppendFormat("Use kernel ID '{0}'", HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID] as string);
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.AppendFormat("Use ram disk ID '{0}'", HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID] as string);
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_Monitoring))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                if ((bool)HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Monitoring])
                    sb.Append("Enable AWS Cloudwatch monitoring for the instances.");
                else
                    sb.Append("Do not enable AWS Cloudwatch monitoring for the instances.");
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData))
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                if ((bool)HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataIsFile])
                    sb.AppendFormat("Apply user data to the instances from file '{0}'.", HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData] as string);
                else
                    sb.AppendFormat("Apply user data '{0}' to the instances.", HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData] as string);
                var isEncoded = false;
                if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataEncoded))
                    isEncoded = (bool)HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataEncoded];

                sb.Append(isEncoded ? " User data is already base64 encoded." : " User data requires base64 encoding.");
            }

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_TerminationProtection))
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                if ((bool)HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_TerminationProtection])
                    sb.Append("Enable termination protection for the instances.");
                else
                    sb.Append("Do not enable termination protection for the instances.");
            }

            var shutdownBehaviour = HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_ShutdownBehavior] as string;
            if (sb.Length > 0)
                sb.AppendLine();
            sb.AppendFormat("When shutdown is initiated from inside the instances, {0} them.", shutdownBehaviour);


            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                var instanceProfile = HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile] as string;
                sb.AppendFormat("Use Instance Profile with ARN {0}.", instanceProfile);
            }

            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap, 
                Text = sb.ToString()
            };

            _pageUI.AddReviewPanel("AMI Options", tb);
        }

        void AddStoragePanel()
        {
            var volumes = HostingWizard[LaunchWizardProperties.StorageProperties.propkey_StorageVolumes] 
                as ICollection<InstanceLaunchStorageVolume>;
            if (volumes == null)
                return;

            var sb = new StringBuilder();
            foreach (var volume in volumes)
            {
                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendFormat("    Add {0} volume{1} {2}, type '{3}'{4}, size {5}GB{6}",
                                volume.StorageType,
                                volume.IsRootDevice ? " (root)" : string.Empty,
                                volume.Device,
                                volume.VolumeType,
                                volume.IsIopsCompatibleDevice 
                                    ? string.Format(" (Iops {0})", volume.Iops) : string.Empty,
                                volume.Size,
                                volume.Encrypted ? ", Encrypted" : string.Empty
                                );
                sb.Append(".");

                var sbInner = new StringBuilder();

                if (volume.Snapshot != null && volume.Snapshot.SnapshotId != SnapshotModel.NO_SNAPSHOT_ID)
                    sbInner.AppendFormat("Use snapshot '{0}'.", volume.Snapshot.SnapshotId);

                if (volume.DeleteOnTermination)
                {
                    if (sbInner.Length > 0)
                        sbInner.Append(" ");
                    sbInner.Append("Delete volume on termination.");
                }

                if (sbInner.Length > 0)
                {
                    sb.AppendLine();
                    sb.Append("        ");
                    sb.Append(sbInner);
                }
            }

            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap, 
                Text = sb.ToString()
            };

            _pageUI.AddReviewPanel("Storage", tb);
        }

        void AddSecurityPanel()
        {
            var sb = new StringBuilder();

            var keyPairName = HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_KeyPair] as string;
            if (string.IsNullOrEmpty(keyPairName))
                sb.Append("Do not use a key pair when launching instances.");
            else
            {
                var createNew = false;
                if (HostingWizard.IsPropertySet(LaunchWizardProperties.SecurityProperties.propkey_CreatePair))
                    createNew = (bool)HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_CreatePair];
                sb.AppendFormat(createNew ? "Create and use a new key pair named '{0}'." : "Use existing key pair '{0}'.",
                                keyPairName);
            }

            sb.AppendLine();

            if (HostingWizard.IsPropertySet(LaunchWizardProperties.SecurityProperties.propkey_Groups))
            {
                sb.Append("Use the following security groups to initialize the instance firewalls: ");
                var groups = HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_Groups] as ICollection<SecurityGroupWrapper>;
                for (var i = 0; i < groups.Count; i++)
                {
                    if (i > 0)
                        sb.AppendFormat(", {0}", groups.ElementAt(i).DisplayName);
                    else
                        sb.Append(groups.ElementAt(i).DisplayName);
                }
            }
            else
            {
                sb.Append("Create and use a new security group to initialize instance firewalls.");
                sb.AppendLine();
                sb.AppendFormat("Create the group with name '{0}'", HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupName] as string);
                string desc = null;
                if (HostingWizard.IsPropertySet(LaunchWizardProperties.SecurityProperties.propkey_GroupDescription))
                    desc = HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupDescription] as string;
                if (!string.IsNullOrEmpty(desc))
                    sb.AppendFormat(" and description '{0}'", desc);
                sb.Append(".");

                sb.AppendLine();
                sb.Append("Enable the following services/ports in the group:");
                sb.AppendLine();

                var perms = HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_GroupPermissions] as ICollection<IPPermissionWrapper>;
                for (int i = 0; i < perms.Count; i++)
                {
                    if (i > 0)
                        sb.AppendLine();

                    var pw = perms.ElementAt(i);
                    sb.AppendFormat("    {0}", pw.FormattedIPProtocol);
                    if (!string.IsNullOrEmpty(pw.Source))
                        sb.AppendFormat(" for source '{0}'", pw.Source);
                }
            }

            var tb = new TextBlock
            {
                TextWrapping = System.Windows.TextWrapping.Wrap, 
                Text = sb.ToString()
            };

            _pageUI.AddReviewPanel("Security", tb);
        }

        void AddTagsPanel()
        {
            var sb = new StringBuilder();
            if (HostingWizard.IsPropertySet(LaunchWizardProperties.UserTagProperties.propkey_UserTags))
            {
                // 'Name' is always there but may be empty
                var tags = HostingWizard[LaunchWizardProperties.UserTagProperties.propkey_UserTags] as ICollection<Tag>;
                if (tags.Count > 1 
                        || (tags.Count == 1 
                                    && tags.ElementAt<Tag>(0).Key == EC2Constants.TAG_NAME 
                                    && !string.IsNullOrEmpty(tags.ElementAt(0).Value)))
                {
                    sb.Append("Apply the following tags to the instances:");
                    sb.AppendLine();
                    for (var i = 0; i < tags.Count; i++)
                    {
                        if (i > 0)
                            sb.AppendLine();

                        string value = tags.ElementAt<Tag>(i).Value;
                        sb.AppendFormat("    Key '{0}', value '{1}'", 
                                        tags.ElementAt<Tag>(i).Key,
                                        string.IsNullOrEmpty(value) ? "(no value)" : value);
                    }
                }
            }

            if (sb.Length > 0)
            {
                var tb = new TextBlock
                {
                    TextWrapping = System.Windows.TextWrapping.Wrap, 
                    Text = sb.ToString()
                };

                _pageUI.AddReviewPanel("Instance Tags", tb);
            }
        }

    }

}
