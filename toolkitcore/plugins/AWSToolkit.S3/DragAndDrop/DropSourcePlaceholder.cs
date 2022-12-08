using System;
using System.IO;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// Manages the temporary file representing the drop source (origin)
    /// data for a drag and drop operation originating from the S3 Browser.
    /// On disposal, this class will attempt to remove the file it created.
    /// </summary>
    internal class DropSourcePlaceholder : IDisposable
    {
        public string Path { get; }

        private bool _disposed = false;
        private bool _fileCreated = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="path">Path to the file this object should manage</param>
        public DropSourcePlaceholder(string path)
        {
            Path = path;
        }

        /// <summary>
        /// Writes the given manifest to this file, if it doesn't already exist.
        /// </summary>
        public void Create(S3DragDropManifest manifest)
        {
            if (_disposed)
            {
                throw new InvalidOperationException("Placeholder object has already been disposed");
            }

            if (File.Exists(Path)) { return; }

            using (var stream = File.Create(Path))
            using (var streamWriter = new StreamWriter(stream))
            {
                _fileCreated = true;

                streamWriter.Write(manifest.AsJson());
                streamWriter.Flush();
                stream.Flush();
            }
        }

        public void Dispose()
        {
            if (_disposed) { return; }
            _disposed = true;

            if (_fileCreated)
            {
                File.Delete(Path);
            }
        }
    }
}
