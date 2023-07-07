using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Amazon.AWSToolkit.Telemetry;
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

        public ProfileProperties ProfileProperties { get; private set; }

        public RegionSelectorViewModel ProfileRegionSelectorViewModel { get; private set; }

        public RegionSelectorViewModel SsoRegionSelectorViewModel { get; private set; }

        private ProfileSubform _subform;

        public ProfileSubform Subform
        {
            get => _subform;
            set => SetProperty(ref _subform, value);
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

            SaveCommand = new AsyncRelayCommand(SaveAsync);
            ImportCsvFileCommand = new RelayCommand(ImportCsvFile);
            OpenCredentialFileCommand = new RelayCommand(OpenCredentialFile);
            OpenIamUsersConsoleCommand = OpenUrlCommandFactory.Create(_toolkitContext, "https://console.aws.amazon.com/iam/home?region=us-east-1#/users");
            OpenLogsCommand = new OpenToolkitLogsCommand(_toolkitContext);
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
            _toolkitContext.TelemetryLogger.RecordAwsModifyCredentials(new AwsModifyCredentials()
            {
                AwsAccount = _toolkitContext.ConnectionManager?.ActiveAccountId ?? MetadataValue.NotSet,
                AwsRegion = MetadataValue.NotApplicable,
                Result = actionResults.AsTelemetryResult(),
                CredentialModification = CredentialModification.Add,
                Source = source.Location,
                ServiceType = source.Service,
                Reason = TelemetryHelper.GetMetricsReason(actionResults?.Exception)
            });
        }
    }
}
