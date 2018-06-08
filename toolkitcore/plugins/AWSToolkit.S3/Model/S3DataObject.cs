using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Navigator.Node;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.Nodes;

namespace Amazon.AWSToolkit.S3.Model
{
    public class S3DataObject : IDataObject
    {
        const string MESSAGE = "Drag and drop only supports dragging from AWSToolkit to a local drive in Windows Explorer.  Network drives will not work and will leave this file around.";
        public const string S3_CHILDITEMS = "S3_CHILDITEMS";
        static List<string> _formats;

        static S3DataObject()
        {
            _formats = new List<string>();
            _formats.Add(DataFormats.FileDrop);
            _formats.Add(S3_CHILDITEMS);
        }

        S3RootViewModel _s3RootViewModel;
        string _tempFileLocation;
        string _bucket;
        string _relativePath;
        List<BucketBrowserModel.ChildItem> _childItems;

        public S3DataObject(S3RootViewModel s3RootViewModel, string bucket, string relativePath, List<BucketBrowserModel.ChildItem> childItems)
        {
            this._s3RootViewModel = s3RootViewModel;
            this._bucket = bucket;
            this._relativePath = relativePath;
            this._childItems = childItems;
        }

        public object GetData(string format)
        {
            if (DataFormats.FileDrop.Equals(format))
            {
                if (this._tempFileLocation == null)
                    writeTempFile();

                return new[] { this._tempFileLocation };
            }

            return null;
        }

        public object GetData(Type format)
        {
            return null;
        }

        public object GetData(string format, bool autoConvert)
        {
            return GetData(format);
        }

        public bool GetDataPresent(string format)
        {
            return _formats.Contains(format);
        }

        public bool GetDataPresent(Type format)
        {
            return false;
        }

        public bool GetDataPresent(string format, bool autoConvert)
        {
            return GetDataPresent(format);
        }

        public string[] GetFormats()
        {
            return _formats.ToArray();
        }

        public string[] GetFormats(bool autoConvert)
        {
            return _formats.ToArray();
        }

        #region Stub SetData
        public void SetData(object data)
        {
        }

        public void SetData(string format, object data)
        {
        }

        public void SetData(Type format, object data)
        {
        }

        public void SetData(string format, object data, bool autoConvert)
        {
        }
        #endregion

        void writeTempFile()
        {
            this._tempFileLocation = Path.GetTempPath() + @"\" + ManifestWatcher.MANIFEST_FILE_NAME;

            using (StreamWriter writer = new StreamWriter(this._tempFileLocation))
            {
                writer.WriteLine(MESSAGE);
                writer.WriteLine(ManifestWatcher.Instance.INSTANCE_IDENTIFIER);

                var s3Config = new Amazon.S3.AmazonS3Config();
                this._s3RootViewModel.CurrentEndPoint.ApplyToClientConfig(s3Config);

                writer.WriteLine(this._s3RootViewModel.AccountViewModel.SettingsUniqueKey);
                writer.WriteLine(s3Config.ServiceURL);

                writer.WriteLine(this._bucket);
                writer.WriteLine(this._relativePath);
                foreach (var item in this._childItems)
                {
                    writer.WriteLine(string.Format("{0}\t{1}", item.ChildType, item.FullPath));
                }
                writer.Flush();
            }
        }
    }
}
