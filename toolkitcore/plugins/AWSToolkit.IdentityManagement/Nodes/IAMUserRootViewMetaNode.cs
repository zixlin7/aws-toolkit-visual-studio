using System.Collections.Generic;

using Amazon.AWSToolkit.CommonUI.Images;
using Amazon.IdentityManagement.Model;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;


namespace Amazon.AWSToolkit.IdentityManagement.Nodes
{
    public class IAMUserRootViewMetaNode : AbstractMetaNode
    {
        public IAMUserViewMetaNode IAMUserViewMetaNode => this.FindChild<IAMUserViewMetaNode>();

        public override bool SupportsRefresh => true;

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

        public override IList<ActionHandlerWrapper> Actions => BuildActionHandlerList(new ActionHandlerWrapper("Create User...", OnCreateUser, new ActionHandlerWrapper.ActionResponseHandler(this.OnCreateUserResponse), false, typeof(AwsImageResourcePath).Assembly, AwsImageResourcePath.IamUser.Path));
    }
}
