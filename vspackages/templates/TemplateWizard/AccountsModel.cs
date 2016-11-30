using System;
using System.Collections.Generic;
using System.Text;

using Amazon.AWSToolkit.Persistence;

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
            var settings = PersistenceManager.Instance.GetSettings(SettingsConstants.RegisteredAccounts);
            Guid uniqueId = Guid.NewGuid();

            var os = settings.NewObjectSettings(uniqueId.ToString());
            os[SettingsConstants.AccessKeyField] = account.AccessKey;
            os[SettingsConstants.DisplayNameField] = account.Name;
            os[SettingsConstants.SecretKeyField] = account.SecretKey;
            os[SettingsConstants.AccountNumberField] = account.Number;

            if (account.IsGovCloudAccount)
                os[SettingsConstants.Restrictions] = "IsGovCloudAccount";
            else
                os[SettingsConstants.Restrictions] = null;


            PersistenceManager.Instance.SaveSettings(SettingsConstants.RegisteredAccounts, settings);
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
