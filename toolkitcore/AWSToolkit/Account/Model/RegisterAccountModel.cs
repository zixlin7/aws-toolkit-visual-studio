using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Regions.Manifest;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterAccountModel : INotifyPropertyChanged
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(RegisterAccountModel));
        private readonly IRegionProvider _regionProvider;
        private ToolkitRegion _region;
        private Partition _partition;
        private IList<Partition> _partitions = new List<Partition>();
        private ObservableCollection<ToolkitRegion> _regions = new ObservableCollection<ToolkitRegion>();
        private bool _isPartitionEnabled = false;

        public RegisterAccountModel(IRegionProvider regionProvider)
        {
            this._regionProvider = regionProvider;
            this._partitions = this._regionProvider?.GetPartitions();
        }

        public RegisterAccountModel()
        {
        }

        public RegisterAccountModel(string credentialId, string displayName, string accessKey, string secretKey)
        {
            this.CredentialId = credentialId;
            this.DisplayName = displayName;
            this.AccessKey = accessKey;
        }

        public RegisterAccountModel(Guid uniqueKey, string displayName, string accessKey, string secretKey)
        {
            this.UniqueKey = uniqueKey;
            this.DisplayName = displayName;
            this.AccessKey = accessKey;
        }

        public Guid UniqueKey { get; set; }
        public string CredentialId { get; set; }

        /// <summary>
        /// Display name of the account as shown in the Aws Explorer eg. "Profile:sample"
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// User entered profile name for the account eg. "sample"
        /// </summary>
        private string _profileName;

        public string ProfileName
        {
            get => _profileName;
            set
            {
                _profileName = value;
                OnPropertyChanged();
            }
        }

        private string _accessKey;

        public string AccessKey
        {
            get => _accessKey;
            set
            {
                if (_accessKey != value)
                {
                    _accessKey = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _secretKey;

        public string SecretKey
        {
            get => _secretKey;
            set
            {
                if (_secretKey != value)
                {
                    _secretKey = value;
                    OnPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Currently selected Region
        /// </summary>
        public ToolkitRegion Region
        {
            get => _region;
            set
            {
                _region = value;
                OnPropertyChanged();
            }
        }


        /// <summary>
        /// Currently selected Partition
        /// </summary>
        public Partition Partition
        {
            get => _partition;
            set
            {
                _partition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Enables visibility of partition field
        /// </summary>
        public bool IsPartitionEnabled
        {
            get => _isPartitionEnabled;
            set
            {
                _isPartitionEnabled = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Regions that can be selected in the Register/Edit dialog
        /// </summary>
        public ObservableCollection<ToolkitRegion> Regions
        {
            get => _regions;
            set
            {
                _regions = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Partitions that can be selected in the Register/Edit dialog
        /// </summary>
        public ObservableCollection<Partition> Partitions
        {
            get
            {
                if (_partitions != null)
                {
                    return new ObservableCollection<Partition>(_partitions);
                }

                return new ObservableCollection<Partition>();
            }
        }

        public void InitializeDefaultPartition()
        {
            Partition = Partitions.FirstOrDefault(x => string.Equals(x.Id, PartitionIds.AWS)) ?? Partitions.FirstOrDefault();
        }

        public ToolkitRegion GetRegion(string regionId)
        {
            if (string.IsNullOrWhiteSpace(regionId))
            {
                return null;
            }

            return Regions.FirstOrDefault(r => r.Id == regionId);
        }

        /// <summary>
        /// Updates the <see cref="Regions"/> list with regions contained by the given partitionId.
        /// Side effect: Databinding generally sets <see cref="Region"/> to null as a result.
        /// </summary>
        public void ShowRegionsForPartition(string partitionId)
        {
            var regions = _regionProvider.GetRegions(partitionId);

            Regions = new ObservableCollection<ToolkitRegion>(regions.Where(r=> !_regionProvider.IsRegionLocal(r.Id)).OrderBy(r => r.DisplayName));
        }

        public override string ToString()
        {
            return string.Format("DisplayName: {0}, AccessKey: {1}, SecretKey {2}", this.ProfileName, this.AccessKey,
                this.SecretKey);
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.ProfileName))
            {
                throw new ApplicationException("Display Name can not be empty");
            }

            if (string.IsNullOrEmpty(this.AccessKey))
            {
                throw new ApplicationException("Access Key can not be empty");
            }

            if (string.IsNullOrEmpty(this.SecretKey))
            {
                throw new ApplicationException("Secret Key can not be empty");
            }
        }

        private System.Windows.Visibility _storageLocationVisibility;
        public System.Windows.Visibility StorageLocationVisibility
        {
            get => _storageLocationVisibility;
            set
            {
                _storageLocationVisibility = value;
                OnPropertyChanged();
            }
        }

        StorageTypes.StorageType selectedStorageType;

        public StorageTypes.StorageType SelectedStorageType
        {
            get
            {
                if (selectedStorageType == null)
                    selectedStorageType = StorageTypes.SharedCredentialsFile;
                return selectedStorageType;
            }
            set
            {
                selectedStorageType = value;
                OnPropertyChanged("SelectedStorageType");
            }
        }

        public IList<StorageTypes.StorageType> AllStorageTypes => StorageTypes.AllStorageTypes;


        public void LoadAWSCredentialsFromCSV(string csvFilename)
        {
            string accessKey, secretKey;
            if (ReadAwsCredentialsFromCsv(csvFilename, out accessKey, out secretKey))
            {
                AccessKey = accessKey;
                SecretKey = secretKey;
            }
        }

        public static bool ReadAwsCredentialsFromCsv(string csvFilename, out string accessKey, out string secretKey)
        {
            const string accessKeyIdColumnHeader = "Access key ID";
            const string secretAccessKeyColumnHeader = "Secret access key";

            accessKey = null;
            secretKey = null;

            try
            {
                var csvData = new HeaderedCsvFile(csvFilename);
                var rowData = csvData.ReadHeaderedData(new[] {accessKeyIdColumnHeader, secretAccessKeyColumnHeader}, 0);

                accessKey = rowData[accessKeyIdColumnHeader];
                secretKey = rowData[secretAccessKeyColumnHeader];

                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Invalid csv credential file", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Invalid File", e.Message);
            }

            return false;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
