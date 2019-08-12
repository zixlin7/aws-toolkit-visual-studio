using System;
using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;

using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class RegisterAccountController
    {
        private RegisterAccountModel _model;
        private RegisterAccountControl _control;
        protected bool DefaultProfileNameInUse;

        protected ActionResults _results;

        public RegisterAccountController()
        {
            this._model = new RegisterAccountModel();
        }

        public RegisterAccountModel Model => this._model;

        public virtual ActionResults Execute()
        {
            this._model = new RegisterAccountModel();
            this.Model.StorageLocationVisibility = System.Windows.Visibility.Visible;
            this.LoadModel();
            this._control = new RegisterAccountControl(this);
            CustomizeControl(this._control);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(this._control))
            {
                return this._results;
            }

            return new ActionResults().WithSuccess(false);
        }

        protected virtual void CustomizeControl(RegisterAccountControl control)
        {

        }

        protected virtual void LoadModel()
        {
            // if this is the first account, seed the display name to 'default'
            // like the first-run experience
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            if (settings.Count == 0)
                _model.DisplayName = "default";
            else
            {
                foreach (var s in settings)
                {
                    if (s["DisplayName"].Equals("default", StringComparison.OrdinalIgnoreCase))
                    {
                        DefaultProfileNameInUse = true;
                        break;
                    }
                }
            }
        }

        public bool PromptToUseDefaultName => !DefaultProfileNameInUse;

        public virtual void Persist()
        {
            this.Model.UniqueKey = Guid.NewGuid();

            if (this.Model.SelectedStorageType == StorageTypes.DotNetEncryptedStore)
            {
                var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);

                var os = settings.NewObjectSettings(this.Model.UniqueKey.ToString());
                os[ToolkitSettingsConstants.AccessKeyField] = this.Model.AccessKey.Trim();
                os[ToolkitSettingsConstants.DisplayNameField] = this.Model.DisplayName.Trim();
                os[ToolkitSettingsConstants.SecretKeyField] = this.Model.SecretKey.Trim();
                os[ToolkitSettingsConstants.AccountNumberField] = this.Model.AccountNumber == null ? null : this.Model.AccountNumber.Trim();
                os[ToolkitSettingsConstants.Restrictions] = this.Model.SelectedAccountType.SystemName;

                PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);
            }
            else
            {
                var profileStore = new SharedCredentialsFile();
                CredentialProfile profile = new CredentialProfile(
                    this.Model.DisplayName.Trim(), 
                    new CredentialProfileOptions {
                        AccessKey = this.Model.AccessKey?.Trim(),
                        SecretKey = this.Model.SecretKey?.Trim()
                    }
                );

                CredentialProfileUtils.SetUniqueKey(profile, this.Model.UniqueKey);
                profileStore.RegisterProfile(profile);

                // The shared credential file can't be used to store account number and restrictions so we'll put that in a side SDK Credential store file.
                var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.NonNetSDKCredentialStoreMetadata);
                var os = settings.NewObjectSettings(this.Model.UniqueKey.ToString());

                os[ToolkitSettingsConstants.AccountNumberField] = (this.Model.AccountNumber ?? string.Empty).Trim();
                os[ToolkitSettingsConstants.Restrictions] = this.Model.SelectedAccountType.SystemName;

                PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.NonNetSDKCredentialStoreMetadata, settings);
            }

            this._results = new ActionResults().WithSuccess(true).WithFocalname(this.Model.DisplayName);
        }
    }
}
