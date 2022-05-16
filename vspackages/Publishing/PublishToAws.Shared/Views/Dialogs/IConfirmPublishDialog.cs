namespace Amazon.AWSToolkit.Publish.Views.Dialogs
{
    public interface IConfirmPublishDialog
    {
        string ProjectName { get; set; }
        string PublishDestinationName { get; set; }
        string RegionName { get; set; }
        string CredentialsId { get; set; }
        bool SilenceFutureConfirmations { get; set; }

        bool? ShowModal();
    }
}
