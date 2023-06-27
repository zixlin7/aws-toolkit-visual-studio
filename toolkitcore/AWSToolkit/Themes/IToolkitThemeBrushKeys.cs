namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Collection of Brush Keys defining a Toolkit Theme.
    /// These back the brush keys in <see cref="ToolkitThemes"/> that are
    /// referenced by Xaml UIs (for example, <see cref="ToolkitThemes.HintTextBrushKey"/>)
    /// </summary>
    public interface IToolkitThemeBrushKeys
    {
        object ErrorBackground { get; }
        object ErrorBorder { get; }
        object ErrorText { get; }
        object SuccessBackground { get; }
        object SuccessBorder { get; }
        object SuccessText { get; }
        /// <summary>
        /// Brush used for Hint Text in a UI (Foreground color)
        /// </summary>
        object HintText { get; }
        object InfoText { get; }
        object InfoBackground { get; }
        object ToolTipBorder { get; }
        object ToolTipBackground { get; }
        object ToolTipText { get; }
        object ToolBarBackground { get; }
    }
}
