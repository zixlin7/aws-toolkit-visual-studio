using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Threading;

using Amazon.AWSToolkit.Account;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;

using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageWorkers;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.EC2.Utils;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Workers;

using Amazon.AWSToolkit.SimpleWorkers;

using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.EC2.Util;

using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    class AMIOptionsPageController : IAWSWizardPageController
    {
        private readonly object _syncLock = new object();
        private int _backgroundWorkersActive = 0;
        private int BackgroundWorkerCount
        {
            get
            {
                int count;
                lock (_syncLock)
                {
                    count = _backgroundWorkersActive;
                }
                return count;
            }
        }

        private AMIOptionsPage _pageUI;
        private string _previouslySelectedAmiId;

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
            get { return "AMI Options"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Additional options for controlling your AMI instance."; }
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
            if (_pageUI == null)
            {
                _pageUI = new AMIOptionsPage(this);
                PopulateZones((HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client);
                PopulateVpcSubnets((HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                // re-initialize if user selected a different ami
                var selectedAmi = HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] as ImageWrapper;
                if (!selectedAmi.ImageId.Equals(_previouslySelectedAmiId, StringComparison.OrdinalIgnoreCase))
                {
                    _previouslySelectedAmiId = selectedAmi.ImageId;
                    var ec2Client = (HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client;

                    _pageUI.SetInstanceTypes(InstanceType.GetValidTypes(selectedAmi.NativeImage));

                    PopulateKernelIDs(ec2Client, selectedAmi);
                    PopulateRamDiskIDs(ec2Client, selectedAmi);
                }

                PopulateIamProfiles();
            }

            // can get here direct from quick launch page if a seed ami was set, so do the same
            // as the ami selector page and turn on the wizard buttons that quick launch turned off
            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, true);
            // may have gotten here direct from quick launch, so make sure button text is correct
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Next");
            TestForwardTransitionEnablement();
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (navigationReason != AWSWizardConstants.NavigationReason.movingBack)
                StorePageData();
            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            var fwdsOK = IsForwardsNavigationAllowed;
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, fwdsOK);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, fwdsOK);
        }

        public bool AllowShortCircuit()
        {
            // user may have gone forwards enough for Finish to be enabled, then come back
            // and changed something so re-save
            StorePageData();
            return true;
        }

        #endregion

        void StorePageData()
        {
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceType] = _pageUI.SelectedInstanceTypeID;
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceCount] = _pageUI.InstanceCount;

            if (!this._pageUI.LaunchIntoVPC && !string.IsNullOrEmpty(_pageUI.SelectedZone))
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone] = _pageUI.SelectedZone;
            else
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_AvailabilityZone] = null;

            var subnet = _pageUI.SelectedSubnet;
            // if this isn't the 'no preference' option used for vpc-only environments, record that
            // we are manually launching into a vpc so the correct subnet id gets passed to RunInstances
            if (this._pageUI.LaunchIntoVPC && subnet != null && subnet.NativeSubnet != null)
            {
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC] = true;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] = _pageUI.SelectedSubnet;
            }
            else
            {
                // if we're in a vpc only env, the service will auto-select a default subnet to use if need be
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC] = false;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] = null;
            }

            if (!string.IsNullOrEmpty(_pageUI.SelectedKernelID))
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID] = _pageUI.SelectedKernelID;
            else
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_KernelID] = null;

            if (!string.IsNullOrEmpty(_pageUI.SelectedRamDiskID))
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID] = _pageUI.SelectedRamDiskID;
            else
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_RamDiskID] = null;

            HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Monitoring] = _pageUI.EnableMonitoring;

            if (!string.IsNullOrEmpty(_pageUI.UserData))
            {
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData] = _pageUI.UserData;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataIsFile] = _pageUI.UserDataIsFile;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserDataEncoded] = _pageUI.UserDataEncoded;
            }
            else
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_UserData] = null;

            HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_TerminationProtection] = _pageUI.PreventTermination;
            HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_ShutdownBehavior] = _pageUI.ShutdownBehavior;

            if (_pageUI.SelectedIamProfile != null)
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile] = _pageUI.SelectedIamProfile.Arn;
            else
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile] = null;
        }

        bool IsForwardsNavigationAllowed
        {
            get
            {
                if (_pageUI != null)
                    return BackgroundWorkerCount == 0 
                                && !string.IsNullOrEmpty(_pageUI.SelectedInstanceTypeID) 
                                && !_pageUI.HasValidationErrors;

                // tweak to allow quick-launch page to enable Launch on its own authority; if the user has
                // gone beyond quick-launch, we have mandatory fields
                if (HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch))
                    return (bool)HostingWizard[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch];

                return false;
            }
        }

        void PopulateZones(IAmazonEC2 ec2Client)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryAvailabilityZonesWorker(ec2Client,
                                             HostingWizard.Logger,
                                             new QueryAvailabilityZonesWorker.DataAvailableCallback(OnZonesAvailable));
        }

        void OnZonesAvailable(IEnumerable<AvailabilityZone> zones)
        {
            _pageUI.Zones = zones;
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void PopulateVpcSubnets(IAmazonEC2 ec2Client)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVpcsAndSubnetsWorker(ec2Client, HostingWizard.Logger, new QueryVpcsAndSubnetsWorker.DataAvailableCallback(OnVpcSubnetsAvailable));
        }

        void OnVpcSubnetsAvailable(ICollection<Vpc> vpcs, ICollection<Subnet> subnets)
        {
            var isVpcOnlyEnvironment = false;
            if (HostingWizard.IsPropertySet(LaunchWizardProperties.Global.propkey_VpcOnly))
                isVpcOnlyEnvironment = (bool)HostingWizard[LaunchWizardProperties.Global.propkey_VpcOnly];

            _pageUI.SetVpcSubnets(vpcs, subnets, isVpcOnlyEnvironment);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void PopulateKernelIDs(IAmazonEC2 ec2Client, ImageWrapper selectedAmi)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryKernelIDsWorker(ec2Client,
                                     selectedAmi.ImageId,
                                     HostingWizard.Logger,
                                     new QueryKernelIDsWorker.DataAvailableCallback(OnKernelIDsAvailable));
        }

        void OnKernelIDsAvailable(ICollection<string> kernelIDs)
        {
            _pageUI.KernelIDs = kernelIDs;
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void PopulateRamDiskIDs(IAmazonEC2 ec2Client, ImageWrapper selectedAmi)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryRamDiskIDsWorker(ec2Client,
                                      selectedAmi.ImageId,
                                      HostingWizard.Logger,
                                      new QueryRamDiskIDsWorker.DataAvailableCallback(OnRamDiskIDsAvailable));
        }

        void OnRamDiskIDsAvailable(ICollection<string> ramDiskIDs)
        {
            _pageUI.RamDiskIDs = ramDiskIDs;
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void PopulateIamProfiles()
        {
            // possibly cached by earlier page
            if (HostingWizard.IsPropertySet(LaunchWizardProperties.Global.propkey_CachedIAMInstanceProfiles))
            {
                var profiles = HostingWizard[LaunchWizardProperties.Global.propkey_CachedIAMInstanceProfiles]
                                    as ICollection<InstanceProfile>;
                if (profiles != null)
                {
                    _pageUI.IamProfiles = profiles;
                    return;
                }
            }

            new QueryInstanceProfilesWorker(
                HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel,
                HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints,
                HostingWizard.Logger,
                new QueryInstanceProfilesWorker.DataAvailableCallback(OnIamProfilesAvailable));
        }

        void OnIamProfilesAvailable(ICollection<InstanceProfile> profiles)
        {
            _pageUI.IamProfiles = profiles;
            // cache so return to the page can re-use
            HostingWizard[LaunchWizardProperties.Global.propkey_CachedIAMInstanceProfiles] = profiles;
        }
    }

}
