using System;
using System.Collections.Generic;
using System.Text;

using Amazon.AWSToolkit.Persistence;
using Amazon.Util;
using Amazon.Runtime;
using Amazon.Runtime.Internal;
using Amazon.Runtime.CredentialManagement;
using Amazon.Runtime.CredentialManagement.Internal;

namespace TemplateWizard
{
    public class AccountsModel
    {
        private List<Account> accounts = new List<Account>();

        public AccountsModel()
        {
            LoadAccounts();

            accounts.Sort((x, y) =>
            {
                return x.Name.CompareTo(y.Name);
            });
        }

        public Account this[int index]
        {
            get
            {
                return accounts[index];
            }
        }

        public Account[] Accounts
        {
            get { return accounts.ToArray(); }
        }

        public int Count
        {
            get { return accounts.Count; }
        }

        public Account LastUsed
        {
            get
            {
                string lastAccountId = PersistenceManager.Instance.GetSetting(SettingsConstants.LastAcountSelectedKey);
                return accounts.Find(account => string.Equals(account.UniqueKey, lastAccountId));
            }
        }

        public bool Contains(Account account)
        {
            return accounts.Contains(account);
        }

        public bool NameExists(string name)
        {
            return accounts.Exists(account => string.Equals(account.Name, name));
        }

        public void SetLastUsed(string uniqueId)
        {
            PersistenceManager.Instance.SetSetting(SettingsConstants.LastAcountSelectedKey, uniqueId);
        }

        public void AddNewAccount(Account account)
        {
            var profileStore = new NetSDKCredentialsFile();
            var profileOptions = new CredentialProfileOptions
            {
                AccessKey = account.AccessKey,
                SecretKey = account.SecretKey
            };
            var profile = new CredentialProfile(account.Name, profileOptions);
            CredentialProfileUtils.SetUniqueKey(profile, Guid.NewGuid());

            CredentialProfileUtils.SetProperty(profile, SettingsConstants.AccountNumberField, account.Number);
            if (account.IsGovCloudAccount)
                CredentialProfileUtils.SetProperty(profile, SettingsConstants.Restrictions, "IsGovCloudAccount");

            profileStore.RegisterProfile(profile);
        }

        public void DeleteAccount(Account account)
        {
            accounts.Remove(account);
            string lastAccountId = PersistenceManager.Instance.GetSetting(SettingsConstants.LastAcountSelectedKey);

            SettingsCollection settings = PersistenceManager.Instance.GetSettings(SettingsConstants.RegisteredAccounts);
            settings.Remove(account.UniqueKey);
            PersistenceManager.Instance.SaveSettings(SettingsConstants.RegisteredAccounts, settings);

            if (string.Compare(lastAccountId, account.UniqueKey) == 0)
                SetLastUsed(string.Empty);
        }

        void LoadAccounts()
        {
            accounts.Clear();
            SettingsCollection settings = PersistenceManager.Instance.GetSettings(SettingsConstants.RegisteredAccounts);
            foreach (SettingsCollection.ObjectSettings setting in settings)
            {
                accounts.Add(new Account(setting.UniqueKey)
                {
                    Name = setting[SettingsConstants.DisplayNameField],
                    AccessKey = setting[SettingsConstants.AccessKeyField],
                    SecretKey = setting[SettingsConstants.SecretKeyField],
                    Number = setting[SettingsConstants.AccountNumberField]
                });
            }
        }


    }
}
