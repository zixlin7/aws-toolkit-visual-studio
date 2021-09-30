using System.Windows;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Toolkit Themed Resource Key Names that can be referenced from Xaml
    /// </summary>
    public static class ThemeResourceKeys
    {
        /// <summary>Gets the key that can be used to get the <see cref="Thickness"/> to use for input controls.</summary>
        public static object InputPaddingKey { get; } = GenerateKeyName(nameof(InputPaddingKey));

        private static string GenerateKeyName(string baseName)
        {
            return $"AwsToolkit{baseName}";
        }
    }
}
