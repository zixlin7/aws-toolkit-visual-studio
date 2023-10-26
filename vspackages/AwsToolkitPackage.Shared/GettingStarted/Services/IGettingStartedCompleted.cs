namespace Amazon.AWSToolkit.VisualStudio.GettingStarted.Services
{
    public interface IGettingStartedCompleted
    {
        bool? Status { get; set; }

        string CredentialTypeName { get; set; }

        string CredentialName { get; set; }
    }
}
