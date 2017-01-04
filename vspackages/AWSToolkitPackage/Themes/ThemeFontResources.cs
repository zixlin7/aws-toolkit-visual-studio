using Microsoft.VisualStudio.PlatformUI;
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
        public static object CaptionFontFamilyKey
        {
            get
            {
                return VsFonts.CaptionFontFamilyKey;
            }
        }

        public static object CaptionFontSizeKey
        {
            get
            {
                return VsFonts.CaptionFontSizeKey;
            }
        }

        public static object CaptionFontWeightKey
        {
            get
            {
                return VsFonts.CaptionFontWeightKey;
            }
        }

        public static object Environment122PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment122PercentFontSizeKey";
#else
                return VsFonts.Environment122PercentFontSizeKey;
#endif
            }
        }

        public static object Environment122PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment122PercentFontWeightKey";
#else
                return VsFonts.Environment122PercentFontWeightKey;
#endif
            }
        }

        public static object Environment133PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment133PercentFontSizeKey";
#else
                return VsFonts.Environment133PercentFontSizeKey;
#endif
            }
        }

        public static object Environment133PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment133PercentFontWeightKey";
#else
                return VsFonts.Environment133PercentFontWeightKey;
#endif
            }
        }

        public static object Environment155PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment155PercentFontSizeKey";
#else
                return VsFonts.Environment155PercentFontSizeKey;
#endif
            }
        }

        public static object Environment155PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment155PercentFontWeightKey";
#else
                return VsFonts.Environment155PercentFontWeightKey;
#endif
            }
        }

        public static object Environment200PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment200PercentFontSizeKey";
#else
                return VsFonts.Environment200PercentFontSizeKey;
#endif
            }
        }

        public static object Environment200PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment200PercentFontWeightKey";
#else
                return VsFonts.Environment200PercentFontWeightKey;
#endif
            }
        }

        public static object Environment310PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment310PercentFontSizeKey";
#else
                return VsFonts.Environment310PercentFontSizeKey;
#endif
            }
        }

        public static object Environment310PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment310PercentFontWeightKey";
#else
                return VsFonts.Environment310PercentFontWeightKey;
#endif
            }
        }

        public static object Environment375PercentFontSizeKey
        {
            get
            {
#if VS2013
                return "dtEnvironment375PercentFontSizeKey";
#else
                return VsFonts.Environment375PercentFontSizeKey;
#endif
            }
        }

        public static object Environment375PercentFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironment375PercentFontWeightKey";
#else
                return VsFonts.Environment375PercentFontWeightKey;
#endif
            }
        }

        public static object EnvironmentBoldFontWeightKey
        {
            get
            {
#if VS2013
                return "dtEnvironmentBoldFontWeightKey";
#else
                return VsFonts.EnvironmentBoldFontWeightKey;
#endif
            }
        }

        public static object EnvironmentFontFamilyKey
        {
            get
            {
                return VsFonts.EnvironmentFontFamilyKey;
            }
        }

        public static object EnvironmentFontSizeKey
        {
            get
            {
                return VsFonts.EnvironmentFontSizeKey;
            }
        }
    }
}