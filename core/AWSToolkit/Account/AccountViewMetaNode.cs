using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

            if (results.GetParameter<bool>(EditAccountController.NAME_CHANGE_PARAMETER, false))
            {
                model.DisplayName = results.FocalName;
            }
            if (results.GetParameter<bool>(EditAccountController.CREDENTIALS_CHANGE_PARAMTER, false))
            {
                model.AccessKey = results.GetParameter<string>(EditAccountController.ACCESSKEY_PARAMETER, string.Empty);
                model.SecretKey = results.GetParameter<string>(EditAccountController.SECRETKEY_PARAMETER, string.Empty);
                model.FullReload(true);
            }
        }

        public override IList<ActionHandlerWrapper> Actions
        {
            get
            {
                return BuildActionHandlerList(
                    new ActionHandlerWrapper("Edit Profile...", OnEditAccount, new ActionHandlerWrapper.ActionResponseHandler(this.OnEditAccountResponse)),
                    new ActionHandlerWrapper("Unregister Profile", OnUnregisterAccount, new ActionHandlerWrapper.ActionResponseHandler(this.OnUnregisterAccountResponse)));
            }
        }
    }
}
