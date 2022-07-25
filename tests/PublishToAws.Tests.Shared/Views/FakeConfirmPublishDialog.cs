using Amazon.AWSToolkit.Publish.Views.Dialogs;

namespace Amazon.AWSToolkit.Tests.Publishing.Views
{
    public class FakeConfirmPublishDialog : IConfirmPublishDialog
    {
        public string ProjectName { get; set; }
        public string PublishDestinationName { get; set; }
        public string RegionName { get; set; }
        public string CredentialsId { get; set; }
        public bool SilenceFutureConfirmations { get; set; }

        public bool ShowModalResult { get; set; } = true;

        public bool? ShowModal()
        {
            return ShowModalResult;
        }
    }
}
