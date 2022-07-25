using System;

using Amazon.AWSToolkit.CommonUI;
using Amazon.S3.IO;

namespace Amazon.AWSToolkit.S3.Model
{
    public class RenameFileModel : BaseModel
    {
        private string _newFileName;

        public RenameFileModel(string bucketName, string fullPath)
        {
            if (S3Path.IsRoot(fullPath) || S3Path.IsDirectory(fullPath))
            {
                throw new ArgumentException($"Argument '{fullPath}' is invalid.  Cannot rename root or directory.", nameof(fullPath));
            }

            BucketName = bucketName;
            OrignalFullPathKey = fullPath;
            NewFileName = S3Path.GetFileName(fullPath);
        }

        public string BucketName { get; }

        public string OrignalFullPathKey { get; }

        public string NewFileName
        {
            get => _newFileName;
            set
            {
                _newFileName = value.Replace(S3Path.DefaultDirectorySeparator, string.Empty);

                DataErrorInfo.ClearErrors(nameof(NewFileName));
                if (!S3Path.IsFile(_newFileName))
                {
                    DataErrorInfo.AddError("Invalid filename.", nameof(NewFileName));
                }

                NotifyPropertyChanged(nameof(NewFileName));
            }
        }

        public string NewFullPathKey => S3Path.Combine(S3Path.GetDirectoryPath(OrignalFullPathKey), NewFileName);
    }
}
