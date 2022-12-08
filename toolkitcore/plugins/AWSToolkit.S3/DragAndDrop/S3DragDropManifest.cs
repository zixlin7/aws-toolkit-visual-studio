using System.IO;

using Newtonsoft.Json;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// Manifest representing the proxy contents being dragged and dropped from a S3 Bucket.
    /// </summary>
    internal class S3DragDropManifest
    {
        /// <summary>
        /// The unique identifier of the drag and drop operation being performed.
        /// Used to handle the operation of interest.
        /// </summary>
        public string RequestId { get; set; }

        /// <summary>
        /// General explanation written into the file so that someone knows what it is.
        /// </summary>
        public string[] Description = new[]
        {
            "This file is created by the AWS Toolkit for Visual Studio when objects are dragged from a S3 Bucket browser to a Windows Explorer window.",
            "This file should be automatically cleaned up once the file transfer starts.",
            "Dragging objects to a network location is not supported, and this file may not get automatically removed.",
            "If you see this file, it is safe to delete."
        };

        public string AsJson()
        {
            var serializerSettings = new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
            };

            return JsonConvert.SerializeObject(this, serializerSettings);
        }

        internal static S3DragDropManifest Load(string path)
        {
            return JsonConvert.DeserializeObject<S3DragDropManifest>(File.ReadAllText(path));
        }
    }
}
