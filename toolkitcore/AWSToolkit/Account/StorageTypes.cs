using System.Collections.Generic;

namespace Amazon.AWSToolkit.Account
{
    public class StorageTypes
    {
        public static readonly StorageType DotNetEncryptedStore = new StorageType(".NET Encrypted Store", "NETEncryptedStore",
            "Using the .NET encrypted store, the profile's AWS credentials will be stored using encryption. The profile will only " +
            "be available to the AWS .NET SDK and AWS .NET tooling.");
        public static readonly StorageType SharedCredentialsFile = new StorageType("Shared Credentials File", "SharedCredentialsFile",
            @"Using the shared credentials file, the profile's AWS credentials will be stored in the <home-directory>\.aws\credentials file. " +
            "The profile will be accessible to all AWS SDKs and tools.");

        public static readonly IList<StorageType> AllStorageTypes = new List<StorageType> { SharedCredentialsFile, DotNetEncryptedStore };

        public class StorageType
        {
            public StorageType(string displayName, string systemName, string description)
            {
                this.DisplayName = displayName;
                this.SystemName = systemName;
                this.Description = description;
            }

            public string DisplayName
            {
                get;
            }

            public string SystemName
            {
                get;
            }

            public string Description
            {
                get;
            }

            public override int GetHashCode()
            {
                return this.SystemName.GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (!(obj is StorageType))
                    return false;

                return string.Equals(this.SystemName, ((StorageType)obj).SystemName);
            }
        }
    }
}
