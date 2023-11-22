using Amazon.AWSToolkit.CommonUI.CredentialProfiles.AddEditWizard.Services;

namespace Amazon.AWSToolkit.VisualStudio.GettingStarted.Services
{
    public enum GettingStartedStep
    {
        AddEditProfileWizards,
        GettingStartedCompleted
    }

    public interface IGettingStarted
    {
        GettingStartedStep CurrentStep { get; set; }

        FeatureType FeatureType { get; set; }
    }
}
