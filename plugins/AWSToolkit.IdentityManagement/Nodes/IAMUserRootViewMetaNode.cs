using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.IdentityManagement.Model;

using Amazon.AWSToolkit.Account;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMUserRootViewMetaNode : AbstractMetaNode
    {
        public IAMUserViewMetaNode IAMUserViewMetaNode
        {
            get { return this.FindChild<IAMUserViewMetaNode>(); }
        }

        public override bool SupportsRefresh
        {
            get { return true; }
        }

        public ActionHandlerWrapper.ActionHandler OnCreateUser
        {
            get;
            set;
        }

        public void OnCreateUserResponse(IViewModel focus, ActionResults results)
        {
            IAMUserRootViewModel rootModel = focus as IAMUserRootViewModel;
            object user;
            if (results.Parameters.TryGetValue(IAMActionResultsConstants.PARAM_IAM_USER, out user) && user is User)
            {
                rootModel.AddUser((User)user);
            }
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(new ActionHandlerWrapper("Create User...", OnCreateUser, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateUserResponse), false, this.GetType().Assembly, "Amazon.AWSToolkit.IdentityManagement.Resources.EmbeddedImages.user_add.png"));
            }
        }
    }
}
