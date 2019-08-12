using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using log4net;

namespace Amazon.AWSToolkit.Account
{
    public class AccountTypes
    {
        private static readonly ILog LOGGER = LogManager.GetLogger(typeof(AccountTypes));

        public static IList<AccountType> AllAccountTypes
        {
            get
            {
                try
                {
                    XDocument xdoc;
                    var result = HostedFileContentLoader.Instance.LoadXmlContent(Constants.ACCOUNTTYPES_INFO_FILE, S3FileFetcher.CacheMode.PerInstance, out xdoc);
                    if (result != HostedFileContentLoadResult.Failed)
                    {
                        var query = from p in xdoc.Elements("account-types")
                                .Elements("account-type")
                            select new AccountType(p.Element("displayname").Value,
                                p.Element("systemname").Value);

                        return query.ToList<AccountType>();
                    }
                }
                catch (Exception e)
                {
                    LOGGER.Error("Exception parsing content for " + Constants.ACCOUNTTYPES_INFO_FILE, e);
                }

                return new List<AccountType> { new AccountType("Standard AWS Account", "") };
            }
        }

        public class AccountType
        {
            public AccountType(string displayName, string systemName)
            {
                this.DisplayName = displayName;
                this.SystemName = systemName;
            }

            public string DisplayName
            {
                get;
            }

            public string SystemName
            {
                get;
            }

            public override int GetHashCode()
            {
                return this.SystemName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is AccountType))
                    return false;

                return string.Equals(this.SystemName, ((AccountType)obj).SystemName);
            }
        }
    }
}
