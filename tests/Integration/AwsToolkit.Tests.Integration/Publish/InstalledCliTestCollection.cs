using Xunit;

namespace Amazon.AWSToolkit.Tests.Integration.Publish
{
    [CollectionDefinition(InstalledCliTestCollection.Name)]
    public class InstalledCliTestCollection : ICollectionFixture<DeployCliInstallationFixture>
    {
        public const string Name = "Tests that share an installed CLI";
    }
}
