using System;
using System.Threading.Tasks;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using log4net;

namespace Amazon.AWSToolkit.Account
{
    /// <summary>
    /// The purpose of this class is to start to introduce a split between the toolkit's
    /// "current account" and the AWS Explorer's UI. For now, this class will be driven by
    /// the NavigatorControl.
    ///
    /// Over time, toolkit code should start to use AccountManager as the source.
    /// </summary>
    public class AccountManager
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AccountManager));

        private string _currentAccountId;

        public string CurrentAccountId
        {
            get => _currentAccountId;
            private set
            {
                _currentAccountId = value;
                OnCurrentAccountIdChanged();
            }
        }

        /// <summary>
        /// Raised whenever the Toolkit's current credentials have changed
        /// </summary>
        public event EventHandler CurrentAccountIdChanged;

        /// <summary>
        /// Sets the credentials currently used by the Toolkit
        /// </summary>
        public async Task SetCurrentAccount(AccountViewModel account)
        {
            try
            {
                await UpdateAccountId(account);
            }
            catch (Exception e)
            {
                LOGGER.Error(e);
            }
        }

        private async Task UpdateAccountId(AccountViewModel account)
        {
            try
            {
                if (account == null)
                {
                    CurrentAccountId = null;
                }
                else
                {
                    CurrentAccountId = await GetAccountId(account);
                }
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to get accountId", e);
                CurrentAccountId = null;
            }
        }

        /// <summary>
        /// Retrieves an AccountId for the given account
        /// </summary>
        /// <remarks>
        /// It is the caller's responsibility to catch Exceptions
        /// </remarks>
        public async Task<string> GetAccountId(AccountViewModel account)
        {
            var sts = new AmazonSecurityTokenServiceClient(account.Credentials, GetStsRegion(account));

            var response = await sts.GetCallerIdentityAsync(new GetCallerIdentityRequest());
            return response.Account;
        }

        private RegionEndpoint GetStsRegion(AccountViewModel account)
        {
            // TODO : This is not forwards compatible. When the Credentials system is revamped,
            // pull the associated default region/partition instead.

            if (account.Restrictions == null) return RegionEndpoint.USEast1;

            if (account.Restrictions.Contains("IsChinaAccount"))
            {
                return RegionEndpoint.CNNorth1;
            }

            if (account.Restrictions.Contains("IsGovCloudAccount"))
            {
                return RegionEndpoint.USGovCloudEast1;
            }

            return RegionEndpoint.USEast1;
        }

        private void OnCurrentAccountIdChanged()
        {
            CurrentAccountIdChanged?.Invoke(this, new EventArgs());
        }
    }
}
