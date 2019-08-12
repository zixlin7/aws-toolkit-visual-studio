using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterAccountModel : INotifyPropertyChanged
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(RegisterAccountModel));

        public RegisterAccountModel()
        {            
        }

        public RegisterAccountModel(Guid uniqueKey, string displayName, string accessKey, string secretKey, string accountNumber)
        {
            this.UniqueKey = uniqueKey;
            this.DisplayName = displayName;
            this.AccessKey = accessKey;
            this.AccountNumber = accountNumber;
        }

        public Guid UniqueKey { get; set; }
        public string DisplayName { get; set; }

        private string _accessKey;
        public string AccessKey
        {
            get => _accessKey;
            set { _accessKey = value; OnPropertyChanged(); }
        }

        private string _secretKey;
        public string SecretKey
        {
            get => _secretKey;
            set { _secretKey = value; OnPropertyChanged(); }
        }

        public string AccountNumber { get; set; }

        public override string ToString()
        {
            return string.Format("DisplayName: {0}, AccessKey: {1}, SecretKey {2}", this.DisplayName, this.AccessKey, this.SecretKey);
        }

        public void Validate()
        {
            if (string.IsNullOrEmpty(this.DisplayName))
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

        AccountTypes.AccountType selectedAccountType;
        public AccountTypes.AccountType SelectedAccountType
        {
            get
            {
                if (selectedAccountType == null)
                    selectedAccountType = AllAccountTypes[0];
                return selectedAccountType;
            }
            set => selectedAccountType = value;
        }

        public IList<AccountTypes.AccountType> AllAccountTypes => AccountTypes.AllAccountTypes;

        public System.Windows.Visibility StorageLocationVisibility
        {
            get;
            set;
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
                var rowData = csvData.ReadHeaderedData(new[] { accessKeyIdColumnHeader , secretAccessKeyColumnHeader }, 0);

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
