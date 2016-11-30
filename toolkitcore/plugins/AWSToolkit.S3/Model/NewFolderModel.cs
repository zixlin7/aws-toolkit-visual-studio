using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;


namespace Amazon.AWSToolkit.S3.Model
{
    public class NewFolderModel : BaseModel
    {
        string _bucketName;
        string _parentPath;
        string _newFolderName;

        public NewFolderModel(string bucketName, string parentPath)
        {
            this._bucketName = bucketName;
            this._parentPath = parentPath;
        }

        public string BucketName
        {
            get { return this._bucketName; }
        }

        public string ParentPath
        {
            get { return this._parentPath; }
        }

        public string NewFolderName
        {
            get { return this._newFolderName; }
            set
            {
                this._newFolderName = value;
                this.NotifyPropertyChanged("NewFolderName");
            }
        }
    }
}
