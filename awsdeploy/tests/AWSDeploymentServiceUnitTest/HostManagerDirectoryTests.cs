using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManagerApp;

namespace MagicHarpServiceUnitTest
{
    /// <summary>
    /// Tests LatestHostManagerDirectory probing for Host Manager binaries
    /// </summary>
    [TestClass]
    public class HostManagerDirectoryTests
    {
        public HostManagerDirectoryTests()
        {
        }

        private DirectoryInfo _basePath = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        private List<string> _createdFolders = new List<string>();
        const string DATETIME_FORMAT_STRING = "yyyy-MM-ddTHH-mm-ssZ"; // can't use HH:MM:SS as folder name :-)

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        
        // Use TestInitialize to run code before running each test 
        //[TestInitialize()]
        //public void MyTestInitialize() { }
        
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup() 
        {
            foreach (string subFolder in _createdFolders)
            {
                DirectoryInfo subPath = new DirectoryInfo(Path.Combine(_basePath.FullName, subFolder));
                subPath.Delete(true);
            }

            _createdFolders.Clear();
        }
        
        #endregion

        /// <summary>
        /// Tests initial deployment case, where binaries exist in sole
        /// .\HostManager subfolder
        /// </summary>
        [TestMethod]
        public void TestInitialDeploymentScenario()
        {
            CreateTestDeploymentFolder("HostManager");

            string latestFolder = HostManagerApp.LatestHostManagerDirectory(_basePath.FullName);
            StringAssert.EndsWith(latestFolder, @"\HostManager", @"Expected \HostManager-suffixed return path.");
        }

        /// <summary>
        /// Test for initial update scenario, where we have replace old version in 
        /// .\HostManager with a new one in .\HostManager.DATETIME_FORMAT_STRING,
        /// which should get returned as latest.
        /// </summary>
        [TestMethod]
        public void TestInitialUpdateScenario()
        {
            CreateTestDeploymentFolder("HostManager");

            string latestRelease = "HostManager." + DateTime.UtcNow.ToString(DATETIME_FORMAT_STRING);
            CreateTestDeploymentFolder(latestRelease);

            string queriedLatest = HostManagerApp.LatestHostManagerDirectory(_basePath.FullName);
            StringAssert.EndsWith(queriedLatest, 
                                    @"\" + latestRelease, 
                                    @"Expected " + latestRelease + " return path, got " + queriedLatest);
        }

        /// <summary>
        /// Tests future multipole update scenario, where we have original .\HostManager
        /// version plus a number of .\HostManager.DATETIME_FORMAT_STRING subfolders
        /// representing updates over time.
        /// </summary>
        [TestMethod]
        public void TestMultipleUpdatesScenario()
        {
            CreateTestDeploymentFolder("HostManager");

            CreateTestDeploymentFolder("HostManager." + DateTime.UtcNow.AddHours(4).ToString(DATETIME_FORMAT_STRING));
            CreateTestDeploymentFolder("HostManager." + DateTime.UtcNow.AddDays(14).ToString(DATETIME_FORMAT_STRING));
            CreateTestDeploymentFolder("HostManager." + DateTime.UtcNow.AddMonths(3).ToString(DATETIME_FORMAT_STRING));

            string latestRelease = "HostManager." + DateTime.UtcNow.AddMonths(6).ToString(DATETIME_FORMAT_STRING);
            CreateTestDeploymentFolder(latestRelease);

            string queriedLatest = HostManagerApp.LatestHostManagerDirectory(_basePath.FullName);
            StringAssert.EndsWith(queriedLatest, 
                                  @"\" + latestRelease,
                                  @"Expected " + latestRelease + " return path, got " + queriedLatest);
        }

        void CreateTestDeploymentFolder(string folderName)
        {
            _basePath.CreateSubdirectory(folderName);
            _createdFolders.Add(folderName);
        }
    }
}
