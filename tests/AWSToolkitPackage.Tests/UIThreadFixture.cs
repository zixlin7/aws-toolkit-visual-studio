using System;
using Microsoft.VisualStudio.Shell;
using Xunit;

namespace AWSToolkitPackage.Tests
{
    public class UIThreadFixture : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;

        public UIThreadFixture()
        {
            // Accessing ServiceProvider.GlobalProvider makes the exercised code believe it is running on the UI Thread
            _serviceProvider = ServiceProvider.GlobalProvider;
        }

        public void Dispose()
        {
            _serviceProvider.Dispose();
        }
    }

    // All tests in a session use the same UI fixture. Otherwise disparate test classes fail when run together.
    [CollectionDefinition(CollectionName)]
    public class UIThreadFixtureCollection : ICollectionFixture<UIThreadFixture>
    {
        public const string CollectionName = "Test Needs UI Thread";

        // This class has no code, and is never created. Its purpose is simply
        // to be the place to apply [CollectionDefinition] and all the
        // ICollectionFixture<> interfaces.
    }
}