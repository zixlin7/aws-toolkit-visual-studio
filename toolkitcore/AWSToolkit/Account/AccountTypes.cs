using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Amazon.AWSToolkit.Account
{
    public class AccountTypes
    {

        public static IList<AccountType> AllAccountTypes
        {
            get
            {
                try
                {

                    string content = S3FileFetcher.Instance.GetFileContent("AccountTypes.xml", S3FileFetcher.CacheMode.PerInstance);

                    XDocument xdoc = XDocument.Parse(content);
                    var query = from p in xdoc.Elements("account-types").Elements("account-type")
                                select new AccountType(p.Element("displayname").Value, p.Element("systemname").Value);

                    return query.ToList<AccountType>();
                }
                catch (Exception)
                {
                    return new List<AccountType> { new AccountType("Standard AWS Account", "") };
                }

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
                private set;
            }

            public string SystemName
            {
                get;
                private set;
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
