using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Nodes;

namespace Amazon.AWSToolkit.Lambda
{
    public class LambdaActivator : AbstractPluginActivator, IAWSLambda
    {
        public override string PluginName
        {
            get { return "Lambda"; }
        }

        public override void RegisterMetaNodes()
        {
            var rootMetaNode = new LambdaRootViewMetaNode();
            var functionMetaNode = new LambdaFunctionViewMetaNode();

            rootMetaNode.Children.Add(functionMetaNode);
            setupContextMenuHooks(rootMetaNode);

            var accountMetaNode = ToolkitFactory.Instance.RootViewMetaNode.FindChild<AccountViewMetaNode>();
            accountMetaNode.Children.Add(rootMetaNode);
        }

        void setupContextMenuHooks(LambdaRootViewMetaNode rootNode)
        {
            rootNode.OnUploadFunction =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<UploadFunctionController>().Execute);


            rootNode.LambdaFunctionViewMetaNode.OnOpen =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<ViewFunctionController>().Execute);
            rootNode.LambdaFunctionViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteFunctionController>().Execute);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSLambda))
                return this as IAWSLambda;

            return null;
        }

        public void  UploadFunctionFromPath(Dictionary<string, string> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, string>();

            var controller = new UploadFunctionController();
            controller.UploadFunctionFromPath(seedProperties);
        }
    }
}
