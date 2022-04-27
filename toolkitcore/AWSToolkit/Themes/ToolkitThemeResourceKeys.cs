namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Toolkit Themed Resource Key Names that can be referenced from Xaml
    /// </summary>
    public static class ToolkitThemeResourceKeys
    {
        /// <summary>Key that references the "base" button style</summary>
        public static object ButtonStyle { get; } = GenerateKeyName(nameof(ButtonStyle));
        public static object ExpanderStyle { get; } = GenerateKeyName(nameof(ExpanderStyle));
        public static object ToolTipStyle { get; } = GenerateKeyName(nameof(ToolTipStyle));

        private static string GenerateKeyName(string baseName)
        {
            return $"AwsToolkitTheme{baseName}";
        }
    }
}
