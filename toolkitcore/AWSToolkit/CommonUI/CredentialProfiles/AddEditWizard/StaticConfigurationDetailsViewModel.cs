using System;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Util;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class StaticConfigurationDetailsViewModel : ConfigurationDetailsViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(StaticConfigurationDetailsViewModel));

        private const string _iamUserConsoleUrl = "https://console.aws.amazon.com/iam/home?region=us-east-1#/users";

        public override CredentialType CredentialType => CredentialType.StaticProfile;

        #region ProfileName
        public string ProfileName
        {
            get => ProfileProperties.Name;
            set
            {
                ProfileProperties.Name = value;
                ValidateProfileName();
                NotifyPropertyChanged(nameof(ProfileName));
            }
        }

        private void ValidateProfileName()
        {
            DataErrorInfo.ClearErrors(nameof(ProfileName));

            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                DataErrorInfo.AddError("Must not be empty and should contain alphanumeric characters, - or _", nameof(ProfileName));
                return;
            }
            var pattern = new Regex("^([a-zA-Z0-9_-])*$");
            var match = pattern.Match(ProfileName);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Must contain alphanumeric, - or _ characters", nameof(ProfileName));
                return;
            }

            var allProfiles = _toolkitContext.CredentialManager.GetCredentialIdentifiers()
                .Where(credId => credId.FactoryId.Equals(GetFactoryId(SelectedCredentialFileType)));
            var result = allProfiles.Select(credId => credId.ProfileName).Any(x => x.Equals(ProfileName));

            if (result)
            {
                DataErrorInfo.AddError("Name is not unique", nameof(ProfileName));
            }
        }

        private string GetFactoryId(CredentialFileType selectedCredentialType)
        {
            switch (selectedCredentialType)
            {
                case CredentialFileType.Sdk:
                    return SDKCredentialProviderFactory.SdkProfileFactoryId;
                case CredentialFileType.Shared:
                    return SharedCredentialProviderFactory.SharedProfileFactoryId;
            }
            return null;
        }
        #endregion

        #region AccessKeyID
        public string AccessKeyID
        {
            get => ProfileProperties.AccessKey;
            set
            {
                ProfileProperties.AccessKey = value;
                ValidateAccessKeyID();
                NotifyPropertyChanged(nameof(AccessKeyID));
            }
        }

        private void ValidateAccessKeyID()
        {
            DataErrorInfo.ClearErrors(nameof(AccessKeyID));

            if (string.IsNullOrWhiteSpace(AccessKeyID))
            {
                DataErrorInfo.AddError("Must be alphanumeric and between 16-128 characters", nameof(AccessKeyID));
                return;
            }

            var pattern = new Regex("^([a-zA-Z0-9]{16,128})$");
            var match = pattern.Match(AccessKeyID);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Must be alphanumeric and between 16-128 characters", nameof(AccessKeyID));
            }
        }
        #endregion

        #region SecretKey
        public string SecretKey
        {
            get => ProfileProperties.SecretKey;
            set
            {
                ProfileProperties.SecretKey = value;
                ValidateSecretKey();
                NotifyPropertyChanged(nameof(SecretKey));
            }
        }

        private void ValidateSecretKey()
        {
            DataErrorInfo.ClearErrors(nameof(SecretKey));

            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                DataErrorInfo.AddError("Must not be empty", nameof(SecretKey));
            }
        }
        #endregion

        #region ImportCsvFile

        private ICommand _importCsvFileCommand;

        public ICommand ImportCsvFileCommand
        {
            get => _importCsvFileCommand;
            set => SetProperty(ref _importCsvFileCommand, value);
        }

        private void ImportCsvFile(object parameter)
        {
            try
            {
                if (PromptForImportCsvFile(out var filename))
                {
                    const string keyColumn = "Access key ID";
                    const string secretColumn = "Secret access key";

                    var csvData = new HeaderedCsvFile(filename);
                    var rowData = csvData.ReadHeaderedData(new[] { keyColumn, secretColumn }, 0);

                    AccessKeyID = rowData[keyColumn];
                    SecretKey = rowData[secretColumn];
                }
            }
            catch (Exception ex)
            {
                var msg = "Unable to import CSV file.";
                _logger.Error(msg, ex);
                _toolkitContext.ToolkitHost.ShowError(msg);
            }
        }

        private bool PromptForImportCsvFile(out string filename)
        {
            var dialog = _toolkitContext.ToolkitHost.GetDialogFactory().CreateOpenFileDialog();
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.DefaultExt = ".csv";
            dialog.Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            dialog.Title = "Import AWS Credentials from CSV file";

            var result = dialog.ShowDialog().GetValueOrDefault();

            filename = result ? dialog.FileName : null;
            return result;
        }
        #endregion

        private ICommand _openIamUsersConsoleCommand;

        public ICommand OpenIamUsersConsoleCommand
        {
            get => _openIamUsersConsoleCommand;
            private set => SetProperty(ref _openIamUsersConsoleCommand, value);
        }

        private string _addButtonText = "Add";

        public string AddButtonText
        {
            get => _addButtonText;
            set => SetProperty(ref _addButtonText, value);
        }

        private ICommand _saveCommand;

        public ICommand SaveCommand
        {
            get => _saveCommand;
            private set => SetProperty(ref _saveCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            ProfileRegionSelectorMixin = new RegionSelectorMixin(_toolkitContext, region => ProfileProperties.Region = region.Id);

            ImportCsvFileCommand = new RelayCommand(ImportCsvFile);
            SaveCommand = new AsyncRelayCommand(CanSave, SaveAsync);
            OpenIamUsersConsoleCommand = OpenUrlCommandFactory.Create(_toolkitContext, _iamUserConsoleUrl);
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveCommand.CanExecute(null);
        }

        private async Task SaveAsync(object parameter)
        {
            try
            {
                _addEditProfileWizard.InProgress = true;
                AddButtonText = "Adding profile...";

                await _addEditProfileWizard.SaveAsync(ProfileProperties, SelectedCredentialFileType);
            }
            catch (Exception ex)
            {
                var msg = "Failed to save profile.";
                _logger.Error(msg, ex);
                _toolkitContext.ToolkitHost.ShowError(msg);
            }
            finally
            {
                AddButtonText = "Add";
                _addEditProfileWizard.InProgress = false;
            }
        }

        private bool CanSave(object parameter)
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(AccessKeyID) && !string.IsNullOrWhiteSpace(SecretKey) && !DataErrorInfo.HasErrors;
        }
    }
}
