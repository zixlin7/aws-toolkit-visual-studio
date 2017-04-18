using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterAccountModel : INotifyPropertyChanged
    {
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
            get { return _accessKey; }
            set { _accessKey = value; OnPropertyChanged(); }
        }

        private string _secretKey;
        public string SecretKey
        {
            get { return _secretKey; }
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
            set
            {
                selectedAccountType = value;
            }
        }

        public IList<AccountTypes.AccountType> AllAccountTypes
        {
            get 
            {
                return AccountTypes.AllAccountTypes;
            }
        }

        public void LoadAWSCredentialsFromCSV(string csvFilename)
        {
            try
            {
                var csvData = new HeaderedCsvFile(csvFilename);
                // we expect to see User name,Password,Access key ID,Secret access key

                var akeyIndex = csvData.ColumnIndexOfHeader("Access key ID");
                var skeyIndex = csvData.ColumnIndexOfHeader("Secret access key");
                if (akeyIndex == -1 || skeyIndex == -1)
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Invalid File", "Csv file does not conform to expected layout. Expected columns 'Access key ID' and 'Secret access key'.");
                    return;
                }

                var rowData = csvData.ColumnValuesForRow(0);
                AccessKey = rowData.ElementAt(akeyIndex);
                SecretKey = rowData.ElementAt(skeyIndex);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
