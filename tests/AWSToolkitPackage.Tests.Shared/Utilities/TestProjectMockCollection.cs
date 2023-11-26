﻿#if VS2022_OR_LATER
using AwsToolkit.Vs.Tests.VsTestFramework;

using Xunit;

namespace AWSToolkitPackage.Tests.Utilities
{
    /// <summary>
    /// This is a hack -- xunit requires collection definitions to live in the same
    /// assembly as the test. So, we "extend" the real collection, for use in this assembly.
    ///
    /// See comments on <see cref="VsMockCollection"/> for additional context.
    /// </summary>
    [CollectionDefinition(CollectionName)]
    public class TestProjectMockCollection : VsMockCollection
    {
        public new const string CollectionName = nameof(TestProjectMockCollection);
    }
}
#endif
