using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

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
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CredentialSelectionDialogViewModel));

        private readonly IToolkitContextProvider _toolkitContextProvider;

        public CredentialSelectionDialogViewModel(IToolkitContextProvider toolkitContextProvider)
        {
            _toolkitContextProvider = toolkitContextProvider;

            LoadCredentialIdentifiers();
           
            CreateCredentialProfileCommand = new AsyncRelayCommand(ExecuteCreateCredentialProfileAsync);
            SubmitDialogCommand = new RelayCommand(CanExecuteSubmitDialog, ExecuteSubmitDialog);
            CancelDialogCommand = new RelayCommand(ExecuteCancelDialog);
        }

        private void LoadCredentialIdentifiers()
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

            SelectedCredentialIdentifier = CredentialIdentifiers.FirstOrDefault();
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

        #region CreateCredentialProfileCommand
        public ICommand CreateCredentialProfileCommand { get; }

        private async Task ExecuteCreateCredentialProfileAsync(object parameter)
        {
            const string commandName = "AWSToolkit.GettingStarted";

            var commandId = await _toolkitContextProvider.GetToolkitContext()
                .ToolkitHost.QueryCommandAsync(commandName);

            if (commandId != null)
            {
                await commandId.ExecuteAsync();
                CancelDialogCommand.Execute(null);
            }
            else
            {
                var msg = "Unable to create a profile.  Try Getting Started.";
                _toolkitContextProvider.GetToolkitContext()?.ToolkitHost
                    .ShowError(msg);
                _logger.Error($"Unable to find {commandName} command.");
                _logger.Warn(msg);
            }
        }
        #endregion

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
