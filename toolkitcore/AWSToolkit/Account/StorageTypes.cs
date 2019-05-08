using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace Amazon.AWSToolkit.Account
{
    public class StorageTypes
    {
        public static readonly StorageType DotNetEncryptedStore = new StorageType(".NET Encrypted Store", "NETEncryptedStore", 
            "Using the .NET encrypted store the AWS credentials for the profile will be stored encrypted. The profile will only " + 
            "be available to AWS .NET SDK and tooling.");
        public static readonly StorageType SharedCredentialsFile = new StorageType("Shared Credentials File", "SharedCredentualsFile",
            @"Using the shared credentials file the AWS credentials for the profile will be stored in the <home-directory>\.aws\credentials file. " + 
            "The profile will be accessible to all AWS sdks and tools.");

        public static readonly IList<StorageType> AllStorageTypes = new List<StorageType> { DotNetEncryptedStore, SharedCredentialsFile };

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
                private set;
            }

            public string SystemName
            {
                get;
                private set;
            }

            public string Description
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
                if (!(obj is StorageType))
                    return false;

                return string.Equals(this.SystemName, ((StorageType)obj).SystemName);
            }
        }
    }
}
