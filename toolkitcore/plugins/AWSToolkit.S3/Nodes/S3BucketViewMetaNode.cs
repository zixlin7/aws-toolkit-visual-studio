using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.S3.Nodes
{
    public class S3BucketViewMetaNode : AbstractMetaNode, IS3BucketViewMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnBrowse
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnEditPolicy
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnProperties
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnViewMultipartUploads
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnCreatePreSignedURL
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            S3BucketViewModel bucketModel = focus as S3BucketViewModel;
            bucketModel.S3RootViewModel.RemoveBucket(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Browse", OnBrowse, null, true, null, null)
                    {VisibilityHandler = VisibilityHandler},
                null,
                new ActionHandlerWrapper("View Multipart Uploads", OnViewMultipartUploads, null, false,
                        this.GetType().Assembly, "Amazon.AWSToolkit.S3.Resources.EmeddedImages.view-multiparts.png")
                    {VisibilityHandler = VisibilityHandler},
                new ActionHandlerWrapper("Create Pre-Signed URL...", OnCreatePreSignedURL, null, false, null, null)
                    {VisibilityHandler = VisibilityHandler},
                null,
                new ActionHandlerWrapper("Edit Policy", OnEditPolicy, null, false, null, "policy.png")
                    {VisibilityHandler = VisibilityHandler},
                null,
                new ActionHandlerWrapper("Delete", OnDelete,
                        new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null,
                        "delete.png")
                    {VisibilityHandler = VisibilityHandler},
                null,
                new ActionHandlerWrapper("Properties", OnProperties, null, false, null, "properties.png")
                    {VisibilityHandler = VisibilityHandler}
            );

        private Amazon.AWSToolkit.Navigator.ActionHandlerWrapper.ActionVisibility VisibilityHandler(IViewModel focus)
        {
            if (((S3BucketViewModel)focus).PendingDeletion)
            {
                return ActionHandlerWrapper.ActionVisibility.disabled;    
            }
            else
            {
                return ActionHandlerWrapper.ActionVisibility.enabled;
            }
            
        }
    }
}
