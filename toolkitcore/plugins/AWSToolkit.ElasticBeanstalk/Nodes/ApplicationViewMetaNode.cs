using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.ElasticBeanstalk.Nodes
{
    public class ApplicationViewMetaNode : AbstractMetaNode, IApplicationViewMetaNode
    {
        public EnvironmentViewMetaNode EnvironmentViewMetaNode
        {
            get { return this.FindChild<EnvironmentViewMetaNode>(); }
        }

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

        public override bool SupportsRefresh
        {
            get
            {
                return true;
            }
        }

        private void OnDeleteApplicationResponse(IViewModel focus, ActionResults results)
        {
            if (results.Success)
            {
                ApplicationViewModel appModel = focus as ApplicationViewModel;
                appModel.ElasticBeanstalkRootViewModel.RemoveApplication(focus.Name);
            }
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View Status", OnApplicationStatus, null, true, this.GetType().Assembly, "Amazon.AWSToolkit.ElasticBeanstalk.Resources.EmbeddedImages.application_view.png"),
                    null,
                    new ActionHandlerWrapper("Delete", OnDeleteApplication, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteApplicationResponse), false, null, "delete.png")
                    );
            }
        }
    }
}
