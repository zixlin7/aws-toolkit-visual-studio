﻿using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Amazon.AWSToolkit.Util;
using log4net;

namespace Amazon.AWSToolkit.Account.Model
{
    public class RegisterServiceCredentialsModel : INotifyPropertyChanged
    {
        private static ILog LOGGER = LogManager.GetLogger(typeof(RegisterServiceCredentialsModel));

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

        public bool ImportCredentialsFromCsv(string csvCredentialsFile)
        {
            string username, password;
            if (ImportCredentialsFromCsv(csvCredentialsFile, out username, out password))
            {
                UserName = username;
                OnPropertyChanged(UserName);

                Password = password;
                OnPropertyChanged(Password);

                return true;
            }

            return false;
        }

        public static bool ImportCredentialsFromCsv(string csvCredentialsFile, out string username, out string password)
        {
            const string userNameColumnHeader = "User Name";
            const string passwordColumnHeader = "Password";

            username = null;
            password = null;

            try
            {
                var csvData = new HeaderedCsvFile(csvCredentialsFile);
                var rowData = csvData.ReadHeaderedData(new[] { userNameColumnHeader, passwordColumnHeader }, 0);

                username = rowData[userNameColumnHeader];
                password = rowData[passwordColumnHeader];

                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Invalid Git credentials file", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Invalid File", e.Message);
            }

            return false;
        }

        public bool PersistCredentials(ServiceSpecificCredentials credentials)
        {
            return PersistCredentials(credentials, Account.SettingsUniqueKey);
        }

        public static bool PersistCredentials(ServiceSpecificCredentials credentials, string accountKey)
        {
            ServiceSpecificCredentialStore
                .Instance
                .SaveCredentialsForService(accountKey, ServiceSpecificCredentialStore.CodeCommitServiceName, credentials);
            return true;
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
