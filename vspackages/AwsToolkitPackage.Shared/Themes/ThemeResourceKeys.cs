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

        /// <summary>Gets the key that can be used to get the <see cref="Brush"/> to use for errors in VS 2019.
        /// EnvironmentColors.ToolWindowValidationErrorBorderBrushKey is only avialable for VS 2019+.
        /// This is a temporary stopgap till we do not update all VSSDK related package references to v16 (VS2019) versions.
        /// </summary>
        public static object VS2019ErrorBrushKey { get; } = GenerateKeyName(nameof(VS2019ErrorBrushKey));

        private static string GenerateKeyName(string baseName)
        {
            return $"AwsToolkit{baseName}";
        }
    }
}
