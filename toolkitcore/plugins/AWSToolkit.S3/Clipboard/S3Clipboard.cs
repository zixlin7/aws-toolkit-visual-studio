using System.Collections.Generic;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;

namespace Amazon.AWSToolkit.S3.Clipboard
{
    public class S3Clipboard
    {
        public enum ClipboardMode { Copy, Cut };

        public S3Clipboard(ClipboardMode mode, BucketBrowserController controller, string sourceRootFolder, List<BucketBrowserModel.ChildItem> itemsInClipboard)
        {
            this.Mode = mode;
            this.BucketBrowserController = controller;
            this.SourceRootFolder = sourceRootFolder;
            this.ItemsInClipboard = itemsInClipboard;
        }

        public ClipboardMode Mode
        {
            get;
            set;
        }

        public List<BucketBrowserModel.ChildItem> ItemsInClipboard
        {
            get;
            set;
        }

        public string SourceRootFolder
        {
            get;
            set;
        }

        public BucketBrowserController BucketBrowserController
        {
            get;
            set;
        }
    }
}
