using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.AWSToolkit.Tests.Common.IO;

using Xunit;

namespace Amazon.AWSToolkit.Util.Tests
{
    public class UtilityTests : IDisposable
    {
        private readonly TemporaryTestLocation _testLocation = new TemporaryTestLocation();
        private readonly DirectoryInfo _logDirectory;
        private const int LogFileRetentionMonths = 1;
        private const long MaxLogDirectorySizeInBytes = 5 * 1024 * 1024;

        public UtilityTests()
        {
            var logDirectoryPath = $@"{_testLocation.InputFolder}\logs\";
            _logDirectory = new DirectoryInfo(logDirectoryPath);
            Directory.CreateDirectory(logDirectoryPath);
        }

        public void Dispose()
        {
            _testLocation.Dispose();
        }


        [Fact]
        public void DeleteOldFiles_WhenWithinLimits()
        {
            SetupLogFiles(3, 1);
            var initialFiles = GetAllFiles();

            Utility.DeleteOldFiles(_logDirectory, LogFileRetentionMonths);

            var actualFiles = GetAllFiles();
            Assert.Equal(initialFiles.Count, actualFiles.Count);
            Assert.Equal(GetTotalSize(initialFiles), GetTotalSize(actualFiles));
        }


        [Fact]
        public void MaintainDirectorySize_WhenWithinLimits()
        {
            SetupLogFiles(3, 1);
            var initialFiles = GetAllFiles();

            Utility.MaintainDirectorySize(_logDirectory, MaxLogDirectorySizeInBytes);

            var actualFiles = GetAllFiles();
            Assert.Equal(initialFiles.Count, actualFiles.Count);
            Assert.Equal(GetTotalSize(initialFiles), GetTotalSize(actualFiles));
        }

        [Fact]
        public void MaintainDirectorySize_WhenSizeExceeds()
        {
            SetupLogFiles(7, 1);
            var initialFiles = GetAllFiles();

            Utility.MaintainDirectorySize(_logDirectory, MaxLogDirectorySizeInBytes);

            var actualFiles = GetAllFiles();
            Assert.True(MaxLogDirectorySizeInBytes > GetTotalSize(actualFiles));
            Assert.NotEqual(initialFiles.Count, actualFiles.Count);
        }

        [Fact]
        public void DeleteOlderFiles_WhenOlderFilesExist()
        {
            SetupOlderFiles(2);

            Utility.DeleteOldFiles(_logDirectory, LogFileRetentionMonths);

            var actualFiles = GetAllFiles();
            Assert.True(MaxLogDirectorySizeInBytes > GetTotalSize(actualFiles));
            Assert.Equal(2, actualFiles.Count);
        }

        private void SetupOlderFiles(int count)
        {
            SetupLogFiles(4, 1);
            MarkFilesAsOld(count);
        }

        /// <summary>
        ///  Mark the specified count of files to be older than retention time period
        /// </summary>
        /// <param name="count"></param>
        private void MarkFilesAsOld(int count)
        {
            var files = GetAllFiles();
            files.Take(count)
                .ToList()
                .ForEach(MarkAsOld);
        }

        /// <summary>
        /// Mark specified file to be older than retention time period
        /// </summary>
        /// <param name="file"></param>
        private void MarkAsOld(FileInfo file)
        {
            file.LastWriteTime = DateTime.Now.AddMonths(-1 * (LogFileRetentionMonths + 1));
        }

        private long GetTotalSize(List<FileInfo> files)
        {
            return files.Sum(fi => fi.Length);
        }

        private List<FileInfo> GetAllFiles()
        {
            return _logDirectory.GetFiles("*.*", SearchOption.AllDirectories).ToList();
        }

        /// <summary>
        /// Sets up number(specified by count) of sample log files of specified size
        /// </summary>
        private void SetupLogFiles(int count, int sizeInMb)
        {
            foreach (var i in Enumerable.Range(0, count))
            {
                var guid = Guid.NewGuid().ToString();
                var filepath = $"{_logDirectory}log-{guid}.txt";
                using (FileStream fileStream =
                       new FileStream(filepath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fileStream.SetLength(sizeInMb * 1024 * 1024);
                }
            }
        }
    }
}
