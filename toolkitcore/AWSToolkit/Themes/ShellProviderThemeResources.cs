using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Allows indirection to the host shell to obtain font resources
    /// which are clr native types (double etc) that cannot be referenced
    /// from xaml via dynamic resource markup.
    /// </summary>
    public static class ShellProviderThemeResources
    {
        static IAWSToolkitShellThemeService _shellProviderThemeService = null;

        static IAWSToolkitShellThemeService ShellProviderThemeService
        {
            get
            {
                if (_shellProviderThemeService == null)
                {
                    if (ToolkitFactory.Instance != null && ToolkitFactory.Instance.ShellProvider != null)
                        _shellProviderThemeService = ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IAWSToolkitShellThemeService>();
                }

                return _shellProviderThemeService;
            }
        }


        public static object CaptionFontFamilyKey => ShellProviderThemeService != null ? ShellProviderThemeService.CaptionFontFamilyKey : "dtCaptionFontFamilyKey";

        public static object CaptionFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.CaptionFontSizeKey : "dtCaptionFontSizeKey";

        public static object CaptionFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.CaptionFontWeightKey : "dtCaptionFontWeightKey";

        public static object EnvironmentBoldFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.EnvironmentBoldFontWeightKey : "dtEnvironmentBoldFontWeightKey";

        public static object EnvironmentFontFamilyKey => ShellProviderThemeService != null ? ShellProviderThemeService.EnvironmentFontFamilyKey : "dtEnvironmentFontFamilyKey";

        public static object EnvironmentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.EnvironmentFontSizeKey : "dtEnvironmentFontSizeKey";

        public static object Environment122PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment122PercentFontSizeKey : "dtEnvironment122PercentFontSizeKey";

        public static object Environment122PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment122PercentFontWeightKey : "dtEnvironment122PercentFontWeightKey";

        public static object Environment133PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment133PercentFontSizeKey : "dtEnvironment133PercentFontSizeKey";

        public static object Environment133PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment133PercentFontWeightKey : "dtEnvironment133PercentFontWeightKey";

        public static object Environment155PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment155PercentFontSizeKey : "dtEnvironment155PercentFontSizeKey";

        public static object Environment155PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment155PercentFontWeightKey : "dtEnvironment155PercentFontWeightKey";

        public static object Environment200PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment200PercentFontSizeKey : "dtEnvironment200PercentFontSizeKey";

        public static object Environment200PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment200PercentFontWeightKey : "dtEnvironment200PercentFontWeightKey";

        public static object Environment310PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment310PercentFontSizeKey : "dtEnvironment310PercentFontSizeKey";

        public static object Environment310PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment310PercentFontWeightKey : "dtEnvironment310PercentFontWeightKey";

        public static object Environment375PercentFontSizeKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment375PercentFontSizeKey : "dtEnvironment375PercentFontSizeKey";

        public static object Environment375PercentFontWeightKey => ShellProviderThemeService != null ? ShellProviderThemeService.Environment375PercentFontWeightKey : "dtEnvironment375PercentFontWeightKey";
    }
}
