using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

using Amazon.AWSToolkit.Collections;
using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;
using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Credentials.Utils;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime;
using Amazon.SSO;
using Amazon.SSO.Model;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public class SsoAwsCredentialConnectedStepViewModel : StepViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(SsoAwsCredentialConnectedStepViewModel));

        private IAddEditProfileWizardHost _host => ServiceProvider.RequireService<IAddEditProfileWizardHost>();

        private IConfigurationDetails _configDetails => ServiceProvider.RequireService<IConfigurationDetails>(CredentialType.SsoProfile.ToString());

        private IResolveAwsToken _resolveAwsToken => ServiceProvider.RequireService<IResolveAwsToken>();

        public ObservableCollection<RoleInfo> SsoAccountRoles { get; } = new ObservableCollection<RoleInfo>();

        #region SelectedSsoAccountRoles
        public ObservableCollection<RoleInfo> SelectedSsoAccountRoles { get; } = new ObservableCollection<RoleInfo>();

        private void ValidateSelectedSsoAccountRoles(object sender, NotifyCollectionChangedEventArgs e)
        {
            DataErrorInfo.ClearErrors(nameof(SelectedSsoAccountRoles));

            var profileNames = ToolkitContext.CredentialManager.GetCredentialIdentifiers()
                .Where(credId => credId.FactoryId.Equals(SharedCredentialProviderFactory.SharedProfileFactoryId))
                .Select(credId => credId.ProfileName);

            foreach (var accountRole in SelectedSsoAccountRoles)
            {
                var profileName = ToProfileName(accountRole.RoleName, accountRole.AccountId);

                if (profileNames.Contains(profileName))
                {
                    DataErrorInfo.AddError($"Profile {profileName} already exists in credentials file", nameof(SelectedSsoAccountRoles));
                }
            }

            SaveCommand.CanExecute(null);
        }
        #endregion

        #region BackToConnectionSetupCommand

        private ICommand _backToConnectionSetupCommand;

        public ICommand BackToConnectionSetupCommand
        {
            get => _backToConnectionSetupCommand;
            private set => SetProperty(ref _backToConnectionSetupCommand, value);
        }

        private void BackToConnectionSetup(object parameter)
        {
            _addEditProfileWizard.CurrentStep = WizardStep.Configuration;
        }
        #endregion

        #region Save

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

        private async Task SaveAsync(object parameter)
        {
            var actionResults = ActionResults.CreateFailed();
            var newConnectionCount = 0;

            try
            {
                _addEditProfileWizard.InProgress = true;
                AddButtonText = "Adding profile(s)...";

                var p = _configDetails.ProfileProperties.ShallowClone();
                SaveAsyncResults saveAsyncResults = null;

                foreach (var accountRoles in SelectedSsoAccountRoles)
                {
                    p.SsoAccountId = accountRoles.AccountId;
                    p.SsoRoleName = accountRoles.RoleName;
                    p.Name = ToProfileName(p.SsoRoleName, p.SsoAccountId);

                    saveAsyncResults = await _addEditProfileWizard.SaveAsync(p, CredentialFileType.Shared);
                    actionResults = saveAsyncResults.ActionResults;

                    if (!actionResults.Success)
                    {
                        throw new ConnectionToolkitException($"Cannot save profile {p.Name}", ConnectionToolkitException.ConnectionErrorCode.UnexpectedErrorOnSave,actionResults.Exception);
                    }

                    ++newConnectionCount;
                }

                if (newConnectionCount == 0 || saveAsyncResults == null)
                {
                    throw new ConnectionToolkitException($"No profiles found to save.", ConnectionToolkitException.ConnectionErrorCode.NoProfilesToSave);
                }

                await _addEditProfileWizard.ChangeAwsExplorerConnectionAsync(saveAsyncResults.CredentialIdentifier);
                _host.ShowCompleted(saveAsyncResults.CredentialIdentifier);
            }
            catch (Exception ex)
            {
                var msg = "Failed to save all SSO profiles.";
                _logger.Error(msg, ex);
                ToolkitContext.ToolkitHost.ShowError(msg);
                actionResults = ActionResults.CreateFailed(ex);
            }
            finally
            {
                _addEditProfileWizard.RecordAuthAddedConnectionsMetric(actionResults, newConnectionCount,
                    newConnectionCount > 0 ?
                        new HashSet<string>() { EnabledAuthConnectionTypes.IamIdentityCenterAwsExplorer } :
                        Enumerable.Empty<string>());

                AddButtonText = "Add";
                _addEditProfileWizard.InProgress = false;
            }
        }

        private bool CanSave(object parameter)
        {
            return SelectedSsoAccountRoles.Any() && !DataErrorInfo.HasErrors;
        }
        #endregion

        private string ToProfileName(string ssoRoleName, string ssoAccountId)
        {
            return $"{_configDetails.ProfileProperties.Name}-{ssoRoleName}-{ssoAccountId}";
        }

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            SaveCommand.CanExecute(null);
        }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            BackToConnectionSetupCommand = new RelayCommand(BackToConnectionSetup);
            SaveCommand = new AsyncRelayCommand(CanSave, SaveAsync);

            SelectedSsoAccountRoles.CollectionChanged += ValidateSelectedSsoAccountRoles;
        }

        public override async Task ViewShownAsync()
        {
            await base.ViewShownAsync();

            var token = await _resolveAwsToken.ResolveAwsTokenAsync();
            if (token == null)
            {
                _addEditProfileWizard.CurrentStep = WizardStep.Configuration;
                return;
            }

            var ssoRegion = RegionEndpoint.GetBySystemName(_configDetails.ProfileProperties.SsoRegion);
            await LoadAccountRoles(token.Token, ssoRegion);
        }

        internal async Task LoadAccountRoles(string token, RegionEndpoint region, IAmazonSSO ssoClient = null)
        {
            _addEditProfileWizard.InProgress = true;

            try
            {
                SsoAccountRoles.Clear();

                var accountRoles = new Collection<RoleInfo>();
                await Task.Run(() =>
                {
                    // This client is a snowflake that doesn't support IAWSTokenProvider.  The token must
                    // be provided directly in the requests and it requires anonymous credentials.  
                    using (ssoClient = ssoClient ?? new AmazonSSOClient(new AnonymousAWSCredentials(), region))
                    {
                        var listAccounts = ssoClient.Paginators.ListAccounts(new ListAccountsRequest()
                        {
                            AccessToken = token
                        });

                        foreach (var account in listAccounts.AccountList)
                        {
                            var listRoles = ssoClient.Paginators.ListAccountRoles(new ListAccountRolesRequest()
                            {
                                AccessToken = token,
                                AccountId = account.AccountId
                            });

                            accountRoles.AddAll(listRoles.RoleList);
                        }
                    }
                });

                SsoAccountRoles.AddAll(accountRoles);
            }
            catch (Exception ex)
            {
                _logger.Error("Unable to load account roles for SSO credentials.", ex);
            }

            if (!SsoAccountRoles.Any())
            {
                ToolkitContext.ToolkitHost.ShowError("No roles found, please verify settings are correct.");
                _addEditProfileWizard.CurrentStep = WizardStep.Configuration;
            }

            _addEditProfileWizard.InProgress = false;
        }
    }
}
