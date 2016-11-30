using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Nodes;

namespace Amazon.AWSToolkit.S3
{
    public class S3Activator : AbstractPluginActivator
    {
        public override string PluginName
        {
            get { return "S3"; }
        }

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new S3RootViewMetaNode();
            var bucketMetaNode = new S3BucketViewMetaNode();

            rootMetaNode.Children.Add(bucketMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(S3RootViewMetaNode rootNode)
        {
            rootNode.OnCreate =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreateBucketController>().Execute);

            rootNode.S3BucketViewMetaNode.OnBrowse =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<BucketBrowserController>().Execute);

            rootNode.S3BucketViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteBucketController>().Execute);

            rootNode.S3BucketViewMetaNode.OnProperties =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<BucketPropertiesController>().Execute);

            rootNode.S3BucketViewMetaNode.OnViewMultipartUploads =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewMultipartUploadsController>().Execute);

            rootNode.S3BucketViewMetaNode.OnCreatePreSignedURL =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<CreatePresignedURLController>().Execute);
        }
    }
}
