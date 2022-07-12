using System;
using System.Collections.Generic;
using System.Linq;

using Amazon.S3.IO;

using Xunit;
using Xunit.Sdk;

namespace AWSToolkit.Tests.S3
{
    public class S3PathTests
    {
        // Be sure to verify null paths return null, root returns root, directory names are always terminated with directory separators, and no-name directories are handled
        // See "Implementation Assumption(s)" comment in S3Path class file for details.

        // When writing tests:
        //   1. For roots, use string.Empty for input and S3Path.Root for expected.  Do not use S3Path.Root for both input and expected.
        //   2. Use [Theory] to test with multiple directory separators for any methods that accept a directorySeparator arg.
        //   3. No-named directories are terminated with a directory separator, use ds to represent them in tests, not string.Empty.
        //   4. Use the Paths class for inputs in all tests that require a path to ensure full coverage of possible path configurations.
        //   5. Use PathTestExecutor whenever possible.  Tests can contain additional local input paths/asserts for any paths that may be unique to that test only.
        //   6. When new path test cases are discovered, add them to PathTestExecutor and update all tests using it so that case is widely covered.
        //   7. Tests should define all input/expected values as string literals when possible.

        public static readonly object[][] DirectorySeparators =
        {
            new object[] {S3Path.DefaultDirectorySeparator}, new object[] {"//"}, new object[] {"BiG***$E/P/A/R/ATOR"}
        };

        public static readonly object[][] InvalidDirectorySeparators =
            S3Path.InvalidDirectorySeparators.Select(ps => new object[] {ps}).ToArray();

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void CombineWithDirectorySeparatorWorks(string directorySeparator)
        {
            // Combine() implicitly tested here when directorySeparator is DefaultDirectorySeparator
            var ds = directorySeparator;

            Assert.Null(S3Path.CombineWithDirectorySeparator(ds, null, null, null));
            Assert.Null(S3Path.CombineWithDirectorySeparator(ds, "a", null, "c"));

            Assert.Equal("file", S3Path.CombineWithDirectorySeparator(ds, "file"));
            Assert.Equal(ds, S3Path.CombineWithDirectorySeparator(ds, ds));
            Assert.Equal(S3Path.Root, S3Path.CombineWithDirectorySeparator(ds, string.Empty));
            Assert.Equal("file", S3Path.CombineWithDirectorySeparator(ds, string.Empty, "file"));
            Assert.Equal($"{ds}file", S3Path.CombineWithDirectorySeparator(ds, string.Empty, string.Empty, "file"));

            Assert.Equal($"a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, "a", "b", "c"));
            Assert.Equal($" a{ds} b {ds}c ", S3Path.CombineWithDirectorySeparator(ds, " a", " b ", "c "));
            Assert.Equal($"a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, $"a{ds}", $"b{ds}", "c"));
            Assert.Equal($"a{ds}b{ds}{ds}c", S3Path.CombineWithDirectorySeparator(ds, $"a", $"b{ds}{ds}", "c"));
            Assert.Equal($"{ds}a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, $"{ds}a", "b", "c"));
            Assert.Equal($"{ds}{ds}a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, $"{ds}{ds}a", "b", "c"));
            Assert.Equal($"a{ds}{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, "a", $"{ds}b", "c"));
            Assert.Equal($"a{ds}{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, "a", $"{ds}b{ds}", "c"));
            Assert.Equal($"a{ds}a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, "a", $"a{ds}b", "c"));
            Assert.Equal($"a{ds}b{ds}c", S3Path.CombineWithDirectorySeparator(ds, string.Empty, "a", "b", "c"));
            Assert.Equal($"a{ds}b{ds}c{ds}", S3Path.CombineWithDirectorySeparator(ds, string.Empty, "a", "b", $"c{ds}"));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetRelativePathWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                S3Path.Root,
                "file",
                $"{ds}file",
                $"{ds}{ds}file",
                "file",
                $"{ds}dir1{ds}file",
                $"dir2{ds}file",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}file",
                ds,
                $"{ds}{ds}",
                $"dir1{ds}",
                $"{ds}dir1{ds}",
                $"dir2{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}"
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetRelativePath($"dir1{ds}", testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetDirectoryPathWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                S3Path.Root,
                S3Path.Root,
                ds,
                $"{ds}{ds}",
                $"dir1{ds}",
                $"{ds}dir1{ds}",
                $"dir1{ds}dir2{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}",
                ds,
                $"{ds}{ds}",
                $"dir1{ds}",
                $"{ds}dir1{ds}",
                $"dir1{ds}dir2{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}"
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetDirectoryPath(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetFileNameWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                null,
                "file",
                "file",
                "file",
                "file",
                "file",
                "file",
                "file",
                null,
                null,
                null,
                null,
                null,
                null
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetFileName(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetParentPathWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                S3Path.Root,
                S3Path.Root,
                ds,
                $"{ds}{ds}",
                $"dir1{ds}",
                $"{ds}dir1{ds}",
                $"dir1{ds}dir2{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}",
                S3Path.Root,
                ds,
                S3Path.Root,
                ds,
                $"dir1{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}"
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetParentPath(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetPathComponentsWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            // Expected case is the same as the input case, so just initialize to all nulls
            new PathTestExecutor(
                ds,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null
            ).Execute(testCase =>
            {
                switch (testCase.Name)
                {
                    case PathTestExecutor.TestCase.NullName:
                        Assert.Empty(S3Path.GetPathComponents(testCase.Input, true, ds));
                        Assert.Empty(S3Path.GetPathComponents(testCase.Input, false, ds));
                        break;
                    case PathTestExecutor.TestCase.RootName:
                        Assert.Single(S3Path.GetPathComponents(testCase.Input, true, ds), S3Path.Root);
                        Assert.Empty(S3Path.GetPathComponents(testCase.Input, false, ds));
                        break;
                    default:
                        foreach (var includeRoot in new[] {true, false})
                        {
                            var components = S3Path.GetPathComponents(testCase.Input, includeRoot, ds).ToArray();

                            Assert.Equal(testCase.Input, string.Join(S3Path.Root, components));
                            Assert.Equal(includeRoot, S3Path.IsRoot(components[0]));
                            Assert.NotEmpty(components[components.Length - 1]);
                        }
                        break;
                }
            });
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetFirstNonRootPathComponentWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                S3Path.Root,
                "file",
                ds,
                ds,
                $"dir1{ds}",
                ds,
                $"dir1{ds}",
                ds,
                ds,
                ds,
                $"dir1{ds}",
                ds,
                $"dir1{ds}",
                ds
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetFirstNonRootPathComponent(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void GetLastPathComponentWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                S3Path.Root,
                "file",
                "file",
                "file",
                "file",
                "file",
                "file",
                "file",
                ds,
                ds,
                $"dir1{ds}",
                $"dir1{ds}",
                $"dir2{ds}",
                $"dir3{ds}"
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.GetLastPathComponent(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void IsDirectoryWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.IsDirectory(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void IsFileWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                false,
                false,
                true,
                true,
                true,
                true,
                true,
                true,
                true,
                false,
                false,
                false,
                false,
                false,
                false
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.IsFile(testCase.Input, ds)));
        }

        [Fact]
        public void IsRootWorks()
        {
            new PathTestExecutor(
                S3Path.DefaultDirectorySeparator,
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false,
                false
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.IsRoot(testCase.Input)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void IsDescendantWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                false,
                false,
                false,
                false,
                false,
                true,
                false,
                true,
                false,
                false,
                false,
                false,
                false,
                true,
                false
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.IsDescendant($"dir1{ds}", testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void ToDirectoryWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            new PathTestExecutor(
                ds,
                null,
                ds, // S3Path has no way to distinguish between root and no-named directory as both are empty strings
                $"file{ds}",
                $"{ds}file{ds}",
                $"{ds}{ds}file{ds}",
                $"dir1{ds}file{ds}",
                $"{ds}dir1{ds}file{ds}",
                $"dir1{ds}dir2{ds}file{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}file{ds}",
                ds,
                $"{ds}{ds}",
                $"dir1{ds}",
                $"{ds}dir1{ds}",
                $"dir1{ds}dir2{ds}",
                $"{ds}{ds}dir1{ds}dir2{ds}{ds}dir3{ds}"
            ).Execute(testCase => Assert.Equal(testCase.Expected, S3Path.ToDirectory(testCase.Input, ds)));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void TrimEndingDirectorySeparatorWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            Assert.Null(S3Path.TrimEndingDirectorySeparator(null, false, ds));
            Assert.Null(S3Path.TrimEndingDirectorySeparator(null, true, ds));

            Assert.Equal(S3Path.Root, S3Path.TrimEndingDirectorySeparator(string.Empty, false, ds));
            Assert.Equal(S3Path.Root, S3Path.TrimEndingDirectorySeparator(string.Empty, true, ds));

            Assert.Equal("file", S3Path.TrimEndingDirectorySeparator("file", false, ds));
            Assert.Equal("file", S3Path.TrimEndingDirectorySeparator("file", true, ds));
            Assert.Equal(string.Empty, S3Path.TrimEndingDirectorySeparator(ds, false, ds));
            Assert.Equal(string.Empty, S3Path.TrimEndingDirectorySeparator(ds, true, ds));
            Assert.Equal($"{ds}{ds}", S3Path.TrimEndingDirectorySeparator($"{ds}{ds}{ds}", false, ds));
            Assert.Equal(string.Empty, S3Path.TrimEndingDirectorySeparator($"{ds}{ds}{ds}", true, ds));

            Assert.Equal($"{ds}dir{ds}", S3Path.TrimEndingDirectorySeparator($"{ds}dir{ds}{ds}", false, ds));
            Assert.Equal($"{ds}dir", S3Path.TrimEndingDirectorySeparator($"{ds}dir{ds}{ds}", true, ds));
            Assert.Equal($"{ds}dir1{ds}dir2{ds}", S3Path.TrimEndingDirectorySeparator($"{ds}dir1{ds}dir2{ds}{ds}", false, ds));
            Assert.Equal($"{ds}dir1{ds}dir2", S3Path.TrimEndingDirectorySeparator($"{ds}dir1{ds}dir2{ds}{ds}", true, ds));
        }

        [Theory]
        [MemberData(nameof(DirectorySeparators))]
        public void TrimStartingDirectorySeparatorWorks(string directorySeparator)
        {
            var ds = directorySeparator;

            Assert.Null(S3Path.TrimStartingDirectorySeparator(null, false, ds));
            Assert.Null(S3Path.TrimStartingDirectorySeparator(null, true, ds));

            Assert.Equal(S3Path.Root, S3Path.TrimStartingDirectorySeparator(string.Empty, false, ds));
            Assert.Equal(S3Path.Root, S3Path.TrimStartingDirectorySeparator(string.Empty, true, ds));

            Assert.Equal("file", S3Path.TrimStartingDirectorySeparator("file", false, ds));
            Assert.Equal("file", S3Path.TrimStartingDirectorySeparator("file", true, ds));
            Assert.Equal(string.Empty, S3Path.TrimStartingDirectorySeparator(ds, false, ds));
            Assert.Equal(string.Empty, S3Path.TrimStartingDirectorySeparator(ds, true, ds));
            Assert.Equal($"{ds}{ds}", S3Path.TrimStartingDirectorySeparator($"{ds}{ds}{ds}", false, ds));
            Assert.Equal(string.Empty, S3Path.TrimStartingDirectorySeparator($"{ds}{ds}{ds}", true, ds));

            Assert.Equal($"{ds}dir{ds}", S3Path.TrimStartingDirectorySeparator($"{ds}{ds}dir{ds}", false, ds));
            Assert.Equal($"dir{ds}", S3Path.TrimStartingDirectorySeparator($"{ds}{ds}dir{ds}", true, ds));
            Assert.Equal($"{ds}dir1{ds}dir2{ds}", S3Path.TrimStartingDirectorySeparator($"{ds}{ds}dir1{ds}dir2{ds}", false, ds));
            Assert.Equal($"dir1{ds}dir2{ds}", S3Path.TrimStartingDirectorySeparator($"{ds}{ds}dir1{ds}dir2{ds}", true, ds));
        }

        [Theory]
        [MemberData(nameof(InvalidDirectorySeparators))]
        public void AllPublicMethodsThatAcceptDirectorySeparatorThrowsOnInvalidDirectorySeparators(string directorySeparator)
        {
            var ds = directorySeparator;

            Assert.Throws<ArgumentException>(() => S3Path.CombineWithDirectorySeparator(ds, ""));
            Assert.Throws<ArgumentException>(() => S3Path.GetDirectoryPath("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.GetFileName("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.GetLastPathComponent("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.GetParentPath("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.GetPathComponents("", true, ds));
            Assert.Throws<ArgumentException>(() => S3Path.IsDirectory("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.IsFile("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.ToDirectory("", ds));
            Assert.Throws<ArgumentException>(() => S3Path.TrimEndingDirectorySeparator("", false, ds));
            Assert.Throws<ArgumentException>(() => S3Path.TrimStartingDirectorySeparator("", false, ds));
        }

        private class PathTestExecutor
        {
            private readonly List<TestCase> _testCases = new List<TestCase>();

            public PathTestExecutor(
                string directorySeparator,

                // Special paths
                object @null,
                object root,

                // File paths (all should end with 'file')
                object file,
                object _file,
                object __file,
                object dir1_file,
                object _dir1_file,
                object dir1_dir2_file,
                object __dir1_dir2__dir3_file,

                // Directory paths (all should end with directory separator '_')
                object _,
                object __,
                object dir1_,
                object _dir1_,
                object dir1_dir2_,
                object __dir1_dir2__dir3_
            )
            {
                var ps = directorySeparator;

                // Special paths
                _testCases.Add(new TestCase(TestCase.NullName, ps, @null));
                _testCases.Add(new TestCase(TestCase.RootName, ps, root));

                // File paths (all should end with 'file')
                _testCases.Add(new TestCase("file", ps, file));
                _testCases.Add(new TestCase("_file", ps, _file));
                _testCases.Add(new TestCase("__file", ps, __file));
                _testCases.Add(new TestCase("dir1_file", ps, dir1_file));
                _testCases.Add(new TestCase("_dir1_file", ps, _dir1_file));
                _testCases.Add(new TestCase("dir1_dir2_file", ps, dir1_dir2_file));
                _testCases.Add(new TestCase("__dir1_dir2__dir3_file", ps, __dir1_dir2__dir3_file));

                // Directory paths (all should end with directory separator '_')
                _testCases.Add(new TestCase("_", ps, _));
                _testCases.Add(new TestCase("__", ps, __));
                _testCases.Add(new TestCase("dir1_", ps, dir1_));
                _testCases.Add(new TestCase("_dir1_", ps, _dir1_));
                _testCases.Add(new TestCase("dir1_dir2_", ps, dir1_dir2_));
                _testCases.Add(new TestCase("__dir1_dir2__dir3_", ps, __dir1_dir2__dir3_));
            }

            public void Execute(Action<TestCase> testExecutor)
            {
                foreach (var testCase in _testCases)
                {
                    try
                    {
                        testExecutor(testCase);
                    }
                    catch (XunitException ex)
                    {
                        var nl = Environment.NewLine;
                        throw new XunitException($"Path Test Case: {testCase.Name}{nl}Exception Type: {ex.GetType()}{nl}{nl}{ex.Message}");
                    }
                }
            }

            public class TestCase
            {
                // Special paths (should be in SCREAMING_SNAKE_CASE)
                public const string NullName = "NULL";

                public const string RootName = "ROOT";

                public TestCase(string name, string directorySeparator, object expected)
                {
                    Name = name;
                    Expected = expected;

                    switch (name)
                    {
                        case NullName:
                            Input = null;
                            break;
                        case RootName:
                            Input = string.Empty;
                            break;
                        default:
                            Input = name.Replace("_", directorySeparator);
                            break;
                    }
                }

                // Underscores in path test cases represent directory separator
                public string Name { get; }

                public string Input { get; }

                public object Expected { get; }
            }
        }
    }
}
