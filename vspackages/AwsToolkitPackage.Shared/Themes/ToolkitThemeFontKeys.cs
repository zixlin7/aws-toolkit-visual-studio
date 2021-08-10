using Amazon.AWSToolkit.Themes;

using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Visual Studio specific Theme Fonts
    /// </summary>
    public class ToolkitThemeFontKeys : IToolkitThemeFontKeys
    {
        public object Heading1FontSize => VsFonts.Environment200PercentFontSizeKey;
        public object Heading1FontWeight => VsFonts.Environment200PercentFontWeightKey;
        public object Heading2FontSize => VsFonts.Environment155PercentFontSizeKey;
        public object Heading2FontWeight => VsFonts.Environment155PercentFontWeightKey;
        public object Heading3FontSize => VsFonts.Environment133PercentFontSizeKey;
        public object Heading3FontWeight => VsFonts.Environment133PercentFontWeightKey;
    }
}
