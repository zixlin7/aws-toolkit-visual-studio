using System;
using System.Linq;

namespace Amazon.AWSToolkit.CodeArtifact.Utils
{
    public class CodeArtifactUri
    {
        public string Domain { get; }

        public string DomainOwner { get; }

        public string Region { get; }

        public Uri CodeArtifactEndpoint { get; }

        private CodeArtifactUri(Uri codeArtifactEndpoint, string domain, string domainOwner, string region)
        {
            CodeArtifactEndpoint = codeArtifactEndpoint;
            this.Domain = domain;
            this.DomainOwner = domainOwner;
            this.Region = region;
        }

        // Valid URLs: "https://testrepo-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json"
        // https://testrepo-test-123-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json"

        // Invalid URL: "https://api.nuget.org/v3/index.json"
        public static bool TryParse(Uri uri, out CodeArtifactUri codeArtifactUri)
        {
            codeArtifactUri = null;
            
            var hostTokens = uri.Host.Split('.');
            if (hostTokens.Length < 5)
                return false;

            if (!string.Equals(hostTokens[2], "codeartifact", StringComparison.OrdinalIgnoreCase))
                return false;

            var domainToken = hostTokens[0];
            var separatorIndex = domainToken.LastIndexOf('-');
            if (separatorIndex < 0)
            {
                return false;
            }
            var domainOwner = domainToken.Substring(separatorIndex + 1);
            var domain = domainToken.Substring(0, separatorIndex);
            var region = hostTokens[3];
            codeArtifactUri = new CodeArtifactUri(uri, domain, domainOwner, region);
            return true;
        }
    }
}
