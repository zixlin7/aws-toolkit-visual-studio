using Amazon.AWSToolkit.Publish.Models.Configuration;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Util
{
    public static class CategorySummaryExtensionMethods
    {
        public static Category AsCategory(this CategorySummary @this)
        {
            return new Category() { Id = @this.Id, DisplayName = @this.DisplayName, Order = @this.Order, };
        }
    }
}
