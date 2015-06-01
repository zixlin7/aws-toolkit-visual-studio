using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using AWSDeploymentHostManager.Tasks;

namespace AWSDeploymentUnitTest
{
    [TestClass]
    public class SelfUpdateTest
    {
        const string DATETIME_FORMAT_STRING = "yyyy-MM-ddTHH-mm-ssZ";
        const string HM_FOLDERNAME_TEMPLATE = "HostManager.{0}";

        DirectoryInfo testExecDir = new DirectoryInfo(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

        // may as well use a real archive so we can also test content is as expected
        string _folderOffsetToHMArchive = @"..\..\..\AWSBeanstalkHostManager\bin\";

        string ZipArchive
        {
            get
            {
                string zipPath = Path.Combine(testExecDir.FullName, _folderOffsetToHMArchive);
#if DEBUG
                zipPath += @"Debug\";
#else
                zipPath += @"Release\";
#endif
                zipPath += "AWSBeanstalkHostManager.zip";

                // if you pass a ..\ relativized name to Shell32.NameSpace, we fail to bind :-(
                FileInfo fi = new FileInfo(zipPath);
                return fi.FullName;
            }
        }

        [TestMethod]
        public void TestArchiveExtraction()
        {
            string versionFolderSuffix = DateTime.UtcNow.ToString(DATETIME_FORMAT_STRING);
            DirectoryInfo targetDir = testExecDir.CreateSubdirectory(string.Format(HM_FOLDERNAME_TEMPLATE, versionFolderSuffix));

            SelfUpdateTask.decompressArchive(ZipArchive, targetDir.FullName);
            targetDir.Refresh();

            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "AWSBeanstalkHostManager.exe")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "AWSBeanstalkHostManager.exe.config")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "AWSBeanstalkHostManager.pdb")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "Interop.Shell32.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "log4net.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "Microsoft.Web.Administration.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "Microsoft.Web.Deployment.dll")));
            Assert.IsTrue(File.Exists(Path.Combine(targetDir.FullName, "System.Data.SQLite.dll")));
        }
    }
}
