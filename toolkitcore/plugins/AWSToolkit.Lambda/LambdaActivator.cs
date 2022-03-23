using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
            // Called from the AWS Explorer (Lambda node) context menu to deploy a zip or folder containing code
            rootNode.OnUploadFunction =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(CreateUploadFunctionController).Execute);


            rootNode.LambdaFunctionViewMetaNode.OnOpen =
                new ActionHandlerWrapper.ActionHandler(new ContextCommandExecutor(() => new ViewFunctionController(ToolkitContext)).Execute);
            rootNode.LambdaFunctionViewMetaNode.OnDelete =
                new ActionHandlerWrapper.ActionHandler(new CommandInstantiator<DeleteFunctionController>().Execute);
        }

        public override object QueryPluginService(Type serviceType)
        {
            if (serviceType == typeof(IAWSLambda))
                return this as IAWSLambda;

            return null;
        }

        /// <summary>
        /// Called from the "Publish to AWS Lambda" context menu in the Solution Explorer.
        /// This is the main Lambda deployment path.
        /// </summary>
        public void UploadFunctionFromPath(Dictionary<string, object> seedProperties)
        {
            if (seedProperties == null)
                seedProperties = new Dictionary<string, object>();

            var controller = CreateUploadFunctionController();
            controller.UploadFunctionFromPath(seedProperties);
        }

        private UploadFunctionController CreateUploadFunctionController()
        {
            return new UploadFunctionController(ToolkitContext,
                ToolkitContext.ConnectionManager.ActiveCredentialIdentifier,
                ToolkitContext.ConnectionManager.ActiveRegion,
                ToolkitFactory.Instance.RootViewModel);
        }

        public async Task EnsureLambdaTesterConfiguredAsync(string projectPath)
        {
            await LambdaTesterInstaller.InstallAsync(projectPath);
        }
    }
}
