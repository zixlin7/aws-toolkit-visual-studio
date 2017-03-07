using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;
using Amazon.Runtime.CredentialManagement;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class UnregisterAccountController : IContextCommand
    {
        public UnregisterAccountController()
        {
        }

        public ActionResults Execute(IViewModel model)
        {
            AccountViewModel account = model as AccountViewModel;
            if (account.ProfileStore is NetSDKCredentialsFile)
            {
                var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
                settings.Remove(account.SettingsUniqueKey);
                PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);
            }
            else
            {
                var profileStore = new SharedCredentialsFile();
                profileStore.UnregisterProfile(account.Name);
            }

            return new ActionResults().WithSuccess(true);
        }
    }
}
