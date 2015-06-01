using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;
using Amazon.Runtime.Internal.Settings;
using Amazon.AWSToolkit.Account.View;
using Amazon.AWSToolkit.Account.Model;

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
            var settings = PersistenceManager.Instance.GetSettings(ToolkitSettingsConstants.RegisteredProfiles);
            settings.Remove(account.SettingsUniqueKey);
            PersistenceManager.Instance.SaveSettings(ToolkitSettingsConstants.RegisteredProfiles, settings);
            return new ActionResults().WithSuccess(true);
        }
    }
}
