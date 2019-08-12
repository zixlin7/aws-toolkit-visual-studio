using System;
using System.Collections.Generic;
using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;

using Amazon.AWSToolkit.Lambda.Controller;
using Amazon.AWSToolkit.Lambda.Nodes;
using Amazon.AWSToolkit.Lambda.Util;

namespace Amazon.AWSToolkit.Lambda
{
    public class LambdaActivator : AbstractPluginActivator, IAWSLambda
    {
        public override string PluginName => "Lambda";

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

        public void  UploadFunctionFromPath(Dictionary<string, object> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, object>();

            var controller = new UploadFunctionController();
            controller.UploadFunctionFromPath(seedProperties);
        }

        public void EnsureLambdaTesterConfigured(string projectPath)
        {
            LambdaTesterInstaller.Install(projectPath);
        }
    }
}
