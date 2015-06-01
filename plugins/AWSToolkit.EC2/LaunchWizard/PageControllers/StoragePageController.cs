using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.CommonUI.WizardFramework;
using Amazon.AWSToolkit.EC2.LaunchWizard.PageUI;
using Amazon.AWSToolkit.EC2.Model;
using Amazon.AWSToolkit.EC2.Nodes;
using Amazon.EC2;
using Amazon.EC2.Model;
using Microsoft.SqlServer.Server;

namespace Amazon.AWSToolkit.EC2.LaunchWizard.PageControllers
{
    public class StoragePageController : IAWSWizardPageController
    {
        private StoragePage _pageUI;
        private string _lastSelectedAmiId;
        private string _lastSelectedInstanceType;

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
            get { return "Storage"; }
        }

        public string ShortPageTitle
        {
            get { return null; }
        }

        public string PageDescription
        {
            get { return "Configure the root device and add additional storage for the instance."; }
        }

        public bool QueryPageActivation(AWSWizardConstants.NavigationReason navigationReason)
        {
            return true;
        }

        public UserControl PageActivating(AWSWizardConstants.NavigationReason navigationReason)
        {
            if (_pageUI == null)
                _pageUI = new StoragePage(this);

            // might have changed ami, so reset model
            if (navigationReason == AWSWizardConstants.NavigationReason.movingForward)
            {
                var image = HostingWizard.CollectedProperties[LaunchWizardProperties.AMIOptions.propkey_SelectedAMI] as ImageWrapper;
                var instanceType = HostingWizard.CollectedProperties[LaunchWizardProperties.AMIOptions.propkey_InstanceType] as string;

                if (image.ImageId.Equals(_lastSelectedAmiId, StringComparison.OrdinalIgnoreCase) &&
                        instanceType.Equals(_lastSelectedInstanceType, StringComparison.OrdinalIgnoreCase)) 
                    return _pageUI;

                Model.Initialize((HostingWizard[LaunchWizardProperties.Global.propkey_EC2RootModel] as FeatureViewModel).EC2Client,
                                    image,
                                    instanceType);
                _lastSelectedAmiId = image.ImageId;
                _lastSelectedInstanceType = instanceType;
            }

            return _pageUI;
        }

        public void PageActivated(AWSWizardConstants.NavigationReason navigationReason)
        {
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
            // if we were never activated, do nothing so EC2 defaults for the selected 
            // ami take effect
            if (_pageUI == null)
                return;

            HostingWizard.SetProperty(LaunchWizardProperties.StorageProperties.propkey_StorageVolumes,
                Model.StorageVolumes);
        }

        bool IsForwardsNavigationAllowed
        {
            get { return true; }
        }

        readonly StoragePageModel _model = new StoragePageModel();

        internal StoragePageModel Model
        {
            get { return _model; }
        }
    }

    internal class StoragePageModel : BaseModel
    {
        // these values based on observation of the console's launch wizard
        private const int RootVolumeSize_Windows = 30;
        private const int RootVolumeSize_Linux = 8;

        // just in case these could ever differ...
        private const int AdditionalVolumeSize_Windows = 8;
        private const int AdditionalVolumeSize_Linux = 8;

        private readonly ObservableCollection<InstanceLaunchStorageVolume> _storageVolumes = new ObservableCollection<InstanceLaunchStorageVolume>();
        
        private ImageWrapper SelectedAMI { get; set; }
        private string SelectedInstanceType { get; set; }

        public ObservableCollection<InstanceLaunchStorageVolume> StorageVolumes
        {
            get { return _storageVolumes; }
        }

        // we get a list of all possible snapshots when we initialize against an ami and then
        // when a volume is added, we filter out the ones used in other storage volumes for the
        // new volume
        readonly List<SnapshotModel> _allSnapshots = new List<SnapshotModel>();

        public string AllowedVolumeTypes
        {
            get; private set;
        }

        public void Initialize(IAmazonEC2 ec2Client, ImageWrapper selectedAMI, string instanceType)
        {
            if (ec2Client == null || selectedAMI == null || string.IsNullOrEmpty(instanceType))
                throw new ArgumentNullException();

            StorageVolumes.Clear();

            SelectedAMI = selectedAMI;
            SelectedInstanceType = instanceType;

            var instanceTypeMeta = InstanceType.FindById(SelectedInstanceType);
            if (instanceTypeMeta != null)
            {
                if (instanceTypeMeta.MaxInstanceStoreVolumes != 0)
                    AllowedVolumeTypes = string.Format("The selected instance type '{0}' supports a maximum of {1} instance store volumes.",
                                                       SelectedInstanceType, 
                                                       instanceTypeMeta.MaxInstanceStoreVolumes);
                else
                    AllowedVolumeTypes = string.Format("The selected instance type '{0}' does not support instance store volumes.",
                                                       SelectedInstanceType);
            }
            else
                AllowedVolumeTypes = string.Empty;


            var rootVolume = new InstanceLaunchStorageVolume
            {
                Device = selectedAMI.IsWindowsPlatform
                                || selectedAMI.VirtualizationType.Equals(VirtualizationType.Paravirtual, StringComparison.OrdinalIgnoreCase)
                    ? "/dev/sda1"
                    : "/dev/xvda",
                Size = selectedAMI.IsWindowsPlatform ? RootVolumeSize_Windows : RootVolumeSize_Linux,
                DeleteOnTermination = true,
                Encrypted = false,
                VolumeType = InstanceLaunchStorageVolume.VolumeTypeFromCode(VolumeWrapper.GeneralPurposeTypeCode)
            };

            rootVolume.SetAvailableStorageTypesForInstanceType(SelectedInstanceType, 0);

            StorageVolumes.Add(rootVolume);

            NotifyPropertyChanged("StorageVolumes");
            NotifyPropertyChanged("AllowedVolumeTypes");

            LoadSnapshots(ec2Client);
        }

        public InstanceLaunchStorageVolume AddVolume(bool autoSelect = true)
        {
            var volume = new InstanceLaunchStorageVolume
            {
                Size = SelectedAMI.IsWindowsPlatform ? AdditionalVolumeSize_Windows : AdditionalVolumeSize_Linux,
                DeleteOnTermination = false,
                Encrypted = false,
                VolumeType = InstanceLaunchStorageVolume.VolumeTypeFromCode(VolumeWrapper.GeneralPurposeTypeCode)
            };

            // whether instance store volumes are allowed, and if so how many, is instance type dependent
            var assignedInstanceStore = StorageVolumes.Count(vol => vol.StorageType.Equals(VolumeWrapper.InstanceStoreVolumeType, StringComparison.Ordinal));
            volume.SetAvailableStorageTypesForInstanceType(SelectedInstanceType, assignedInstanceStore);

            // filter out all snaps already selected for use and hand what's left to the new volume
            var usedSnapshots = new HashSet<string>();
            foreach (var v in StorageVolumes.Where(v => v.Snapshot != null && !v.Snapshot.SnapshotId.Equals(SnapshotModel.NO_SNAPSHOT_ID)))
            {
                usedSnapshots.Add(v.Snapshot.SnapshotId);
            }

            SnapshotModel noSnapshotModel = null;

            var availableSnapshots = new ObservableCollection<SnapshotModel>();
            foreach (var s in _allSnapshots.Where(s => !usedSnapshots.Contains(s.SnapshotId)))
            {
                if (s.SnapshotId.Equals(SnapshotModel.NO_SNAPSHOT_ID))
                    noSnapshotModel = s;

                availableSnapshots.Add(s);
            }
            volume.Snapshots = availableSnapshots;
            if (noSnapshotModel != null)
                volume.Snapshot = noSnapshotModel;

            StorageVolumes.Add(volume);

            NotifyPropertyChanged("StorageVolumes");
            if (autoSelect)
            {
                SelectedVolume = volume;
                NotifyPropertyChanged("SelectedVolume");
            }

            return volume;
        }

        public void RemoveVolume(string volumeId)
        {
            if (string.IsNullOrEmpty(volumeId))
                return;

            foreach (var vol in StorageVolumes)
            {
                if (vol.ID.Equals(volumeId, StringComparison.OrdinalIgnoreCase))
                {
                    RemoveVolume(vol);
                    return;
                }
            }
        }

        public void RemoveVolume(InstanceLaunchStorageVolume volume)
        {
            if (volume == null) 
                return;

            SelectedVolume = null;
            NotifyPropertyChanged("SelectedVolume");

            StorageVolumes.Remove(volume);
            NotifyPropertyChanged("StorageVolumes");
        }

        public InstanceLaunchStorageVolume SelectedVolume { get; set; }

        public void LoadSnapshots(IAmazonEC2 ec2Client)
        {
            _allSnapshots.Clear();
            _allSnapshots.Add(new SnapshotModel(SnapshotModel.NO_SNAPSHOT_ID, "", "0"));

            var response = ec2Client.DescribeSnapshots(new DescribeSnapshotsRequest { OwnerIds = new List<string> { "self" } });

            foreach (var snapshot in response.Snapshots)
            {
                string name = String.Empty;
                foreach (var tag in snapshot.Tags)
                {
                    if (tag.Key.ToLower().Equals("name"))
                    {
                        name = tag.Value;
                        break;
                    }
                }
                _allSnapshots.Add(new SnapshotModel(snapshot.SnapshotId, snapshot.Description, snapshot.VolumeSize.ToString(), name));
            }
        }
    }
}
                                  