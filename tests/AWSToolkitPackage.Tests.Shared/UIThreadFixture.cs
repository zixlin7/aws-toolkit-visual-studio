using System.Reflection;

using Microsoft.VisualStudio.Shell;

using Xunit;

namespace AWSToolkitPackage.Tests
{
    public class UIThreadFixture
    {
        public UIThreadFixture()
        {
            DefineUiThread();
        }

        /// <summary>
        /// Make the test code think that it is running on the UI thread.
        /// Required for tests exercising code that calls ThreadHelper.ThrowIfNotOnUIThread.
        /// </summary>
        private static void DefineUiThread()
        {
            // HACK : This fixture used to call the ServiceProvider.GlobalProvider getter in order to
            // keep ThreadHelper.ThrowIfNotOnUIThread from throwing. It turns out that the implementation
            // ultimately calls ThreadHelper.SetUIThread, which is what the tests relied on in order to pass.
            //
            // ThreadHelper.SetUIThread essentially takes the current thread and declares it as the UI thread.
            // 
            // ThreadHelper.ThrowIfNotOnUIThread is an internal static method, so we use reflection to locate and invoke it.
            const string methodName = "SetUIThread";

            var threadHelperType = typeof(ThreadHelper);
            var setUiThread = threadHelperType.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);

            setUiThread.Invoke(null, new object[] { });
        }
    }

    // All tests in a session use the same UI fixture. Otherwise disparate test classes can fail when run concurrently.
    [CollectionDefinition(CollectionName)]
    public class UIThreadFixtureCollection : ICollectionFixture<UIThreadFixture>
    {
        public const string CollectionName = "Test Needs UI Thread";

        // This class has no code, and is never created.
        // Its purpose is to contain the [CollectionDefinition] declaration,
        // and any ICollectionFixture<> interfaces.
        // See https://xunit.net/docs/shared-context#collection-fixture for details.
    }
}
