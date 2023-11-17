using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AwsToolkit.CodeWhisperer.Commands;
using Amazon.AwsToolkit.CodeWhisperer.Settings;
using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Sono;
using Amazon.AWSToolkit.Credentials.Utils;

using log4net;

namespace Amazon.AwsToolkit.CodeWhisperer.Credentials
{
    internal class CredentialSelectionDialogViewModel : BaseModel
    {
        private class OverridenGettingStartedCommand : GettingStartedCommand
        {
            private readonly CredentialSelectionDialogViewModel _parent;

            public OverridenGettingStartedCommand(IToolkitContextProvider toolkitContextProvider,
                CredentialSelectionDialogViewModel parent)
                : base(toolkitContextProvider)
            {
                _parent = parent;
            }

            protected override async Task ExecuteCoreAsync(object parameter)
            {
                await base.ExecuteCoreAsync(parameter);
                _parent.CancelDialogCommand.Execute(null);
            }
        }

        private static readonly ILog _logger = LogManager.GetLogger(typeof(CredentialSelectionDialogViewModel));

        private readonly IToolkitContextProvider _toolkitContextProvider;

        private readonly ICodeWhispererSettingsRepository _settingsRepository;

        public CredentialSelectionDialogViewModel(
            IToolkitContextProvider toolkitContextProvider,
            ICodeWhispererSettingsRepository settingsRepository)
        {
            _toolkitContextProvider = toolkitContextProvider;
            _settingsRepository = settingsRepository;

            CreateCredentialProfileCommand = new OverridenGettingStartedCommand(_toolkitContextProvider, this);
            SubmitDialogCommand = new RelayCommand(CanExecuteSubmitDialog, ExecuteSubmitDialog);
            CancelDialogCommand = new RelayCommand(ExecuteCancelDialog);
        }

        public async Task InitializeAsync()
        {
            var tkc = _toolkitContextProvider.GetToolkitContext();
            var csm = tkc.CredentialSettingsManager;

            CredentialIdentifiers.AddAll(tkc.CredentialManager.GetCredentialIdentifiers()
                .Where(id =>
                {
                    var scopes = csm.GetProfileProperties(id).SsoRegistrationScopes;
                    return
                        scopes?.Length >= 2
                        && scopes.Contains(SonoProperties.CodeWhispererAnalysisScope)
                        && scopes.Contains(SonoProperties.CodeWhispererCompletionsScope)
                        && csm.GetCredentialType(id) == CredentialType.BearerToken;
                })
                .OrderBy(id => id.ProfileName));

            var settings = await _settingsRepository.GetAsync();
            SelectedCredentialIdentifier = CredentialIdentifiers.Where(ci => ci.Id == settings.CredentialIdentifier).FirstOrDefault()
                ?? CredentialIdentifiers.FirstOrDefault();
        }

        public ObservableCollection<ICredentialIdentifier> CredentialIdentifiers { get; } = new ObservableCollection<ICredentialIdentifier>();

        private ICredentialIdentifier _selectedCredentialIdentifier;

        public ICredentialIdentifier SelectedCredentialIdentifier
        {
            get => _selectedCredentialIdentifier;
            set => SetProperty(ref _selectedCredentialIdentifier, value);
        }

        private bool? _dialogResult;

        public bool? DialogResult
        {
            get => _dialogResult;
            private set => SetProperty(ref _dialogResult, value);
        }

        public ICommand CreateCredentialProfileCommand { get; }

        #region SubmitDialogCommand
        public ICommand SubmitDialogCommand { get; }

        private bool CanExecuteSubmitDialog(object parameter)
        {
            return SelectedCredentialIdentifier != null;
        }

        private void ExecuteSubmitDialog(object parameter)
        {
            DialogResult = true;
        }
        #endregion

        #region CancelDialogCommand
        public ICommand CancelDialogCommand { get; }

        private void ExecuteCancelDialog(object parameter)
        {
            DialogResult = false;
        }
        #endregion
    }
}
