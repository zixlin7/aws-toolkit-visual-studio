using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.S3.Model
{
    public class RenameFileModel : BaseModel
    {
        string _bucketName;
        string _newFileName;
        string _orignalFullPathKey;

        public RenameFileModel(string bucketName, string fullPath)
        {
            this._bucketName = bucketName;
            this._orignalFullPathKey = fullPath;

            int pos = fullPath.LastIndexOf('/');
            if (pos != -1 && (pos + 1) < fullPath.Length)
                this.NewFileName = fullPath.Substring(pos + 1);
            else
                this.NewFileName = fullPath;
        }

        public string BucketName
        {
            get { return this._bucketName; }
        }

        public string OrignalFullPathKey
        {
            get { return this._orignalFullPathKey; }
        }

        public string NewFileName
        {
            get { return this._newFileName; }
            set
            {
                this._newFileName = value;
                this.NotifyPropertyChanged("NewFileName");
            }
        }

        public string NewFullPathKey
        {
            get
            {
                int pos = this.OrignalFullPathKey.LastIndexOf('/');
                string newKey = string.Empty;
                if (pos > 0)
                {
                    newKey = this.OrignalFullPathKey.Substring(0, pos + 1);
                }

                string newFile = this.NewFileName;
                if (newFile.StartsWith("/"))
                    newFile = newFile.Substring(1);
                newKey += newFile;

                return newKey;
            }
        }
    }
}
