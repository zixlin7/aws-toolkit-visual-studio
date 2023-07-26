namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Toolkit Themed Resource Key Names that can be referenced from Xaml
    /// </summary>
    public static class ToolkitThemeResourceKeys
    {
        /// <summary>Key that references the "base" button style</summary>
        public static object ButtonStyle { get; } = GenerateKeyName(nameof(ButtonStyle));
        public static object FlowDocumentStyle { get; } = GenerateKeyName(nameof(FlowDocumentStyle));
        public static object ExpanderStyle { get; } = GenerateKeyName(nameof(ExpanderStyle));
        public static object ListBoxStyle { get; } = GenerateKeyName(nameof(ListBoxStyle));
        public static object ListViewColumnHeaderStyle { get; } = GenerateKeyName(nameof(ListViewColumnHeaderStyle));
        public static object ListViewItemStyle { get; } = GenerateKeyName(nameof(ListViewItemStyle));
        public static object ListViewStyle { get; } = GenerateKeyName(nameof(ListViewStyle));
        public static object ToolTipStyle { get; } = GenerateKeyName(nameof(ToolTipStyle));

        private static string GenerateKeyName(string baseName)
        {
            return $"AwsToolkitTheme{baseName}";
        }
    }
}
