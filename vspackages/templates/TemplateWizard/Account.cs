using System;
using System.Collections.Generic;
using System.Text;

namespace TemplateWizard
{
    public class Account : IEquatable<Account>
    {
        private static Account emtpy = new Account(string.Empty);
        public static Account Empty { get { return emtpy; } }

        public string UniqueKey { get; set; }
        public string Name { get; set; }
        public string AccessKey { get; set; }
        public string SecretKey { get; set; }
        public string Number { get; set; }
        public bool IsGovCloudAccount { get; set; }

        public Account(string uniqueKey)
        {
            UniqueKey = uniqueKey;
            Name = AccessKey = SecretKey = Number = string.Empty;
        }

        public bool IsValid
        {
            get
            {
                return
                    !string.IsNullOrEmpty(Name) &&
                    !string.IsNullOrEmpty(AccessKey) &&
                    !string.IsNullOrEmpty(SecretKey);
            }
        }

        #region IEquatable<Account> Members

        public bool Equals(Account other)
        {
            if (other == null) return false;

            return
                string.Equals(this.Name ?? string.Empty, other.Name ?? string.Empty) &&
                string.Equals(this.AccessKey ?? string.Empty, other.AccessKey ?? string.Empty) &&
                string.Equals(this.SecretKey ?? string.Empty, other.SecretKey ?? string.Empty) &&
                string.Equals(this.Number ?? string.Empty, other.Number ?? string.Empty);
        }

        #endregion
    }
}
