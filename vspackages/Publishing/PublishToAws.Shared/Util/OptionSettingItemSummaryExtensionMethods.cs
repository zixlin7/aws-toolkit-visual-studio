using System.Linq;

using AWS.Deploy.ServerMode.Client;

namespace Amazon.AWSToolkit.Publish.Util
{
    public static class OptionSettingItemSummaryExtensionMethods
    {
        public static bool HasValueMapping(this OptionSettingItemSummary itemSummary)
        {
            return itemSummary?.ValueMapping?.Any() ?? false;
        }

        public static bool HasAllowedValues(this OptionSettingItemSummary itemSummary)
        {
            return itemSummary?.AllowedValues?.Any() ?? false;
        }
    }
}
