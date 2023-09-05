using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;

using log4net;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard
{
    public abstract class StepViewModel : ViewModel
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(StepViewModel));

        protected IAddEditProfileWizard _addEditProfileWizard => ServiceProvider.RequireService<IAddEditProfileWizard>();
    }
}
