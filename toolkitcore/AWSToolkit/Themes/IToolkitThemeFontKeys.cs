namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Collection of Font Keys defining a Toolkit Theme.
    /// These back the brush keys in <see cref="ToolkitThemes"/> that are
    /// referenced by Xaml UIs (for example, <see cref="ToolkitThemes.Heading1FontSizeKey"/>)
    /// </summary>
    public interface IToolkitThemeFontKeys
    {
        /// <summary>
        /// Keys used for the largest Text size/weight in a UI, getting smaller with each Heading size
        /// </summary>
        object Heading1FontSize { get; }
        object Heading1FontWeight { get; }
        object Heading2FontSize { get; }
        object Heading2FontWeight { get; }
        object Heading3FontSize { get; }
        object Heading3FontWeight { get; }
    }
}
