using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ApplicationViewMetaNode : AbstractMetaNode, IApplicationViewMetaNode
    {
        public EnvironmentViewMetaNode EnvironmentViewMetaNode => this.FindChild<EnvironmentViewMetaNode>();

        public ActionHandlerWrapper.ActionHandler OnApplicationStatus
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDeleteApplication
        {
            get;
            set;
        }

        public override bool SupportsRefresh => true;

        private void OnDeleteApplicationResponse(IViewModel focus, ActionResults results)
        {
            if (results.Success)
            {
                ApplicationViewModel appModel = focus as ApplicationViewModel;
                appModel.ElasticBeanstalkRootViewModel.RemoveApplication(focus.Name);
            }
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("View Status", OnApplicationStatus, null, true, typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.ElasticBeanstalkApplication.Path),
                null,
                new ActionHandlerWrapper("Delete", OnDeleteApplication, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteApplicationResponse), false, null, "delete.png")
            );
    }
}
