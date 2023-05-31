using System.Collections.Generic;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

using Amazon.AWSToolkit.Account.Controller;

namespace Amazon.AWSToolkit.Account
{
    public class AccountViewMetaNode : AbstractMetaNode
    {
        public ActionHandlerWrapper.ActionHandler OnEditAccount
        {
            get;
            set;
        }

        public ActionHandlerWrapper.ActionHandler OnUnregisterAccount
        {
            get;
            set;
        }

        private void OnUnregisterAccountResponse(IViewModel focus, ActionResults results)
        {
            AccountViewModel model = focus as AccountViewModel;
            AWSViewModel parentModel = model.Parent as AWSViewModel;

            parentModel.Refresh();
        }

        private void OnEditAccountResponse(IViewModel focus, ActionResults results)
        {
            AccountViewModel model = focus as AccountViewModel;
            AWSViewModel parentModel = model.Parent as AWSViewModel;

            if (!results.Success)
                return;

            // TODO IDE-10814 Update when refactoring EditAccountController
            if (results.GetParameter<bool>(LegacyEditAccountController.NAME_CHANGE_PARAMETER, false))
            {
                model.DisplayName = results.FocalName;
            }
            if (results.GetParameter<bool>(LegacyEditAccountController.CREDENTIALS_CHANGE_PARAMETER, false))
            {
                model.ReloadFromPersistence(model.DisplayName);
                model.FullReload(true);
            }
        }

        public override IList<ActionHandlerWrapper> Actions =>
            BuildActionHandlerList(
                new ActionHandlerWrapper("Edit Profile...", OnEditAccount, new ActionHandlerWrapper.ActionResponseHandler(this.OnEditAccountResponse)),
                new ActionHandlerWrapper("Unregister Profile", OnUnregisterAccount, new ActionHandlerWrapper.ActionResponseHandler(this.OnUnregisterAccountResponse)));
    }
}
