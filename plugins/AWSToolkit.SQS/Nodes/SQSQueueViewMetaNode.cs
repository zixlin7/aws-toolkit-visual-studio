﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.SQS.Nodes
{
    public class SQSQueueViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnView
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnEditPolicy
        {
            get;
            set;
        }


        public ActionHandlerWrapper.ActionHandler OnPermissions
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
            SQSQueueViewModel queueModel = focus as SQSQueueViewModel;
            queueModel.SQSRootViewModel.RemovedQueue(queueModel);
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("View Queue", OnView, null, true, null, null),
                    new ActionHandlerWrapper("Permissions...", OnPermissions, null, false, null, null),
                    new ActionHandlerWrapper("Edit Policy", OnEditPolicy, null, false, null, "policy.png"),
                    null,
                    new ActionHandlerWrapper("Delete", OnDelete, new ActionHandlerWrapper.ActionResponseHandler(this.OnDeleteResponse), false, null, "delete.png")
                    );
            }
        }
    }
}
