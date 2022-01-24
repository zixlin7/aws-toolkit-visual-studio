namespace Amazon.AWSToolkit.Models
{
    public class KeyValueConversion
    {
        /// <summary>
        /// Converts a KeyValue object to a "key=value" string
        /// </summary>
        public static string ToAssignmentString(KeyValue keyValue)
        {
            return $"{keyValue.Key.Trim()}={keyValue.Value}";
        }

        /// <summary>
        /// Converts a "key=value" string to a KeyValue object
        /// </summary>
        public static KeyValue FromAssignmentString(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            var splitPos = text.IndexOf('=');
            if (splitPos == -1)
            {
                splitPos = text.Length;
            }

            var key = text.Substring(0, splitPos);
            var value = splitPos >= text.Length ? string.Empty : text.Substring(splitPos + 1);

            return new KeyValue(key, value);
        }
    }
}
