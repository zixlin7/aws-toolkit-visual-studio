using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Models
{
    public static class DisplayedResourceSummaryExtensionMethods
    {
        public static PublishResource AsPublishResource(this DisplayedResourceSummary resourceSummary)
        {
         return new PublishResource(resourceSummary.Id, resourceSummary.Type,
                resourceSummary.Description, resourceSummary.Data);
        }
    }
}
