using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    /// <summary>
    /// The data object used by the drag and drop system to represent objects being dragged from the S3 browser.
    /// See <see cref="S3DragAndDropHandler"/> for S3 drag and drop details
    /// </summary>
    public class S3DataObject : IDataObject
    {
        private static readonly IList<string> _formats;

        private bool _dropSourceCreated = false;

        static S3DataObject()
        {
            _formats = new List<string>()
            {
                DataFormats.FileDrop
            };
        }

        private readonly IS3DragAndDropHandler _dragAndDropHandler;

        public S3DataObject(IS3DragAndDropHandler dragAndDropHandler)
        {
            _dragAndDropHandler = dragAndDropHandler;
        }

        public object GetData(string format)
        {
            // FileDrop - User is dragging S3 bucket objects and is over a component that accepts object drops
            if (DataFormats.FileDrop.Equals(format))
            {
                // Create a proxy drop source file (S3DragDropManifest) in case user decides to drop
                // the objects on a windows folder.
                // The proxy file is what will be copied into the folder, and is what triggers
                // (via file watcher - S3DropRequestEventArgs) downloading the selected objects from S3.
                //
                // GetData can be called many times, depending on what components/windows the user
                // drags objects over before dropping. Only create the drop source file once for this
                // drag and drop operation.
                if (!_dropSourceCreated)
                {
                    _dragAndDropHandler.WriteDropSourcePlaceholder();
                    _dropSourceCreated = true;
                }
                
                return new[] { _dragAndDropHandler.GetDropSourcePath() };
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
    }
}
