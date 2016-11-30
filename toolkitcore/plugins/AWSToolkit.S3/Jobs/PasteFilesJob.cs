using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.CommonUI.JobTracker;

using Amazon.AWSToolkit.S3.Clipboard;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;
using Amazon.AWSToolkit.S3.View;

using Amazon.S3;
using Amazon.S3.IO;
using Amazon.S3.Model;

namespace Amazon.AWSToolkit.S3.Jobs
{
    public class PasteFilesJob : QueueProcessingJob
    {
        BucketBrowserController _controller;

        S3Clipboard _clipboard;
        string _folderPathToPasteTo;

        HashSet<BucketBrowserModel.ChildItem> _itemsSuccessfullyCopied = new HashSet<BucketBrowserModel.ChildItem>();

        int _numberOfToCopiedKeys;
        int _numberOfCopiedKeys;

        public PasteFilesJob(BucketBrowserController controller,
            S3Clipboard clipboard, string folderPathToPasteTo)
        {
            this._controller = controller;
            this._clipboard = clipboard;
            this._folderPathToPasteTo = folderPathToPasteTo;

            this.Title = string.Format("{0} Files", this._clipboard.Mode.ToString());
        }

        protected override Queue<QueueProcessingJob.IJobUnit> BuildQueueOfJobUnits()
        {
            this.CurrentStatus = "Generating list of keys";

            Queue<QueueProcessingJob.IJobUnit> units = new Queue<IJobUnit>();
            foreach (var childItem in this._clipboard.ItemsInClipboard)
            {
                var unit = new PasteUnit(this, this._controller, childItem);
                this._numberOfToCopiedKeys += unit.CountOfKeys;
                units.Enqueue(unit);
            }

            this.CurrentStatus = string.Empty;
            this.Title = string.Format("{1} {0} File(s)", units.Count, this._clipboard.Mode.ToString());
            return units;
        }        

        protected override string CurrentStatusPostFix
        {
            get 
            {
                if(this._clipboard.Mode == S3Clipboard.ClipboardMode.Copy)
                    return "Copied files";

                return "Cut Files";
            }
        }

        protected override int TotalUnits
        {
            get
            {
                return this._numberOfToCopiedKeys;
            }
        }

        protected override int CompletedUnits
        {
            get
            {
                return this._numberOfCopiedKeys;
            }
        }

        protected override void PostExecuteJob(Exception exception)
        {
            if (exception == null)
                this._controller.AddChildItemsToModel(this._itemsSuccessfullyCopied.ToList());
            else
                this._controller.Refresh();

            if (this._clipboard.Mode == S3Clipboard.ClipboardMode.Cut &&
                this._controller != this._clipboard.BucketBrowserController)
            {
                if (exception == null)
                    this._clipboard.BucketBrowserController.RemoveChildItemsFromModel(this._clipboard.ItemsInClipboard);
                else
                    this._clipboard.BucketBrowserController.Refresh();
            }
        }

        void addCompletedKey(BucketBrowserModel.ChildItem childItem)
        {
            lock (this._itemsSuccessfullyCopied)
            {
                this._itemsSuccessfullyCopied.Add(childItem);
            }
        }

        public class PasteUnit : IJobUnit
        {
            PasteFilesJob _job;
            BucketBrowserController _controller;
            BucketBrowserModel.ChildItem _childItem;
            List<string> _keysToBeCopied;

            public PasteUnit(PasteFilesJob job, BucketBrowserController controller, BucketBrowserModel.ChildItem childItem)
            {
                this._job = job;
                this._controller = controller;
                this._childItem = childItem;

                List<BucketBrowserModel.ChildItem> childItems = new List<BucketBrowserModel.ChildItem>();
                childItems.Add(this._childItem);
                this._keysToBeCopied = this._controller.GetListOfKeys(childItems, true);
            }

            public int CountOfKeys
            {
                get { return this._keysToBeCopied.Count; }
            }

            public void Execute()
            {
                foreach (string key in this._keysToBeCopied)
                {
                    string destinationKey = createTranslateKeyToNewLocation(key);

                    if (this._job._clipboard.Mode == S3Clipboard.ClipboardMode.Cut)
                    {
                        S3File.Move(this._job._controller.S3Client, this._job._clipboard.BucketBrowserController.BucketName, key,
                                this._job._controller.BucketName, destinationKey);
                    }
                    else
                    {
                        S3File.Copy(this._job._controller.S3Client, this._job._clipboard.BucketBrowserController.BucketName, key,
                                this._job._controller.BucketName, destinationKey);
                    }

                    lock (this._job)
                    {
                        this._job._numberOfCopiedKeys++;
                    }
                }

                string newPath = createTranslateKeyToNewLocation(this._childItem.FullPath);

                
                var childItem = new BucketBrowserModel.ChildItem(newPath, this._childItem.ChildType, this._childItem.Size, DateTime.Now, S3StorageClass.Standard);
                this._job.addCompletedKey(childItem);
            }

            string createTranslateKeyToNewLocation(string key)
            {
                string destinationKey = this._job._folderPathToPasteTo;
                if (destinationKey.Length > 0 && !destinationKey.EndsWith("/"))
                    destinationKey += "/";


                if (key.StartsWith(this._job._clipboard.SourceRootFolder))
                {
                    string relativePath = key.Substring(this._job._clipboard.SourceRootFolder.Length);
                    if (relativePath.StartsWith("/"))
                        relativePath = relativePath.Substring(1);
                    destinationKey += relativePath;
                }
                else
                    destinationKey += S3File.GetName(key);

                return destinationKey;
            }
        }

    }
}
