using System;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.DragAndDrop;
using Amazon.AWSToolkit.S3.Nodes;

namespace Amazon.AWSToolkit.S3
{
    public class S3Activator : AbstractPluginActivator
    {
        public override string PluginName => "S3";

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new S3RootViewMetaNode(ToolkitContext);
            var bucketMetaNode = new S3BucketViewMetaNode();

            rootMetaNode.Children.Add(bucketMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IS3DragAndDropManager))
            {
                return new S3DragAndDropManager(ToolkitContext);
            }

            return base.QueryPluginService(serviceType);
        }

        void setupContextMenuHooks(S3RootViewMetaNode rootNode)
        {
            var shellProvider = ToolkitFactory.Instance.ShellProvider;

            rootNode.OnCreate =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new CreateBucketController(ToolkitContext, shellProvider)).Execute);

            rootNode.S3BucketViewMetaNode.OnBrowse =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new BucketBrowserController(ToolkitContext)).Execute);

            rootNode.S3BucketViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new DeleteBucketController(ToolkitContext, shellProvider)).Execute);

            rootNode.S3BucketViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new BucketPropertiesController(ToolkitContext)).Execute);

            rootNode.S3BucketViewMetaNode.OnViewMultipartUploads =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewMultipartUploadsController>().Execute);

            rootNode.S3BucketViewMetaNode.OnCreatePreSignedURL =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreatePresignedURLController>().Execute);
        }
    }
}
