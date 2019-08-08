using Amazon.KeyManagementService.Model;

namespace Amazon.AWSToolkit.Lambda.Model
{
    public class KeyAndAliasWrapper
    {
        public const string MyKeysCategoryName = "My Keys";
        public const string ServiceKeysCategoryName = "Service-provided Keys";

        private const string AliasPrefix = "alias/";

        public static readonly KeyAndAliasWrapper LambdaDefaultKMSKey
            = new KeyAndAliasWrapper(new KeyListEntry(),   // use empty id/arn fields as they'll appear in UI otherwise
                                     new AliasListEntry { AliasName = "(default) aws/lambda" },
                                     ServiceKeysCategoryName);

        public KeyListEntry Key { get; private set; }

        public AliasListEntry Alias { get; private set; }

        public string GroupCategoryName { get; set; }

        public bool IsServiceKeyDefault(string serviceKeyAlias)
        {
            return Alias != null && Alias.AliasName.EndsWith(serviceKeyAlias, System.StringComparison.OrdinalIgnoreCase);
        }

        public string AliasName
        {
            get
            {
                if (Alias == null)
                {
                    return string.Empty;
                }
                // emulate console and rip off any alias/ prefix
                var aliasName = Alias.AliasName;
                if (aliasName.StartsWith(AliasPrefix))
                {
                    return aliasName.Substring(AliasPrefix.Length);
                }

                return aliasName;
            }
        }

        public string FormattedDisplayName

        {
            get
            {
                var aliasName = AliasName;
                if (string.IsNullOrEmpty(aliasName)) { 
                    return Key.KeyArn;
                }

                if (string.IsNullOrEmpty(Key.KeyArn))
                {
                    return aliasName;
                }

                return $"{this.AliasName} ({this.Key.KeyArn})";
            }
        }

        internal KeyAndAliasWrapper(KeyListEntry key, AliasListEntry alias, string groupCategory)
        {
            Key = key;
            Alias = alias;
            GroupCategoryName = groupCategory;
        }

        internal KeyAndAliasWrapper(KeyListEntry key, AliasListEntry alias) : this(key, alias, MyKeysCategoryName)
        {
        }

        internal KeyAndAliasWrapper(KeyListEntry key) : this(key, null)
        {
        }
    }
}
