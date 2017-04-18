namespace Amazon.AWSToolkit.Shared
{
    public class GitIgnoreOption
    {
        public enum OptionType
        {
            VSToolkitDefault,
            Custom,
            None
        }

        public string DisplayText { get; set; }

        public OptionType GitIgnoreType { get; set; }

        public string CustomFilename { get; set; }
    }
}
