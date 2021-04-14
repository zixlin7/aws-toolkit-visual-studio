using Amazon.AWSToolkit.Credentials.Core;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.Navigator.Node;

namespace Amazon.AWSToolkit.Account.Controller
{
    public class UnregisterAccountController : IContextCommand
    {
        private ICredentialIdentifier _identifier;
        private readonly ICredentialSettingsManager _credentialSettingsManager;

        public UnregisterAccountController(ICredentialSettingsManager credentialSettingsManager)
        {
            _credentialSettingsManager = credentialSettingsManager;
        }

        public ActionResults Execute(IViewModel model)
        {
            AccountViewModel account = model as AccountViewModel;
            _identifier = account?.Identifier;
            _credentialSettingsManager.DeleteProfile(account?.Identifier);
            return new ActionResults().WithSuccess(true);
        }
    }
}
