using System;
using System.IO;

using BuildTasks.Test.IO;
using BuildTasks.VersionManagement;

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BuildTasks.Test
{
    [TestClass]
    public class GetVersionTaskTests
    {
        private readonly TemporaryTestLocation _repositoryTestLocation = new TemporaryTestLocation();
        private string _jsonFilename;
        private GetVersionTask _sut;

        [TestInitialize]
        public void TestSetup()
        {
            _jsonFilename = Path.Combine(_repositoryTestLocation.TestFolder, "sample.json");
            _sut = new GetVersionTask
            {
                Filename = _jsonFilename,
            };
        }

        [TestCleanup]
        public void TestCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _repositoryTestLocation.Dispose();
        }

        [TestMethod]
        public void GetVersion()
        {
            WriteVersionToSampleJson("1.2.3.4");
            Assert.IsTrue(_sut.Execute());
            Assert.AreEqual("1.2.3.4", _sut.Version);
        }

        private void WriteVersionToSampleJson(string version)
        {
            File.WriteAllText(_jsonFilename, $"{{ \"version\": \"{version}\" }}");
        }
    }
}
