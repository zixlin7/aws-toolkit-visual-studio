using System.Collections.Generic;
using System.Collections.ObjectModel;

using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.ViewModels;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Views.DesignData
{
    /// <summary>
    /// Populates design time views with sample data
    /// </summary>
    internal class DesignSamplePublishDocumentViewModel : PublishToAwsDocumentViewModel
    {
        public DesignSamplePublishDocumentViewModel()
            : base(null)
        {
            PublishDestination = new PublishRecommendation(new RecommendationSummary()
            {
                DeploymentType = DeploymentTypes.BeanstalkEnvironment,
            });

            PublishedArtifactId = "sample-artifact-id";

            PublishResources = new ObservableCollection<PublishResource>();
            AddPublishResource();
            AddPublishResource();
        }

        private void AddPublishResource()
        {
            PublishResources.Add(new PublishResource("resource-id", "Sample Resource", "Sample resource description", new Dictionary<string, string>()));
        }
    }
}
