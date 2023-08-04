using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.State;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Regions;
using Amazon.AWSToolkit.Telemetry.Model;
using Amazon.AWSToolkit.Util;
using Amazon.Runtime.CredentialManagement;

using log4net;

using CredentialType = Amazon.AWSToolkit.Credentials.Utils.CredentialType;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public class CredentialProfileFormViewModel : BaseModel, IDisposable
    {
        private const int _validateConnectionTimeoutMillis = 10000;

        private const int _saveTimeoutMillis = 10000;

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CredentialProfileFormViewModel));

        private readonly ToolkitContext _toolkitContext;

        private ProfileProperties _profileProperties;

        public ProfileProperties ProfileProperties
        {
            get => _profileProperties;
            private set => SetProperty(ref _profileProperties, value);
        }

        public RegionSelectorViewModel ProfileRegionSelectorViewModel { get; private set; }

        public RegionSelectorViewModel SsoRegionSelectorViewModel { get; private set; }

        private ProfileSubform _subform;

        public ProfileSubform Subform
        {
            get => _subform;
            set => SetProperty(ref _subform, value);
        }
        
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

        public string AccessKey
        {
            get => ProfileProperties.AccessKey;
            set
            {
                ProfileProperties.AccessKey = value;
                ValidateAccessKey();
                NotifyPropertyChanged(nameof(AccessKey));
            }
        }


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


        public string SsoStartUrl
        {
            get => ProfileProperties.SsoStartUrl;
            set
            {
                ProfileProperties.SsoStartUrl = value;
                ValidateSsoStartUrl();
                NotifyPropertyChanged(nameof(SsoStartUrl));
            }
        }


        #region CredentialType
        public IEnumerable<KeyValuePair<string, CredentialType>> CredentialTypes { get; } = new ObservableCollection<KeyValuePair<string, CredentialType>>()
        {
            new KeyValuePair<string, CredentialType>("IAM Identity Center (Successor to AWS Single Sign-on)", CredentialType.SsoProfile),
            new KeyValuePair<string, CredentialType>("IAM User Role Credentials", CredentialType.StaticProfile)
        };

        private CredentialType _selectedCredentialType = CredentialType.SsoProfile;

        public CredentialType SelectedCredentialType
        {
            get => _selectedCredentialType;
            set => SetProperty(ref _selectedCredentialType, value);
        }
        #endregion

        #region CredentialFileType
        public enum CredentialFileType
        {
            Sdk,
            Shared
        }

        public IEnumerable<KeyValuePair<string, CredentialFileType>> CredentialFileTypes { get; } = new ObservableCollection<KeyValuePair<string, CredentialFileType>>()
        {
            new KeyValuePair<string, CredentialFileType>(".NET Encrypted Store", CredentialFileType.Sdk),
            new KeyValuePair<string, CredentialFileType>("Shared Credentials File", CredentialFileType.Shared)
        };

        private CredentialFileType _selectedCredentialFileType = CredentialFileType.Shared;

        public CredentialFileType SelectedCredentialFileType
        {
            get => _selectedCredentialFileType;
            set => SetProperty(ref _selectedCredentialFileType, value);
        }
        #endregion

        private bool? _status;

        public bool? Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #region Save
        public class CredentialProfileSavedEventArgs : EventArgs
        {
            public ICredentialIdentifier CredentialIdentifier { get; }

            public CredentialProfileSavedEventArgs(ICredentialIdentifier credentialIdentifier)
            {
                CredentialIdentifier = credentialIdentifier;
            }
        }

        public event EventHandler<CredentialProfileSavedEventArgs> CredentialProfileSaved;

        protected virtual void OnCredentialProfileSaved(CredentialProfileSavedEventArgs e)
        {
            CredentialProfileSaved?.Invoke(this, e);
        }

        public ICommand SaveCommand { get; }

        private async Task SaveAsync(object parameter)
        {
            Status = null;

            if (!await ValidateConnectionAsync())
            {
                Status = false;
                return;
            }

            var credId = SelectedCredentialFileType == CredentialFileType.Shared ?
                new SharedCredentialIdentifier(ProfileProperties.Name) as ICredentialIdentifier :
                new SDKCredentialIdentifier(ProfileProperties.Name);
            var region = _toolkitContext.RegionProvider.GetRegion(ProfileProperties.Region ?? ToolkitRegion.DefaultRegionId);

            ActionResults actionResults = null;
            try
            {
                using (var cancelSource = new CancellationTokenSource(_saveTimeoutMillis))
                {
                    await _toolkitContext.CredentialSettingsManager.CreateProfileAsync(credId, ProfileProperties, cancelSource.Token);
                    await _toolkitContext.ConnectionManager.ChangeConnectionSettingsAsync(credId, region, cancelSource.Token);
                }

                OnCredentialProfileSaved(new CredentialProfileSavedEventArgs(credId));
                actionResults = new ActionResults().WithSuccess(true);
            }
            catch (Exception ex)
            {
                _logger.Error($"Unable to save and/or set profile {ProfileProperties?.Name} in AWS Explorer.", ex);

                Status = false;
                actionResults = ActionResults.CreateFailed(ex);
            }
            finally
            {
                RecordAddMetric(actionResults, MetricSources.CredentialProfileFormMetricSource.CredentialProfileForm);
            }
        }

        private async Task<bool> ValidateConnectionAsync()
        {
            using (var cancelSource = new CancellationTokenSource(_validateConnectionTimeoutMillis))
            {
                try
                {
                    return ConnectionState.IsValid(await ProfileProperties.ValidateConnectionAsync(_toolkitContext, cancelSource.Token));
                }
                catch (TaskCanceledException ex)
                {
                    _logger.Error($"Unable to validate credentials for {ProfileProperties?.Name}.", ex);
                    return false;
                }
            }
        }
        #endregion

        #region ImportCsvFile
        public ICommand ImportCsvFileCommand { get; }

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

                        ProfileProperties.AccessKey = rowData[keyColumn];
                        ProfileProperties.SecretKey = rowData[secretColumn];

                        NotifyPropertyChanged(nameof(ProfileProperties));
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

        #region OpenCredentialFile
        public ICommand OpenCredentialFileCommand { get; }

        private void OpenCredentialFile(object parameter)
        {
            _toolkitContext.ToolkitHost.OpenInEditor(new SharedCredentialsFile().FilePath);
        }
        #endregion

        public ICommand OpenIamUsersConsoleCommand { get; }

        public ICommand OpenLogsCommand { get; }

        public CredentialProfileFormViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            SelectedCredentialType = CredentialType.StaticProfile;
            ProfileProperties = new ProfileProperties();

            ProfileRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.Region, (value) => ProfileProperties.Region = value);
            SsoRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.SsoRegion, (value) => ProfileProperties.SsoRegion = value);

            SaveCommand = new AsyncRelayCommand(CanSave, SaveAsync);
            ImportCsvFileCommand = new RelayCommand(ImportCsvFile);
            OpenCredentialFileCommand = new RelayCommand(OpenCredentialFile);
            OpenIamUsersConsoleCommand = OpenUrlCommandFactory.Create(_toolkitContext, "https://console.aws.amazon.com/iam/home?region=us-east-1#/users");
            OpenLogsCommand = new OpenToolkitLogsCommand(_toolkitContext);
            PropertyChanged += OnPropertyChanged;
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveCommand.CanExecute(null);
        }

        private bool _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                ProfileRegionSelectorViewModel?.Dispose();
                ProfileRegionSelectorViewModel = null;

                SsoRegionSelectorViewModel?.Dispose();
                SsoRegionSelectorViewModel = null;
            }
        }

        private void RecordAddMetric(ActionResults actionResults, BaseMetricSource source)
        {
            var accountId = _toolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet;

            var data = actionResults.CreateMetricData<AwsModifyCredentials>(accountId, MetadataValue.NotApplicable);
            data.Result = actionResults.AsTelemetryResult();
            data.CredentialModification = CredentialModification.Add;
            data.Source = source.Location;
            data.ServiceType = source.Service;

            _toolkitContext.TelemetryLogger.RecordAwsModifyCredentials(data);
        }

        private bool CanSave(object arg)
        {
            if (string.IsNullOrWhiteSpace(ProfileName))
            {
                return false;
            }

            switch (SelectedCredentialType)
            {
                case CredentialType.StaticProfile:
                    return !string.IsNullOrWhiteSpace(AccessKey) && !string.IsNullOrWhiteSpace(SecretKey) && !HasStaticValidationErrors();

                case CredentialType.SsoProfile:
                    return !string.IsNullOrWhiteSpace(SsoStartUrl) && !HasSsoValidationErrors();

                default:
                    return !DataErrorInfo.HasErrors;
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

        private void ValidateSecretKey()
        {
            DataErrorInfo.ClearErrors(nameof(SecretKey));

            if (string.IsNullOrWhiteSpace(SecretKey))
            {
                DataErrorInfo.AddError("Must not be empty", nameof(SecretKey));
            }
        }

        private void ValidateSsoStartUrl()
        {
            DataErrorInfo.ClearErrors(nameof(SsoStartUrl));

            if (string.IsNullOrWhiteSpace(SsoStartUrl))
            {
                DataErrorInfo.AddError("Must not be empty", nameof(SsoStartUrl));
                return;
            }

            var pattern = new Regex("^http(s)?://.*\\.awsapps\\.com/start$");
            var match = pattern.Match(SsoStartUrl);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Url must be of format - `https://your_subdomain.awsapps.com/start`", nameof(SsoStartUrl));
            }
        }

        private void ValidateAccessKey()
        {
            DataErrorInfo.ClearErrors(nameof(AccessKey));

            if (string.IsNullOrWhiteSpace(AccessKey))
            {
                DataErrorInfo.AddError("Must be alphanumeric and between 16-128 characters", nameof(AccessKey));
                return;
            }

            var pattern = new Regex("^([a-zA-Z0-9]{16,128})$");
            var match = pattern.Match(AccessKey);
            if (!match.Success)
            {
                DataErrorInfo.AddError("Must be alphanumeric and between 16-128 characters", nameof(AccessKey));
            }
        }

        private bool HasStaticValidationErrors()
        {
            var profileNameErrors = DataErrorInfo.GetErrors(nameof(ProfileName)).OfType<object>().Any();
            var accessKeyError = DataErrorInfo.GetErrors(nameof(AccessKey)).OfType<object>().Any();
            var secretKeyErrors = DataErrorInfo.GetErrors(nameof(SecretKey)).OfType<object>().Any();

            return profileNameErrors || accessKeyError || secretKeyErrors;
        }

        private bool HasSsoValidationErrors()
        {
            var profileNameErrors = DataErrorInfo.GetErrors(nameof(ProfileName)).OfType<object>().Any();
            var ssoStartUrlError = DataErrorInfo.GetErrors(nameof(SsoStartUrl)).OfType<object>().Any();

            return profileNameErrors || ssoStartUrlError;
        }
    }
}
