using Microsoft.VisualStudio.Shell;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Returns font keys available in the shell. Note that VS2013 supports
    /// only the basic Environment and Caption fonts, the scaling fonts
    /// belong to VS2015 or higher. In the VS2013 scenario we return the
    /// design time keys from the toolkit core (DesignTimeFonts.xaml) that
    /// equate to the same data.
    /// </summary>
    public static class ThemeFontResources
    {
        public static object CaptionFontFamilyKey => VsFonts.CaptionFontFamilyKey;

        public static object CaptionFontSizeKey => VsFonts.CaptionFontSizeKey;

        public static object CaptionFontWeightKey => VsFonts.CaptionFontWeightKey;

        public static object Environment122PercentFontSizeKey => VsFonts.Environment122PercentFontSizeKey;

        public static object Environment122PercentFontWeightKey => VsFonts.Environment122PercentFontWeightKey;

        public static object Environment133PercentFontSizeKey => VsFonts.Environment133PercentFontSizeKey;

        public static object Environment133PercentFontWeightKey => VsFonts.Environment133PercentFontWeightKey;

        public static object Environment155PercentFontSizeKey => VsFonts.Environment155PercentFontSizeKey;

        public static object Environment155PercentFontWeightKey => VsFonts.Environment155PercentFontWeightKey;

        public static object Environment200PercentFontSizeKey => VsFonts.Environment200PercentFontSizeKey;

        public static object Environment200PercentFontWeightKey => VsFonts.Environment200PercentFontWeightKey;

        public static object Environment310PercentFontSizeKey => VsFonts.Environment310PercentFontSizeKey;

        public static object Environment310PercentFontWeightKey => VsFonts.Environment310PercentFontWeightKey;

        public static object Environment375PercentFontSizeKey => VsFonts.Environment375PercentFontSizeKey;

        public static object Environment375PercentFontWeightKey => VsFonts.Environment375PercentFontWeightKey;

        public static object EnvironmentBoldFontWeightKey => VsFonts.EnvironmentBoldFontWeightKey;

        public static object EnvironmentFontFamilyKey => VsFonts.EnvironmentFontFamilyKey;

        public static object EnvironmentFontSizeKey => VsFonts.EnvironmentFontSizeKey;
    }
}
