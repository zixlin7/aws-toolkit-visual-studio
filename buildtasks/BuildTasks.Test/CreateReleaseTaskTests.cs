using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTasks.ChangeLog;
using BuildTasks.Test.IO;

namespace BuildTasks.Test
{
    [TestClass]
    public class CreateReleaseTaskTests
    {
        private readonly TemporaryTestLocation _repositoryTestLocation = new TemporaryTestLocation();
        private string _nextReleasePath;
        private CreateReleaseTask _createReleaseTask;

        [TestInitialize]
        public void TestSetup()
        {
            _nextReleasePath = Path.Combine(_repositoryTestLocation.TestFolder, ".changes", "next-release");
            _createReleaseTask = new CreateReleaseTask
            {
                ReleaseVersion = "1.2.3.4",
                RepositoryRoot = _repositoryTestLocation.TestFolder,
                ChangeDirectoryPath = Path.Combine(_repositoryTestLocation.TestFolder, ".changes"),
                NextReleasePath = _nextReleasePath,
                ReleaseNotesPath = Path.Combine(_repositoryTestLocation.TestFolder, "vspackages",
                    "AWSToolkitPackage", "ReleaseNotes.txt"),
                ChangeLogPath = Path.Combine(_repositoryTestLocation.TestFolder, "CHANGELOG.md")
            };
            Directory.CreateDirectory(Directory.GetParent(_createReleaseTask.ReleaseNotesPath).FullName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            _repositoryTestLocation.Dispose();
        }

        [TestMethod]
        public void NoChangeFilesCreatedForRelease()
        {
            Assert.IsFalse(_createReleaseTask.Execute());
            Assert.IsFalse(Directory.Exists(_nextReleasePath));
        }

        [TestMethod]
        public void AlreadyExistingReleaseFile()
        {
            ChangeLogGeneratorTask.WriteChangeToFile("Feature", "Testing feature change 1", _nextReleasePath);
            var jsonFilePath = Path.Combine(_createReleaseTask.RepositoryRoot, ".changes", "1.2.3.4.json");
            File.Create(jsonFilePath);
            Assert.IsFalse(_createReleaseTask.Execute());
        }

        [TestMethod]
        public void ValidateChangeLogObjectsRetrieved()
        {
            WriteChangeToFileHelper();
            var changeLogList = _createReleaseTask.RetrieveChangeLogObjects();
            Assert.AreEqual(changeLogList.Count, 2);
            Assert.AreEqual(changeLogList[0].Type, "Feature");
            Assert.AreEqual(changeLogList[0].Description, "Testing feature change 1");
        }

        [TestMethod]
        public void ValidJsonFileWritten()
        {
            WriteChangeToFileHelper();
            var releaseLog = _createReleaseTask.CreateChangeLogVersion();
            var jsonFilePath = Path.Combine(_nextReleasePath, $"{_createReleaseTask.ReleaseVersion}.json");
            CreateReleaseTask.WriteJsonReleaseVersion(releaseLog, jsonFilePath);
            var fileContent = File.ReadAllText(jsonFilePath);

            Assert.IsTrue(File.Exists(jsonFilePath));
            Assert.IsTrue(fileContent.Contains("Feature") && fileContent.Contains("Test") &&
                          fileContent.Contains(releaseLog.Date));
        }

        [TestMethod]
        public void ValidChangeAndReleaseNotesWritten()
        {
            WriteChangeToFileHelper();
            var releaseLog = _createReleaseTask.CreateChangeLogVersion();
            _createReleaseTask.UpdateChangeLogAndReleaseNotes(releaseLog);

            Assert.IsTrue(File.Exists(_createReleaseTask.ReleaseNotesPath));
            Assert.IsTrue(File.Exists(_createReleaseTask.ChangeLogPath));
            var releaseFileContent = File.ReadAllText(_createReleaseTask.ReleaseNotesPath);
            var changeFileContent = File.ReadAllText(_createReleaseTask.ChangeLogPath);

            Assert.IsTrue(releaseFileContent.Contains(
                $"v{_createReleaseTask.ReleaseVersion} ({releaseLog.Date}){Environment.NewLine}* Testing feature change 1"));
            Assert.IsTrue(changeFileContent.Contains(
                $"## {_createReleaseTask.ReleaseVersion} ({releaseLog.Date}){Environment.NewLine}{Environment.NewLine}- **Feature** Testing feature change 1"));
        }

        [TestMethod]
        public void CreateFirstReleaseNotesFile()
        {
            WriteChangeToFileHelper();
            var jsonFilePath = Path.Combine(_createReleaseTask.RepositoryRoot, ".changes", "1.2.3.4.json");

            Assert.IsTrue(_createReleaseTask.Execute());
            Assert.IsTrue(File.Exists(Path.Combine(_createReleaseTask.RepositoryRoot, "CHANGELOG.md")));

            Assert.IsTrue(File.Exists(jsonFilePath));
            Assert.IsTrue(File.Exists(_createReleaseTask.ReleaseNotesPath));

            var releaseFileContent = File.ReadAllText(_createReleaseTask.ReleaseNotesPath);
            var jsonFileContent = File.ReadAllText(jsonFilePath);

            Assert.IsTrue(releaseFileContent.Contains($"v{_createReleaseTask.ReleaseVersion}") &&
                          releaseFileContent.Contains("Testing feature change 1"));
            Assert.IsTrue(jsonFileContent.Contains("Feature") && jsonFileContent.Contains("Test"));
        }

        [TestMethod]
        public void AppendReleaseNotesFile()
        {
            ChangeLogGeneratorTask.WriteChangeToFile("Feature", "Testing feature change 1", _nextReleasePath);
            var jsonFilePath = Path.Combine(_createReleaseTask.RepositoryRoot, ".changes/1.2.3.4.json");

            Assert.IsTrue(_createReleaseTask.Execute());
            Assert.IsTrue(File.Exists(jsonFilePath));
            Assert.IsTrue(File.Exists(_createReleaseTask.ReleaseNotesPath));
            Assert.AreEqual(1, Directory.GetFiles(_createReleaseTask.ChangeDirectoryPath).Length);

            var releaseFileInfoInitial = new FileInfo(_createReleaseTask.ReleaseNotesPath);
            var initialLength = releaseFileInfoInitial.Length;

            ChangeLogGeneratorTask.WriteChangeToFile("Breaking Change", "testing breaking change 1", _nextReleasePath);
            var jsonFilePath2 = Path.Combine(_createReleaseTask.RepositoryRoot, ".changes/1.2.3.5.json");
            _createReleaseTask.ReleaseVersion = "1.2.3.5";

            Assert.IsTrue(_createReleaseTask.Execute());

            //check if release notes has new version
            var releaseFileInfoFinal = new FileInfo(_createReleaseTask.ReleaseNotesPath);
            var finalLength = releaseFileInfoFinal.Length;
            var releaseFileContent = File.ReadAllText(_createReleaseTask.ReleaseNotesPath);

            Assert.IsTrue(File.Exists(jsonFilePath2));
            Assert.IsTrue((finalLength - initialLength) > 0);
            Assert.AreEqual(2, Directory.GetFiles(_createReleaseTask.ChangeDirectoryPath).Length);
            Assert.IsTrue(releaseFileContent.Contains("v1.2.3.5") &&
                          releaseFileContent.Contains("testing breaking change 1"));
        }

        private void WriteChangeToFileHelper()
        {
            ChangeLogGeneratorTask.WriteChangeToFile("Feature", "Testing feature change 1", _nextReleasePath);
            ChangeLogGeneratorTask.WriteChangeToFile("Test", "Testing 2", _nextReleasePath);
        }
    }
}