using Amazon.AWSToolkit.CommonUI;
using Amazon.S3.IO;


namespace Amazon.AWSToolkit.S3.Model
{
    public class NewFolderModel : BaseModel
    {
        string _newFolderName;

        public NewFolderModel(string bucketName, string parentPath)
        {
            BucketName = bucketName;
            ParentPath = parentPath;
        }

        public string BucketName { get; }

        public string ParentPath { get; }

        public string NewFolderName
        {
            get => _newFolderName;
            set
            {
                _newFolderName = value.Replace(S3Path.DefaultDirectorySeparator, string.Empty);
                NotifyPropertyChanged("NewFolderName");
            }
        }
    }
}
