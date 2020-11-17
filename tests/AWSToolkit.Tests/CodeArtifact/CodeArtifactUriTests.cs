using Amazon.AWSToolkit.CodeArtifact.Utils;
using System;
using Xunit;

namespace AWSToolkit.Tests.CodeArtifact
{
    public class CodeArtifactUriTests
    {
        [Theory]
        [InlineData("https://testrepo-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json", "testrepo", "123412341234", "us-west-2")]
        [InlineData("https://testrepo-test-123-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json", "testrepo-test-123", "123412341234", "us-west-2")]
        [InlineData("https://api.nuget.org/v3/index.json", null, null, null)]
        public void TestResourceExtraction(string uri, string domain, string domainOwner, string region)
        {
            if (CodeArtifactUri.TryParse(new Uri(uri), out var codeArtifactUri))
            {
                Assert.NotNull(domain);

                Assert.Equal(domain, codeArtifactUri.Domain);
                Assert.Equal(domainOwner, codeArtifactUri.DomainOwner);
                Assert.Equal(region, codeArtifactUri.Region);
            }
            else
            {
                Assert.Null(domain);
            }
        }

        [Theory]
        [InlineData("https://testrepo-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json", true)]
        [InlineData("https://testrepo-test-123-123412341234.d.codeartifact.us-west-2.amazonaws.com/nuget/test-repo/v3/index.json", true)]
        [InlineData("https://api.nuget.org/v3/index.json", false)]
        public void TestUri(string uri, bool isValid)
        {
            Assert.Equal(CodeArtifactUri.TryParse(new Uri(uri), out var codeArtifactUri), isValid);

        }
    }
}