using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public abstract class ConfigurationDetailsViewModel : ViewModel, IConfigurationDetails
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ConfigurationDetailsViewModel));

        protected IAddEditProfileWizard _addEditProfileWizard => ServiceProvider.RequireService<IAddEditProfileWizard>();

        public abstract CredentialType CredentialType { get; }

        public string CredentialTypeDescription => CredentialType.GetDescription();

        private ProfileProperties _profileProperties;

        public ProfileProperties ProfileProperties
        {
            get => _profileProperties;
            set
            {
                if (_profileProperties != value)
                {
                    _profileProperties = value;
                    NotifyPropertyChanged(string.Empty); // Update all bindings
                }
            }
        }

        private class AddNewProfileCredentialIdentifier : ICredentialIdentifier
        {
            public string Id => throw new System.NotImplementedException();

            public string ProfileName => throw new System.NotImplementedException();

            public string DisplayName => "Add new profile";

            public string ShortName => throw new System.NotImplementedException();

            public string FactoryId => throw new System.NotImplementedException();
        }

        private readonly AddNewProfileCredentialIdentifier _addNewProfileCredentialIdentifier = new AddNewProfileCredentialIdentifier();

        private readonly ProfileProperties _addNewProfileProperties = new ProfileProperties();

        public ObservableCollection<ICredentialIdentifier> CredentialIdentifiers { get; } = new ObservableCollection<ICredentialIdentifier>();

        private void LoadCredentialIdentifiers()
        {
            SelectedCredentialIdentifier = null;
            CredentialIdentifiers.Clear();

            CredentialIdentifiers.Add(_addNewProfileCredentialIdentifier);

            CredentialIdentifiers.AddAll(
                FilterCredentialIdentifiers(ToolkitContext.CredentialManager.GetCredentialIdentifiers())
                .OrderBy(id => id.ProfileName));

            SelectedCredentialIdentifier = _addNewProfileCredentialIdentifier;
        }

        protected virtual IEnumerable<ICredentialIdentifier> FilterCredentialIdentifiers(IEnumerable<ICredentialIdentifier> credentialIdentifiers)
        {
            return credentialIdentifiers;
        }

        private ICredentialIdentifier _selectedCredentialIdentifier;

        public ICredentialIdentifier SelectedCredentialIdentifier
        {
            get => _selectedCredentialIdentifier;
            set
            {
                SetProperty(ref _selectedCredentialIdentifier, value);
                IsAddNewProfile = _selectedCredentialIdentifier == _addNewProfileCredentialIdentifier;
                ProfileProperties = IsAddNewProfile || _selectedCredentialIdentifier == null ?
                    _addNewProfileProperties :
                    ToolkitContext.CredentialSettingsManager.GetProfileProperties(_selectedCredentialIdentifier);
            }
        }

        private bool _isAddNewProfile;

        public bool IsAddNewProfile
        {
            get => _isAddNewProfile;
            private set => SetProperty(ref _isAddNewProfile, value);
        }

        private bool _credentialIdentifierSelectorVisible;

        public bool CredentialIdentifierSelectorVisible
        {
            get => _credentialIdentifierSelectorVisible;
            set => SetProperty(ref _credentialIdentifierSelectorVisible, value);
        }

        private RegionSelectorMixin _profileRegionSelectorMixin;

        public RegionSelectorMixin ProfileRegionSelectorMixin
        {
            get => _profileRegionSelectorMixin;
            protected set => SetProperty(ref _profileRegionSelectorMixin, value);
        }

        #region CredentialFileType

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

        public ConfigurationDetailsViewModel()
        {
            _profileProperties = _addNewProfileProperties;
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            LoadCredentialIdentifiers();
        }
    }
}
