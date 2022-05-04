using System.Collections.Generic;
using System.Timers;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views.DesignData
{
    /// <summary>
    /// Populates design time views with sample data
    /// </summary>
    internal class DesignSamplePublishProjectViewModel : PublishProjectViewModel
    {
        public DesignSamplePublishProjectViewModel() : base(null)
        {
            StackName = "Some Stack Name";
            RecipeName = "Recipe Name";
            RegionName = "US West (Oregon)";

            PublishProgress = "this is\nsome publish output\nfoo...";
            PublishedArtifactId = "some-resource-id";

            PopulatePublishResources();

            // Cycle through states so that we can see the different presentation modes at design time
            var timer = new Timer(3333);
            timer.AutoReset = true;
            timer.Elapsed += (sender, args) =>
            {
                CyclePublishStates();
            };
            timer.Start();
        }

        private void PopulatePublishResources()
        {
            AddPublishResource();
            AddPublishResource();
        }

        private void AddPublishResource()
        {
            PublishResources.Add(new PublishResource("resource-id", "Sample Resource", "Sample resource description", new Dictionary<string, string>()
            {
                { "some key", "some value" },
                { "another key", "another value" },
                { "some url", "http://www.amazon.com" },
            }));
        }

        private void CyclePublishStates()
        {
            switch (ProgressStatus)
            {
                case ProgressStatus.Loading:
                    PopulateWithSuccess();
                    break;
                case ProgressStatus.Success:
                    PopulateWithFailure();
                    break;
                case ProgressStatus.Fail:
                    PopulateWithLoading();
                    break;
            }
        }

        private void PopulateWithLoading()
        {
            ProgressStatus = ProgressStatus.Loading;
            PublishProgress = "publish output\npublish in progress...";
            IsFailureBannerEnabled = false;
            IsPublishing = true;
        }

        private void PopulateWithSuccess()
        {
            ProgressStatus = ProgressStatus.Success;
            PublishProgress = "publish output\nSample: publish succeeded!";
            IsFailureBannerEnabled = false;
            IsPublishing = false;
        }

        private void PopulateWithFailure()
        {
            ProgressStatus = ProgressStatus.Fail;
            PublishProgress = "publish output\nSample: publish failed";
            IsFailureBannerEnabled = true;
            IsPublishing = false;
        }
    }
}
