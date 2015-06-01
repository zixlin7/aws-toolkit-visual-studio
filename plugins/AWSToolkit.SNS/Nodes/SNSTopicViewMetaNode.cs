using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SNS.Nodes
{
    public class SNSTopicViewMetaNode : AbstractMetaNode, ISNSTopicViewMetaNode
    {

        public ActionHandlerWrapper.ActionHandler OnViewTopic
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnEditPolicy
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnDelete
        {
            get;
            set;
        }

        private void OnDeleteResponse(IViewModel focus, ActionResults results)
        {
            SNSTopicViewModel topicModel = focus as SNSTopicViewModel;
            topicModel.SNSRootViewModel.RemoveTopic(focus.Name);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View Topic", OnViewTopic, null, true, null,null),
                    new ActionHandlerWrapper("Edit Policy", OnEditPolicy, null, false, null, "policy.png"),
                    null,
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
                    );
            }
        }

    }
}
