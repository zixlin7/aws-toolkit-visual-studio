using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BuildTasks.ChangeLog;
using BuildTasks.Test.IO;

namespace BuildTasks.Test
{
    [TestClass]
    public class ChangeLogGeneratorTests
    {
        private readonly TemporaryTestLocation _repositoryTestLocation = new TemporaryTestLocation();
        private string _nextReleasePath;
        private ChangeLogGeneratorTask _changelog;

        [TestInitialize]
        public void TestSetup()
        {
            _nextReleasePath = Path.Combine(_repositoryTestLocation.TestFolder, ".changes" ,"next-release");
            _changelog = new ChangeLogGeneratorTask
            {
                RepositoryRoot = _repositoryTestLocation.TestFolder,
               NextReleasePath = _nextReleasePath
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
        public void ValidChangeTypeInput()
        {
            var typeInput = new StringReader("2");
            Console.SetIn(typeInput);
            Assert.AreEqual("Feature", ChangeLogGeneratorTask.PromptChangeType());
        }

        [TestMethod]
        public void ValidChangeDescriptionInput()
        {
            var messageInput = new StringReader("Feature test");
            Console.SetIn(messageInput);
            Assert.AreEqual("Feature test", ChangeLogGeneratorTask.PromptChangeMessage());
        }

        [TestMethod]
        public void CancelChangeNoFileCreated()
        {
            var typeInput = new StringReader("0");
            Console.SetIn(typeInput);
            Assert.IsFalse(_changelog.Execute());
            Assert.IsFalse(Directory.Exists(_nextReleasePath));
        }

        [TestMethod]
        public void NewChangeFileCreated()
        {
            var typeInput = new StringReader("2\nhello");
            Console.SetIn(typeInput);
            Assert.IsTrue(_changelog.Execute());
            Assert.IsTrue(Directory.Exists(_nextReleasePath));

            var fileArray = Directory.GetFiles(_nextReleasePath);
            Assert.AreEqual(1, fileArray.Length);
            
            var fileContent = File.ReadAllText(fileArray.First());
            Assert.IsTrue(fileContent.Contains("hello") && fileContent.Contains("Feature"));
        }

        [TestMethod]
        public void WriteChangeToFile()
        {
            Assert.IsFalse(Directory.Exists(_nextReleasePath));
            ChangeLogGeneratorTask.WriteChangeToFile("Feature", "Testing feature change 2",_nextReleasePath);
            Assert.IsTrue(Directory.Exists(_nextReleasePath));
            Assert.AreEqual(1, Directory.GetFiles(_nextReleasePath).Length);

            var fileArray = Directory.GetFiles(_nextReleasePath);
            var fileContent = File.ReadAllText(fileArray.First());

            Assert.AreEqual(1, fileArray.Length);
            Assert.IsTrue(fileContent.Contains("Feature") && fileContent.Contains("Testing feature change 2"));
        }
    }
}