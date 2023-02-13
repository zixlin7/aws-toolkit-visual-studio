using Amazon.AWSToolkit.Context;

namespace Amazon.AWSToolkit.S3.DragAndDrop
{
    internal class S3DragAndDropManager : IS3DragAndDropManager
    {
        private readonly ToolkitContext _toolkitContext;

        public S3DragAndDropManager(ToolkitContext toolkitContext)
        {
            _toolkitContext = toolkitContext;
        }

        public IS3DragAndDropHandler Register(S3DragAndDropRequest dragDropRequest)
        {
            return new S3DragAndDropHandler(dragDropRequest, _toolkitContext);
        }
    }
}
