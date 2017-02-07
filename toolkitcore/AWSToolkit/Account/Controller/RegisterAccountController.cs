using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;

using Amazon.Runtime.CredentialManagement;

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

        public RegisterAccountModel Model
        {
            get { return this._model; }
        }

        public virtual ActionResults Execute()
        {
            this._model = new RegisterAccountModel();
            this.LoadModel();
            this._control = new RegisterAccountControl(this);
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(this._control))
            {
                return this._results;
            }

            return new ActionResults().WithSuccess(false);
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

        public bool PromptToUseDefaultName
        {
            get { return !DefaultProfileNameInUse; }
        }

        public virtual void Persist()
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            this.Model.UniqueKey = Guid.NewGuid();

            var os = settings.NewObjectSettings(this.Model.UniqueKey.ToString());
            os[ToolkitSettingsConstants.AccessKeyField] = this.Model.AccessKey.Trim();
            os[ToolkitSettingsConstants.DisplayNameField] = this.Model.DisplayName.Trim();
            os[ToolkitSettingsConstants.SecretKeyField] = this.Model.SecretKey.Trim();
            os[ToolkitSettingsConstants.AccountNumberField] = this.Model.AccountNumber == null ? null : this.Model.AccountNumber.Trim();
            os[ToolkitSettingsConstants.Restrictions] = this.Model.SelectedAccountType.SystemName;

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);

            this._results = new ActionResults().WithSuccess(true).WithFocalname(this.Model.DisplayName);
        }
    }
}
