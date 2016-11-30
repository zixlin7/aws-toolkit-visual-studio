using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterAccountModel
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
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
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
    }
}
