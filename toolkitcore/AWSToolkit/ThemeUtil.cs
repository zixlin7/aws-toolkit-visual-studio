using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

using Amazon.AWSToolkit.Shared;
using log4net;

namespace Amazon.AWSToolkit
{
    public enum VsTheme
    {
        Unknown = 0,
        Light,
        Dark,
        Blue,
        HighContrast
    }

    public static class ThemeUtil
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(ThemeUtil));

        private static readonly IDictionary<string, VsTheme> Themes = new Dictionary<string, VsTheme>(StringComparer.OrdinalIgnoreCase)
        {
            { "de3dbbcd-f642-433c-8353-8f1df4370aba", VsTheme.Light }, 
            { "1ded0138-47ce-435e-84ef-9ec1f439b749", VsTheme.Dark }, 
            { "a4d6a176-b948-4b29-8c66-53c97a1ed7d0", VsTheme.Blue },
            { "a5c004b4-2d4b-494e-bf01-45fc492522c7", VsTheme.HighContrast }
        };

        public static VsTheme GetCurrentTheme()
        {
            string themeId = GetThemeId();
            if (string.IsNullOrEmpty(themeId) == false)
            {
                VsTheme theme;
                if (Themes.TryGetValue(themeId, out theme))
                {
                    return theme;
                }
            }

            return VsTheme.Unknown;
        }

        static string _registryVersion;

        public static void Initialize(string registryVersion)
        {
            _registryVersion = registryVersion;
        }

        public static string GetThemeId()
        {
            if (string.IsNullOrEmpty(_registryVersion))
                return null;

            // VS2015 moved the location and format of the persisted theme indication, and
            // isn't consistent with regards to the enclosing of the theme id in {}!
            string themeId = null;
            try
            {
                var vrn = double.Parse(_registryVersion);
                if (vrn < 14)
                {
                    const string CategoryName = "General";
                    const string ThemePropertyName = "CurrentTheme";
                    string keyName = string.Format(@"Software\Microsoft\VisualStudio\{0}\{1}", _registryVersion, CategoryName);
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName))
                    {
                        if (key != null)
                            themeId = (string)key.GetValue(ThemePropertyName, string.Empty);
                    }
                }
                else
                {
                    const string CategoryName = @"ApplicationPrivateSettings\Microsoft\VisualStudio";
                    const string ThemePropertyName = "ColorTheme";
                    string keyName = string.Format(@"Software\Microsoft\VisualStudio\{0}\{1}", _registryVersion, CategoryName);
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(keyName))
                    {
                        if (key != null)
                        {
                            // format is '0*System.String*1ded0138-47ce-435e-84ef-9ec1f439b749'
                            var value = (string)key.GetValue(ThemePropertyName, string.Empty);
                            if (!string.IsNullOrEmpty(value))
                            {
                                var parts = value.Split('*');
                                if (parts.Length == 3)
                                    themeId = parts[2];
                            }
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(themeId))
                return themeId.Trim('{', '}');

            return null;
        }

        // Returns one of the logo_aws_* image resources, automatically selecting
        // the white logo if we're in dark mode.
        public static ImageSource GetLogoImageSource(string basename)
        {
            const string logoResourcePath = "/AWSToolkit;component/Resources/Logos/";

            var logoResourceName = String.Concat(logoResourcePath, 
                                                 basename, 
                                                 GetCurrentTheme() == VsTheme.Dark ? "_white.png" : ".png");

            try
            {
                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(logoResourceName, UriKind.Relative);
                img.EndInit();
                return img;
            }
            catch (Exception e)
            {
                LOGGER.Error("Failed to load logo resource " + logoResourceName, e);
            }

            return null;
        }

        public static event EventHandler ThemeChange;

        public static void RaiseThemeChangeEvent()
        {
            ThemeChange?.Invoke(null, new EventArgs());
        }

        public static void UpdateDictionariesForTheme(ResourceDictionary controlResourceDictionary)
        {
            try
            {
                controlResourceDictionary.BeginInit();

                IEnumerable<Uri> additions;
                IEnumerable<Uri> removals;
                (ToolkitFactory.Instance.ShellProvider.QueryShellProviderService<IAWSToolkitShellThemeService>()).QueryShellThemeOverrides(out additions, out removals);

                var mergedDictionaries = controlResourceDictionary.MergedDictionaries;

                if (removals != null)
                {
                    // this is O(n*n) but dictionaries typically don't contain a lot of
                    // entries
                    var enumerable = removals as Uri[] ?? removals.ToArray();
                    for (var r = 0; r < enumerable.Count(); r++)
                    {
                        for (var i = 0; i < mergedDictionaries.Count; i++)
                        {
                            if (mergedDictionaries[i].Source.Equals(enumerable[r]))
                            {
                                mergedDictionaries.RemoveAt(i);
                                break;
                            }
                        }
                    }
                }

                if (additions != null)
                {
                    // process Insert in reverse order to maintain the ordering the shell gave us
                    var enumerable = additions as Uri[] ?? additions.ToArray();
                    for (var a = enumerable.Count() - 1; a >= 0; a--)
                    {
                        var uri = enumerable.ElementAt(a);
                        var rd = new ResourceDictionary {Source = uri};
                        mergedDictionaries.Insert(0, rd);
                    }
                }
            }
            catch (NullReferenceException)
            {
            }
            finally
            {
                controlResourceDictionary.EndInit();
            }
        }

    }
}