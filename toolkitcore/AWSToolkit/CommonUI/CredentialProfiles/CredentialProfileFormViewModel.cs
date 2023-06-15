using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public class CredentialProfileFormViewModel : BaseModel, IDisposable
    {
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

        private Task Save(object parameter)
        {
            var credentialIdentifier = SelectedCredentialFileType == CredentialFileType.Shared ?
                new SharedCredentialIdentifier(ProfileProperties.Name) as ICredentialIdentifier :
                new SDKCredentialIdentifier(ProfileProperties.Name);

            _toolkitContext.CredentialSettingsManager.CreateProfile(credentialIdentifier, ProfileProperties);
            CredentialProfileSaved?.Invoke(this, new CredentialProfileSavedEventArgs());

            return Task.CompletedTask;
        }
        #endregion

        #region ImportCsvFile
        public ICommand ImportCsvFileCommand { get; }

        private Task ImportCsvFile(object parameter)
        {
            // TODO IDE-10794
            return Task.CompletedTask;
        }
        #endregion

        #region OpenCredentialFile
        public ICommand OpenCredentialFileCommand { get; }

        private Task OpenCredentialFile(object parameter)
        {
            // TODO IDE-10912
            return Task.CompletedTask;
        }
        #endregion

        public CredentialProfileFormViewModel(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;

            SelectedCredentialType = CredentialType.StaticProfile;
            ProfileProperties = new ProfileProperties();

            ProfileRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.Region, (value) => ProfileProperties.Region = value);
            SsoRegionSelectorViewModel = new RegionSelectorViewModel(_toolkitContext, () => ProfileProperties.SsoRegion, (value) => ProfileProperties.SsoRegion = value);

            SaveCommand = new AsyncRelayCommand(Save);
            ImportCsvFileCommand = new AsyncRelayCommand(ImportCsvFile);
            OpenCredentialFileCommand = new AsyncRelayCommand(OpenCredentialFile);
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
    }
}
