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

        // VS2019... resource keys support use of brush keys that are not part of the VSSDK in VS2019. See ToolkitThemeBrushKeys
        public static object VS2019ErrorBackgroundBrushKey { get; } = GenerateKeyName(nameof(VS2019ErrorBackgroundBrushKey));
        public static object VS2019ErrorBorderBrushKey { get; } = GenerateKeyName(nameof(VS2019ErrorBorderBrushKey));
        public static object VS2019ErrorTextBrushKey { get; } = GenerateKeyName(nameof(VS2019ErrorTextBrushKey));
        public static object VS2019SuccessBackgroundBrushKey { get; } = GenerateKeyName(nameof(VS2019SuccessBackgroundBrushKey));
        public static object VS2019SuccessBorderBrushKey { get; } = GenerateKeyName(nameof(VS2019SuccessBorderBrushKey));
        public static object VS2019SuccessTextBrushKey { get; } = GenerateKeyName(nameof(VS2019SuccessTextBrushKey));

        private static string GenerateKeyName(string baseName)
        {
            return $"AwsToolkit{baseName}";
        }
    }
}
