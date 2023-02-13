using System;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// See S3DragAndDropHandler for details about the drag and drop system
    /// </summary>
    public interface IS3DragAndDropHandler : IDisposable
    {
        /// <summary>
        /// Gets the location of the proxy "drop source" manifest
        /// </summary>
        string GetDropSourcePath();

        /// <summary>
        /// Produces the proxy "drop source" manifest
        /// </summary>
        void WriteDropSourcePlaceholder();
    }
}
