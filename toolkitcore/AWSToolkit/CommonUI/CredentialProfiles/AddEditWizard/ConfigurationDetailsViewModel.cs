using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Controls;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Utils;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public abstract class ConfigurationDetailsViewModel : ViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(ConfigurationDetailsViewModel));

        protected IAddEditProfileWizard _addEditProfileWizard => ServiceProvider.RequireService<IAddEditProfileWizard>();

        public abstract CredentialType CredentialType { get; }

        public string CredentialTypeDescription => CredentialType.GetDescription();

        public ProfileProperties ProfileProperties { get; } = new ProfileProperties();

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

        private ICommand _openCredentialsFileCommand;

        public ICommand OpenCredentialsFileCommand
        {
            get => _openCredentialsFileCommand;
            private set => SetProperty(ref _openCredentialsFileCommand, value);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            OpenCredentialsFileCommand = new RelayCommand(parameter =>
                _addEditProfileWizard.OpenCredentialsFile());
        }
    }
}
