using System.Windows;

namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Represents the Brush keys for an extremely basic theme.
    /// Primary use is for displaying UIs in the Xaml Designer while
    /// working on the Toolkit (prevents errors relating to key lookup failures).
    /// </summary>
    public class DesignTimeToolkitThemeBrushKeys : IToolkitThemeBrushKeys
    {
        private static readonly object FallbackTextBrushKey = SystemColors.ControlTextBrushKey;
        private static readonly object FallbackInfoBrushKey = SystemColors.InfoBrushKey;

        public object HintText => FallbackTextBrushKey;
        public object ToolTipBorder => FallbackTextBrushKey;
        public object ToolTipBackground => FallbackInfoBrushKey;
        public object ToolTipText => FallbackTextBrushKey;
    }
}
