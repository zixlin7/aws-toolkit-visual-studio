using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Amazon.AWSToolkit.Util;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterServiceCredentialsModel : INotifyPropertyChanged
    {
        public RegisterServiceCredentialsModel(AccountViewModel account)
        {
            Account = account;
        }

        public string IAMConsoleEndpoint
        {
            get { return "https://console.aws.amazon.com/iam/home?region=us-east-1#/users"; }
        }

        public AccountViewModel Account { get; }

        public string UserName
        {
            get { return _userName; }
            set { _userName = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get { return _password; }
            set { _password = value; OnPropertyChanged(); }
        }

        public bool IsValid
        {
            get { return _isValid; }
            set { _isValid = value; OnPropertyChanged(); }
        }

        public void ImportCredentialsFromCSV(string csvCredentialsFile)
        {
            var csvData = new HeaderedCsvFile(csvCredentialsFile);

            // we expect to see User Name,Password
            var unameIndex = csvData.ColumnIndexOfHeader("User Name");
            var psswdIndex = csvData.ColumnIndexOfHeader("Password");
            if (unameIndex == -1 || psswdIndex == -1)
                throw new InvalidOperationException("Csv file does not conform to expected layout");

            var rowData = csvData.ColumnValuesForRow(0);
            UserName = rowData.ElementAt(unameIndex);
            OnPropertyChanged(UserName);

            Password = rowData.ElementAt(psswdIndex);
            OnPropertyChanged(Password);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _userName;
        private string _password;
        private bool _isValid;
    }
}
