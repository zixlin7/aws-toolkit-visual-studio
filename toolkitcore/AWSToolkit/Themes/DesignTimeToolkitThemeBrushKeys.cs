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

        public object HintText => FallbackTextBrushKey;
    }
}
