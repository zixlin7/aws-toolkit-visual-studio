using Xunit;

using BaseVsMockCollection = AwsToolkit.Vs.Tests.VsTestFramework.VsMockCollection;

namespace Amazon.AwsToolkit.CodeWhisperer.Tests.TestUtilities
{
    /// <summary>
    /// This is a hack -- xunit requires collection definitions to live in the same
    /// assembly as the test. So, we "extend" the real collection, for use in this assembly.
    /// </summary>
    [CollectionDefinition(CollectionName)]
    public class VsMockCollection : BaseVsMockCollection
    {
        public new const string CollectionName = nameof(VsMockCollection);
    }
}
