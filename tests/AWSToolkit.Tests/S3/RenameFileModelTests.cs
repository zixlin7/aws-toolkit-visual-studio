
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using Amazon.AWSToolkit.S3.Model;

using Xunit;

namespace AWSToolkit.Tests.S3
{
    public class RenameFileModelTests
    {
        [Fact]
        public void NewFullPathKeyReturnsTheCorrectPathForSimpleFileNameRenaming()
        {
            string Rename(string originalPath, string newFileName)
            {
                var model = new RenameFileModel("BucketName", originalPath);
                model.NewFileName = newFileName;
                return model.NewFullPathKey;
            }

            Assert.Equal("to", Rename("from", "to"));
            Assert.Equal("/to", Rename("/from", "to"));
            Assert.Equal("dir/to", Rename("dir/from", "to"));
            Assert.Equal("/dir/to", Rename("/dir/from", "to"));
            Assert.Equal("/dir//to", Rename("/dir//from", "to"));
        }

        [Fact]
        public void NewFullPathKeyFailsWhenRenamingFromDirectoryOrRoot()
        {
            void AssertRenameFromDirectoryOrRootThrows(string originalPath, string newFileName)
            {
                Assert.Throws<ArgumentException>("fullPath", () =>
                {
                    var sut = new RenameFileModel("BucketName", originalPath);
                    sut.NewFileName = newFileName;
                });
            }

            AssertRenameFromDirectoryOrRootThrows("", "to");
            AssertRenameFromDirectoryOrRootThrows("from/", "to");
            AssertRenameFromDirectoryOrRootThrows("/", "to");
            AssertRenameFromDirectoryOrRootThrows("from//", "to");
            AssertRenameFromDirectoryOrRootThrows("//", "to");
        }

        [Fact]
        public void NewFullPathKeyFailsWhenRenamingToSlashes()
        {
            void AssertRenameToDirectoryOrRootIsError(string originalPath, string newFileName)
            {
                var sut = new RenameFileModel("BucketName", originalPath);
                sut.NewFileName = newFileName;
                Assert.Contains("Invalid filename.", ((INotifyDataErrorInfo) sut).GetErrors(null).OfType<object>());
            }

            AssertRenameToDirectoryOrRootIsError("from", "");
            AssertRenameToDirectoryOrRootIsError("from", "/");
            AssertRenameToDirectoryOrRootIsError("from", "//");
        }

        [Fact]
        public void NewFullPathKeyRemovesSlashesInNewFileName()
        {
            string Rename(string originalPath, string newFileName)
            {
                var model = new RenameFileModel("BucketName", originalPath);
                model.NewFileName = newFileName;
                return model.NewFullPathKey;
            }

            Assert.Equal("to", Rename("from", "to/"));
            Assert.Equal("to", Rename("from", "to//"));
            Assert.Equal("to", Rename("from", "/to"));
            Assert.Equal("to", Rename("from", "//to"));
        }
    }
}
