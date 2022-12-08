namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    public interface IS3DragAndDropManager
    {
        /// <summary>
        /// Produces a handler capable of fulfilling the S3 bucket objects being dragged to a windows explorer folder
        /// </summary>
        IS3DragAndDropHandler Register(S3DragAndDropRequest dragDropRequest);
    }
}
