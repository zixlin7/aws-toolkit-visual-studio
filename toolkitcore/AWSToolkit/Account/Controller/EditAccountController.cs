using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.Navigator;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class EditAccountController : RegisterAccountController, IContextCommand
    {
        public const string NAME_CHANGE_PARAMETER = "nameChange";
        public const string CREDENTIALS_CHANGE_PARAMETER = "credentialsChange";

        public const string ACCESSKEY_PARAMETER = "AccessKey";
        public const string SECRETKEY_PARAMETER = "SecretKey";
        public const string ACCOUNTNUMBER_PARAMETER = "AccountNumber";

        AccountViewModel _accountViewModel;
        public ActionResults Execute(IViewModel model)
        {
            this._accountViewModel = model as AccountViewModel;
            if(this._accountViewModel == null)
                return new ActionResults().WithSuccess(false);

            return base.Execute();
        }

        protected override void LoadModel()
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            var os = settings[this._accountViewModel.SettingsUniqueKey];
            this.Model.DisplayName = os[ToolkitSettingsConstants.DisplayNameField];
            this.Model.AccessKey = os[ToolkitSettingsConstants.AccessKeyField];
            this.Model.SecretKey = os[ToolkitSettingsConstants.SecretKeyField];
            this.Model.AccountNumber = os[ToolkitSettingsConstants.AccountNumberField];

            var restrictions = os[ToolkitSettingsConstants.Restrictions];
            if (restrictions != null)
            {
                foreach (var restriction in restrictions.Split(','))
                {
                    var accountType = this.Model.AllAccountTypes.FirstOrDefault(x => x.SystemName == restriction.Trim());
                    if (accountType != null)
                    {
                        this.Model.SelectedAccountType = accountType;
                        break;
                    }
                }
            }

            // don't want to show the 'use default because...' prompt in this scenario
            DefaultProfileNameInUse = true;
            this.Model.UniqueKey = new Guid( this._accountViewModel.SettingsUniqueKey);
        }

        public override void Persist()
        {
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            var os = settings[this._accountViewModel.SettingsUniqueKey];

            bool nameChange = os[ToolkitSettingsConstants.DisplayNameField] != this.Model.DisplayName;
            bool credentialsChange = os[ToolkitSettingsConstants.AccessKeyField] != this.Model.AccessKey || os[ToolkitSettingsConstants.SecretKeyField] != this.Model.SecretKey;
            os[ToolkitSettingsConstants.DisplayNameField] = (this.Model.DisplayName ?? string.Empty).Trim();
            os[ToolkitSettingsConstants.AccessKeyField] = (this.Model.AccessKey ?? string.Empty).Trim();
            os[ToolkitSettingsConstants.SecretKeyField] = (this.Model.SecretKey ?? string.Empty).Trim();
            os[ToolkitSettingsConstants.AccountNumberField] = (this.Model.AccountNumber ?? string.Empty).Trim();
            os[ToolkitSettingsConstants.Restrictions] = this.Model.SelectedAccountType.SystemName;

            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);

            this._accountViewModel.ReloadFromPersistence();

            this._results = new ActionResults().WithSuccess(true)
                .WithFocalname(this.Model.DisplayName)
                .WithParameter(NAME_CHANGE_PARAMETER, nameChange)
                .WithParameter(CREDENTIALS_CHANGE_PARAMETER, credentialsChange)
                .WithParameter(ACCOUNTNUMBER_PARAMETER, this.Model.AccountNumber)
                .WithParameter(ACCESSKEY_PARAMETER, this.Model.AccessKey)
                .WithParameter(SECRETKEY_PARAMETER, this.Model.SecretKey);
        }
    }
}
