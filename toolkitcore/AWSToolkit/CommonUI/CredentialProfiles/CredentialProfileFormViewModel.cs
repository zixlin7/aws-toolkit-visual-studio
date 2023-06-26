using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;

using Amazon.AwsToolkit.Telemetry.Events.Core;
using Amazon.AwsToolkit.Telemetry.Events.Generated;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Telemetry;
using Amazon.AWSToolkit.Telemetry.Model;

using log4net;

using CredentialType = Amazon.AWSToolkit.Credentials.Utils.CredentialType;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public class CredentialProfileFormViewModel : BaseModel, IDisposable
    {
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

        #region Save
        public class CredentialProfileSavedEventArgs : EventArgs { }

        public event EventHandler<CredentialProfileSavedEventArgs> CredentialProfileSaved;

        public ICommand SaveCommand { get; }

        private void Save(object parameter)
        {
            var credentialIdentifier = SelectedCredentialFileType == CredentialFileType.Shared ?
                new SharedCredentialIdentifier(ProfileProperties.Name) as ICredentialIdentifier :
                new SDKCredentialIdentifier(ProfileProperties.Name);

            try
            {
                _toolkitContext.CredentialSettingsManager.CreateProfile(credentialIdentifier, ProfileProperties);
                RecordAddMetric(new ActionResults().WithSuccess(true), MetricSources.CredentialProfileFormMetricSource.CredentialProfileForm);
                CredentialProfileSaved?.Invoke(this, new CredentialProfileSavedEventArgs());
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to save credential profile.", ex);
                RecordAddMetric(ActionResults.CreateFailed(ex), MetricSources.CredentialProfileFormMetricSource.CredentialProfileForm);
            }
        }
        #endregion

        #region ImportCsvFile
        public ICommand ImportCsvFileCommand { get; }

        private void ImportCsvFile(object parameter)
        {
            // TODO IDE-10794
        }
        #endregion

        #region OpenCredentialFile
        public ICommand OpenCredentialFileCommand { get; }

        private void OpenCredentialFile(object parameter)
        {
            // TODO IDE-10912
        }
        #endregion

        #region OpenIamUsersConsole
        public ICommand OpenIamUsersConsoleCommand { get; }

        private void OpenIamUsersConsole(object parameter)
        {
            _toolkitContext.ToolkitHost.OpenInBrowser("https://console.aws.amazon.com/iam/home?region=us-east-1#/users", false);
        }
        #endregion

        public CredentialProfileFormViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            SelectedCredentialType = CredentialType.StaticProfile;
            ProfileProperties = new ProfileProperties();

            ProfileRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.Region, (value) => ProfileProperties.Region = value);
            SsoRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.SsoRegion, (value) => ProfileProperties.SsoRegion = value);

            SaveCommand = new RelayCommand(Save);
            ImportCsvFileCommand = new RelayCommand(ImportCsvFile);
            OpenCredentialFileCommand = new RelayCommand(OpenCredentialFile);
            OpenIamUsersConsoleCommand = new RelayCommand(OpenIamUsersConsole);
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
