using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Amazon.AWSToolkit.VisualStudio.Notification
{
    /// <summary>
    /// See the Notifications/VisualStudio folder in the internal hosted files repo
    /// </summary>
    public class NotificationModel
    {
        [JsonPropertyName("notifications")]
        public List<Notification> Notifications { get; set; }
    }

    public class Notification
    {
        // Represents a Unix timestamp 
        [JsonPropertyName("createdOn")]
        public long CreatedOn { get; set; }

        [JsonPropertyName("notificationId")]
        public string NotificationId { get; set; }

        [JsonPropertyName("content")]
        public Dictionary<string, string> Content { get; set; }

        [JsonPropertyName("actions")]
        public List<NotificationAction> Actions { get; set; }

        [JsonPropertyName("displayIf")]
        public DisplayIf DisplayIf { get; set; }
    }

    public class NotificationAction
    {
        [JsonPropertyName("gesture")]
        public string Gesture { get; set; }

        [JsonPropertyName("actionId")]
        public string ActionId { get; set; }

        [JsonPropertyName("displayText")]
        public Dictionary<string, string> DisplayText { get; set; }

        [JsonPropertyName("args")]
        public Dictionary<string, string> Args { get; set; }
    }

    public class DisplayIf
    {
        [JsonPropertyName("toolkitVersion")]
        public string ToolkitVersion { get; set; }

        [JsonPropertyName("comparison")]
        public string Comparison { get; set; }
    }

    public enum Gesture
    {
        Button,
        Link
    }

    public enum ActionContexts
    {
        None,
        ShowMarketplace,
        ShowUrl
    }

}
