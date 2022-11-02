using System;

using Amazon.AWSToolkit.Util;
using Amazon.CodeCatalyst.Model;

namespace Amazon.AWSToolkit.CodeCatalyst.Models
{
    internal class CodeCatalystAccessToken : ICodeCatalystAccessToken
    {
        internal const string _defaultAccountArtifactsId = "c7d59288-8df0-490f-99c8-4c07f1d219c4";

        public string Name { get; }

        public string Secret { get; }

        public DateTime ExpiresOn { get; }

        internal CodeCatalystAccessToken(string name, string secret, DateTime expiresOn)
        {
            Arg.NotNull(name, nameof(name));
            Arg.NotNull(secret, nameof(secret));

            Name = name;
            Secret = secret;
            ExpiresOn = expiresOn;
        }

        internal CodeCatalystAccessToken(CreateAccessTokenResponse response)
            : this(response.Name, response.Secret, response.ExpiresTime) { }

        internal CodeCatalystAccessToken(ServiceSpecificCredentials creds)
            : this(creds.Username, creds.Password, creds.ExpiresOn.Value) { }

        protected bool Equals(CodeCatalystAccessToken other)
        {
            return Name == other.Name && Secret == other.Secret && ExpiresOn.Equals(other.ExpiresOn);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != GetType())
            {
                return false;
            }

            return Equals((CodeCatalystAccessToken) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Name != null ? Name.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Secret != null ? Secret.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ ExpiresOn.GetHashCode();
                return hashCode;
            }
        }
    }
}
