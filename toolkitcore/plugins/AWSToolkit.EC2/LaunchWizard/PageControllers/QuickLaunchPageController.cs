using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows.Controls;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.AWSToolkit.Account;

using Amazon.EC2;
using Amazon.EC2.Model;
using AMIImage = Amazon.EC2.Model.Image;
using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.SimpleWorkers;

using log4net;
using ThirdParty.Json.LitJson;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageWorkers;
using Amazon.AWSToolkit.EC2.Workers;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    /// <summary>
    /// Landing page for AMI launch; either a pre-selected ami or one or more
    /// 'quick launch' amis.
    /// </summary>
    class QuickLaunchPageController : IAWSWizardPageController
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

        QuickLaunchPage _pageUI;
        readonly Dictionary<string, List<EC2QuickLaunchImage>> _quickLaunchByRegion = new Dictionary<string, List<EC2QuickLaunchImage>>();
        readonly Dictionary<string, ImageWrapper> _imageWrappers = new Dictionary<string, ImageWrapper>();
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(QuickLaunchPageController));
        bool? _quickLaunchVerificationResult = null;

        // set true if user or active region requires use of a vpc
        public bool IsVpcOnlyEnvironment { get; set; }

        // set true based on selected image
        public bool SelectedImageRequiresVpc { get; set; }

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
            get 
            {
                return HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI) 
                    ? "Launch AMI" : "Quick Launch";
            }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get 
            {
                return HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI) 
                    ? "Select the instance type and other options to launch one instance of the selected AMI." : "Select the Amazon Machine Image (AMI), instance type and other options to launch a single instance.";
            }
        }

        public void ResetPage()
        {

        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            // If we're seeded to an ami, ok to show page. If we're in general quick-launch mode,
            // we can only show the page if we can get the quick-launch file and the number of amis
            // it holds for the current region matches what is actually available (to get around EC2
            // changing the file and the toolkit version of the file falling out of date)
            if (HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI))
            {
                if ((bool)HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI))
                    return true;
            }

            // already checked and declared unusable?
            if (HostingWizard.IsPropertySet(LaunchWizardProperties.Global.propkey_QuickLaunchUnavailable))
                return !(bool)HostingWizard[LaunchWizardProperties.Global.propkey_QuickLaunchUnavailable];

            if (_quickLaunchVerificationResult == null)
                _quickLaunchVerificationResult = VerifyQuickLaunchAMIsAvailable();

            return _quickLaunchVerificationResult.GetValueOrDefault();
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
            {
                _pageUI = new QuickLaunchPage(this);
                _pageUI.PropertyChanged += _pageUI_PagePropertyChanged;

                if (HostingWizard.IsPropertySet(LaunchWizardProperties.Global.propkey_VpcOnly))
                    IsVpcOnlyEnvironment = (bool)HostingWizard[LaunchWizardProperties.Global.propkey_VpcOnly];

                var account = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedAccount] as AccountViewModel;
                var ec2FVM = HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel;

                if (HostingWizard.IsPropertySet(LaunchWizardProperties.AMIOptions.propkey_SeedAMI))
                    LoadSeededAMI(account);
                else
                {
                    // can't change region in wizard, so safe to do this here
                    _pageUI.Images = _quickLaunchByRegion[ec2FVM.RegionSystemName];
                }

                // this page doesn't currently show for gov cloud accounts, but in case it ever does
                // remove the ability to create keypairs
                _pageUI.AllowCreateKeyPairSelection = !ec2FVM.AccountViewModel.Restrictions.Contains("IsGovCloudAccount");

                LoadExistingKeyPairNames(account);
                LoadExistingVpcSubnets(ec2FVM.EC2Client);
                LoadExistingSecurityGroups(null);
                LoadIAMProfiles(account);
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch] = true;

            HostingWizard.SetNavigationButtonVisibility(AWSWizardConstants.NavigationButtons.Back, false);
            HostingWizard.SetNavigationButtonText(AWSWizardConstants.NavigationButtons.Forward, "Advanced");

            // 'Advanced', aka Next is always enabled but Launch (Finish) is dependant on page controls
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, true);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, CanQuickLaunch);

            // note we force instance count back to 1 here - if the user has hit the Advanced button, edited
            // the field and then decides to come back to quick launch, we override them. Figure this is better
            // than (a) not allowing them back to Quick Launch after clicking Advanced (deters exploration)
            // and (b) retaining any value and having users surprised when > 1 instance launches
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceCount] = 1;
        }

        public bool PageDeactivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            // store the selection data even if moving to 'advanced' page -- we can use this to
            // re-select the ami & type on the next ami page, 'name' on the tags page and
            // keypair/group on the security page
            StorePageData();

            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
                HostingWizard[LaunchWizardProperties.AMIOptions.propkey_IsQuickLaunch] = false;

            return true;
        }

        public bool QueryFinishButtonEnablement()
        {
            return IsForwardsNavigationAllowed;
        }

        public void TestForwardTransitionEnablement()
        {
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Forward, IsForwardsNavigationAllowed);
            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, CanQuickLaunch);
        }

        public bool AllowShortCircuit()
        {
            // if we're called on this then we're not the active page (user has gone for the full wizard)
            // so we don't need to store anything
            return true;
        }

        #endregion

        private void StorePageData()
        {
            var selectedAmi = _pageUI.SelectedAMI;
            if (selectedAmi != null)
                HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] =
                    _imageWrappers[_pageUI.SelectedAMIID];
            else
                HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] = null;

            if (_pageUI.SelectedInstanceType != null)
                HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceType] = _pageUI.SelectedInstanceType.Id;
            else
                HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceType] = null;
            HostingWizard[LaunchWizardProperties.AMIOptions.propkey_InstanceCount] = 1;

            var keypairName = _pageUI.SelectedKeyPairName;
            HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_KeyPair] = keypairName;
            HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_CreatePair] =
                !string.IsNullOrEmpty(keypairName) && !_pageUI.IsExistingKeyPairNameSelected;

            var subnet = _pageUI.SelectedSubnet;
            // if this isn't the 'no preference' option used for vpc-only environments, record that
            // we are manually launching into a vpc so the correct subnet id gets passed to RunInstances
            if (subnet != null && subnet.NativeSubnet != null)
            {
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC] = true;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] = subnet;
            }
            else
            {
                // if we're in a vpc only env, the service will auto-select a default subnet to use if need be
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_LaunchIntoVPC] = false;
                HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_Subnet] = null;
            }

            List<SecurityGroupWrapper> securityGroups = null;
            if (_pageUI.SelectedSecurityGroup != null)
            {
                securityGroups = new List<SecurityGroupWrapper>();
                securityGroups.Add(_pageUI.SelectedSecurityGroup);
            }
            HostingWizard[LaunchWizardProperties.SecurityProperties.propkey_Groups] = securityGroups;

            if (_pageUI.InstanceNameIsValid)
            {
                var name = _pageUI.InstanceName;
                if (!string.IsNullOrEmpty(name))
                    InstanceTagsPageController.UpdateNameInstanceTagValue(HostingWizard, name);
            }

            HostingWizard[LaunchWizardProperties.AdvancedAMIOptions.propkey_InstanceProfile] =
                _pageUI.SelectedInstanceProfile != null ? _pageUI.SelectedInstanceProfile.Arn : null;

            HostingWizard[LaunchWizardProperties.StorageProperties.propkey_QuickLaunchVolumeType] = _pageUI.SelectedVolumeType.TypeCode;
            // if the user entered volume size is less than size declared in quicklaunch data, or built-in 
            // sizes for volume type on platform, override
            var userVolumeSize = _pageUI.VolumeSize;
            if (selectedAmi != null)
            {
                var minSize = selectedAmi.TotalImageSize;
                if (minSize <= 0)
                    minSize = _pageUI.SelectedVolumeType.MinimumSizeForPlatform(selectedAmi.Platform);
                if (userVolumeSize < minSize)
                    userVolumeSize = minSize;
            }
            HostingWizard[LaunchWizardProperties.StorageProperties.propkey_QuickLaunchVolumeSize] = userVolumeSize;
        }

        // used to ensure 'Advanced' (aka Next) is always enabled; for Launch (aka Finish) depends on
        // page control population
        bool IsForwardsNavigationAllowed
        {
            get { return true; }
        }

        // used to enable the Launch button as soon as the bare minimum of data is available from the user
        bool CanQuickLaunch
        {
            get
            {
                if (BackgroundWorkerCount != 0)
                    return false;

                if (string.IsNullOrEmpty(_pageUI.SelectedAMIID) || _pageUI.SelectedInstanceType == null)
                    return false;

                if (!_pageUI.InstanceNameIsValid)
                    return false;

                if (IsVpcOnlyEnvironment && _pageUI.SelectedSubnet == null)
                    return false;

                if (_pageUI.SelectedInstanceType.RequiresVPC && _pageUI.SelectedSubnet == null)
                    return false;

                if (_pageUI.SelectedSecurityGroup == null)
                    return false;

                return true;
            }
        }

        void LoadSeededAMI(AccountViewModel account)
        {
            var seedAMI = HostingWizard[LaunchWizardProperties.AMIOptions.propkey_SeedAMI] as AMIImage;
            var img = EC2QuickLaunchImage.FromImage(seedAMI);
            _pageUI.AllowFiltering = false;
            _pageUI.Images = new[] { img };
            _pageUI.SelectedAMI = img;
        }

        /// <summary>
        /// check prior to first activation that the full set of declared amis in the toolkit copy of the
        /// quick launch file are available; if not declare this page unusable
        /// </summary>
        /// <returns></returns>
        bool VerifyQuickLaunchAMIsAvailable()
        {
            var quickLaunchFileValid = false;

            try
            {
                var json = S3FileFetcher.Instance.GetFileContent("EC2QuickLaunch.json");
                quickLaunchFileValid = VerifyQuickLaunchAMIData(json);
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception loading/processing quick launch data; declaring quick launch unavailable: {0}", exc.Message);
            }

            if (!quickLaunchFileValid)
                HostingWizard.SetProperty(LaunchWizardProperties.Global.propkey_QuickLaunchUnavailable, true);
            else
                HostingWizard.SetProperty(LaunchWizardProperties.Global.propkey_QuickLaunchUnavailable, null);

            return quickLaunchFileValid;
        }

        bool VerifyQuickLaunchAMIData(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentException("Supplied ec2 quick launch data was empty. Unable to access file?");

            var jdata = JsonMapper.ToObject(json);
            for (var r = 0; r < jdata.Count; r++)
            {
                var images = new List<EC2QuickLaunchImage>();
                var regionObj = jdata[r];
                var amiList = regionObj["amiList"];
                if (amiList != null)
                {
                    for (var a = 0; a < amiList.Count; a++)
                    {
                        var img = EC2QuickLaunchImage.Deserialize(amiList[a]);
                        images.Add(img);
                    }
                }

                var regionTag = (string)regionObj["region"];
                _quickLaunchByRegion.Add(regionTag, images);
            }

            // must check that all amis for the region we're going to use are available before
            // declaring page usable
            var ec2FVM = HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel;
            IEnumerable<EC2QuickLaunchImage> regionImages = _quickLaunchByRegion[ec2FVM.RegionSystemName];
            if (regionImages == null || !regionImages.Any())
                return false;

            var imageIDs = new string[regionImages.Count()];
            var i = 0;
            foreach (var img in regionImages)
            {
                if(!string.IsNullOrEmpty(img.ImageId32))
                    imageIDs[i++] = img.ImageId32;
                else if (!string.IsNullOrEmpty(img.ImageId64))
                    imageIDs[i++] = img.ImageId64;
            }

            try
            {
                var response = ec2FVM.EC2Client.DescribeImages(new DescribeImagesRequest { ImageIds = imageIDs.ToList() });
                if (response.Images.Count < regionImages.Count())
                {
                    LOGGER.ErrorFormat("Quick launch file declares {0} amis for region {1}, service returned {2}",
                                        regionImages.Count<EC2QuickLaunchImage>(),
                                        ec2FVM.RegionSystemName,
                                        response.Images.Count);
                    return false; // don't mind if there's more unreferenced ones
                }
            }
            catch (AmazonEC2Exception exc)
            {
                LOGGER.ErrorFormat("EC2 exception querying existence of quick launch amis: {0}", exc.Message);
                throw new Exception("Rethrowing EC2 exception", exc);
            }

            return true;
        }

        void LoadExistingSecurityGroups(string vpcId)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QuerySecurityGroupsWorker((HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client, 
                                          vpcId,
                                          HostingWizard.Logger,
                                          OnSecurityGroupsAvailable);
        }

        void OnSecurityGroupsAvailable(ICollection<SecurityGroup> securityGroups)
        {
            this._pageUI.SetAvailableSecurityGroups(securityGroups, string.Empty);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        /// <summary>
        /// Load all existing key pairs for the in-context account and preselect the
        /// last used pair, if it was persisted on a previous run
        /// </summary>
        void LoadExistingKeyPairNames(AccountViewModel account)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            var region = HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] as RegionEndPointsManager.RegionEndPoints;
            if (region == null)
                return;

            new QueryKeyPairNamesWorker(account,
                                        region.SystemName,
                                        (HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client,
                                        HostingWizard.Logger,
                                        new QueryKeyPairNamesWorker.DataAvailableCallback(OnKeyPairNamesAvailable));
        }

        void OnKeyPairNamesAvailable(ICollection<string> keypairNames, ICollection<string> keyPairsStoredInToolkit)
        {
            _pageUI.SetAvailableKeyPairs(keypairNames, keyPairsStoredInToolkit, string.Empty /* todo on persistence */);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void LoadExistingVpcSubnets(IAmazonEC2 ec2Client)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryVpcsAndSubnetsWorker(ec2Client, HostingWizard.Logger, new QueryVpcsAndSubnetsWorker.DataAvailableCallback(OnVpcSubnetsAvailable));
        }

        void OnVpcSubnetsAvailable(ICollection<Vpc> vpcs, ICollection<Subnet> subnets)
        {
            this._pageUI.SetAvailableVpcSubnets(vpcs, subnets, IsVpcOnlyEnvironment);
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void LoadIAMProfiles(AccountViewModel account)
        {
            Interlocked.Increment(ref _backgroundWorkersActive);
            new QueryInstanceProfilesWorker(account, 
                                            HostingWizard[CommonWizardProperties.AccountSelection.propkey_SelectedRegion] 
                                                as RegionEndPointsManager.RegionEndPoints,
                                            HostingWizard.Logger,
                                            new QueryInstanceProfilesWorker.DataAvailableCallback(OnIAMProfilesAvailable));
        }

        void OnIAMProfilesAvailable(ICollection<InstanceProfile> profiles)
        {
            _pageUI.SetIAMInstanceProfiles(profiles);
            // cache so advanced mode of the wizard can re-use if needed
            HostingWizard[LaunchWizardProperties.Global.propkey_CachedIAMInstanceProfiles] = profiles;
            Interlocked.Decrement(ref _backgroundWorkersActive);
            TestForwardTransitionEnablement();
        }

        void _pageUI_PagePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == QuickLaunchPage.uiProperty_Ami)
            {
                IList<InstanceType> instanceTypes = null;
                if (_pageUI.SelectedAMI != null)
                {
                    var iw = ImageWrapperFromAMIId(_pageUI.SelectedAMIID);
                    if (iw != null)
                        instanceTypes = InstanceType.GetValidTypes(iw.NativeImage);
                }

                if (instanceTypes == null)
                    instanceTypes = new List<InstanceType>();

                _pageUI.SetInstanceTypes(instanceTypes);
                _pageUI.SetVolumeSizeForSelectedAMI();
            }

            if (e.PropertyName == QuickLaunchPage.uiProperty_VpcSubnet)
            {
                var subnet = _pageUI.SelectedSubnet;
                // if a real subnet was selected, reset the security groups dropdown to the
                // groups belonging to the parent vpc, otherwise populate with non-vpc groups
                string vpcId = null;
                if (subnet != null && subnet.NativeSubnet != null)
                    vpcId = subnet.VpcId;

                LoadExistingSecurityGroups(vpcId);
            }

            HostingWizard.SetNavigationEnablement(this, AWSWizardConstants.NavigationButtons.Finish, CanQuickLaunch);
        }

        ImageWrapper ImageWrapperFromAMIId(string amiid)
        {
            if (_imageWrappers.ContainsKey(amiid))
                return _imageWrappers[amiid];

            // for simplicity, make this a sync call for now
            try
            {
                var ec2Client = (HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client;
                var response = ec2Client.DescribeImages(new DescribeImagesRequest { ImageIds = new List<string>{ amiid }});
                if (response.Images != null && response.Images.Count > 0)
                {
                    var iw = new ImageWrapper(response.Images[0]);
                    _imageWrappers.Add(amiid, iw);
                    return iw;
                }
            }
            catch (Exception exc)
            {
                LOGGER.ErrorFormat("Caught exception whilst request details for ami id '{0}', exception message '{1}'.", amiid, exc.Message);
            }

            return null;
        }
    }
}
