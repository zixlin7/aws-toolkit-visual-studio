using System;
using System.Collections.Generic;

using Amazon.AWSToolkit.Credentials.Core;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// An entity being dragged and dropped from the S3 browser.
    /// Requests something to be downloaded (if dropped on a local folder).
    /// </summary>
    public class S3DragAndDropItem
    {
        /// <summary>
        /// Represents the type of object (eg: file or folder)
        /// See BucketBrowserModel.ChildType
        /// </summary>
        public string ItemType;

        /// <summary>
        /// Full path of S3 object to drag and drop
        /// </summary>
        public string Key;
    }

    /// <summary>
    /// A drag and drop request to download one or more objects from the S3 browser to a local destination.
    /// </summary>
    public class S3DragAndDropRequest
    {
        /// <summary>
        /// Uniquely identifies the drag and drop operation being performed
        /// </summary>
        public string RequestId { get; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Parent credentials + region that objects are being dragged from
        /// </summary>
        public AwsConnectionSettings ConnectionSettings { get; }

        /// <summary>
        /// Bucket being dragged from
        /// </summary>
        public string BucketName { get; }

        /// <summary>
        /// Root S3 path of the objects being dragged
        /// </summary>
        public string BaseBucketPath { get; }

        /// <summary>
        /// S3 Objects being dragged and dropped
        /// </summary>
        public IEnumerable<S3DragAndDropItem> Items { get; }

        public S3DragAndDropRequest(AwsConnectionSettings connectionSettings,
            string bucketName, string baseBucketPath,
            IEnumerable<S3DragAndDropItem> items)
        {
            ConnectionSettings = connectionSettings;
            BucketName = bucketName;
            BaseBucketPath = baseBucketPath;
            Items = items;
        }
    }
}
