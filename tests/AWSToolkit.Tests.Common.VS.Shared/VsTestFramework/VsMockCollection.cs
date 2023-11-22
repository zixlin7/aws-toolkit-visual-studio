#if VS2022_OR_LATER
using Microsoft.VisualStudio.Sdk.TestFramework;

using Xunit;

namespace AwsToolkit.Vs.Tests.VsTestFramework
{
    /// <summary>
    /// Collection fixture providing access to mocked VS services in unit tests.
    /// VS 2022 is the minimum supported version to use this.
    /// </summary>
    /// <remarks>
    /// This collection helps write tests around code that use JoinableTaskFactory and portions of the VS SDK.
    /// Without this, tests that use JoinableTaskFactory will fail with an "assembly not found" message.
    /// This collection allows tests to work with ThreadHelper.JoinableTaskContext.
    ///
    /// To use,
    /// - Create a class in your test assembly that derives from <see cref="VsMockCollection"/>,
    ///   and annotate it with [CollectionDefinition(CollectionName)].
    ///   xunit requires collection definitions to live in the same assembly as the test.
    ///   See VsMockCollection in AwsToolkit.CodeWhisperer.Tests.Shared for an example.
    /// - add a Collection annotation to your test class
    ///   [Collection(VsMockCollection.CollectionName)]
    /// - pass in the required fixture(s) to the ctor
    /// - call the GlobalServiceProvider object's Reset method.
    ///   this stubs in some VS SDK objects.
    /// 
    /// The use of a collection fixture is how the vssdktestfx library prescribes implementing this functionality.  While there 
    /// is a similar MockedVS class in the library, the toolkit uses its own class as it doesn't use the build constants defined 
    /// in the <see href="https://github.com/microsoft/vssdktestfx/blob/main/src/Microsoft.VisualStudio.Sdk.TestFramework.Xunit/contentFiles/MockedVS.cs">MockedVS</see> class. 
    ///
    /// Collection fixtures do carry the risk of maintaining state between tests for all classes in the test collection.  
    /// Tests in a collection cannot be run in parallel. 
    /// </remarks>
    /// <seealso href="https://github.com/microsoft/vssdktestfx/blob/main/src/Microsoft.VisualStudio.Sdk.TestFramework.Xunit/README.md">Using CollectionAttribute for VS mocked services</seealso>
    /// <seealso href="https://github.com/Microsoft/vs-threading/blob/main/doc/testing_vs.md">Using JoinableTaskFactory in unit tests</seealso>
    /// <seealso href="https://xunit.net/docs/shared-context">XUnit Shared Context between Tests</seealso>
    [CollectionDefinition(CollectionName)]
    public class VsMockCollection :
        ICollectionFixture<GlobalServiceProvider>,
        ICollectionFixture<MefHostingFixture>
    {
        public const string CollectionName = nameof(VsMockCollection);
    }
}
#endif
