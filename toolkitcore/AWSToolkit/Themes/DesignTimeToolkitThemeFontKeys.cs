using System.Windows;

namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Represents the Font related (size, weighting) keys for an extremely basic theme.
    /// Primary use for this class is for displaying UIs in the Xaml Designer while
    /// working on the Toolkit (prevents errors relating to key lookup failures).
    /// </summary>
    public class DesignTimeToolkitThemeFontKeys : IToolkitThemeFontKeys
    {
        /// <summary>
        /// Text will appear with the regular size font at design time.
        /// </summary>
        private static readonly object FallbackSizeKey = SystemFonts.MessageFontSizeKey;
        private static readonly object FallbackWeightKey = SystemFonts.MessageFontWeightKey;

        public object Heading1FontSize => FallbackSizeKey;
        public object Heading1FontWeight => FallbackWeightKey;
        public object Heading2FontSize => FallbackSizeKey;
        public object Heading2FontWeight => FallbackWeightKey;
        public object Heading3FontSize => FallbackSizeKey;
        public object Heading3FontWeight => FallbackWeightKey;
        public object Heading4FontSize => FallbackSizeKey;
        public object Heading4FontWeight => FallbackWeightKey;
    }
}
