using System.Windows;

namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// A helper class that can automatically theme any XAML control or window using VS and Toolkit theme properties.
    ///
    /// Initialize on Extension startup by passing a theme provider to <see cref="Initialize"/>.
    /// The theme provider:
    ///     - maps specific values to generalized properties that are referenced by controls
    ///     - maps styles to controls/ResourceDictionaries.
    ///
    /// To make a view (like a Window or a UserControl) use the Visual Studio styling, add the following
    /// attributes into the element:
    ///     xmlns:themes="clr-namespace:Amazon.AWSToolkit.Themes"
    ///     themes:ToolkitThemes.UseToolkitTheme="True"
    ///
    /// To apply specific colors, reference properties on this class, for example:
    ///     Foreground="{DynamicResource {x:Static themes:ToolkitThemes.HintTextBrushKey}}"
    ///
    /// Colors should be referenced by purpose, not the actual color.
    /// See https://docs.microsoft.com/en-us/visualstudio/extensibility/ux-guidelines/shared-colors-for-visual-studio
    /// for guidance.
    /// </summary>
    /// <remarks>Should only be referenced from within .xaml files.</remarks>
    public static class ToolkitThemes
    {
        private static readonly IToolkitThemeBrushKeys DesignTimeToolkitThemeBrushKeys = new DesignTimeToolkitThemeBrushKeys();
        private static IToolkitThemeProvider _toolkitThemeProvider;

        public static void Initialize(IToolkitThemeProvider toolkitThemeProvider)
        {
            _toolkitThemeProvider = toolkitThemeProvider;
            _toolkitThemeProvider.Initialize();
        }

        /// <summary>
        /// Xaml control property responsible for enabling/disabling the Toolkit theme
        /// </summary>
        public static readonly DependencyProperty UseToolkitThemeProperty = DependencyProperty.RegisterAttached("UseToolkitTheme", typeof(bool), typeof(ToolkitThemes), new PropertyMetadata(false, UseToolkitThemePropertyChanged));

        /// <summary>
        /// Dependency Property Getter/Setter for UseToolkitTheme
        /// </summary>
        public static void SetUseToolkitTheme(UIElement element, bool value) => element.SetValue(UseToolkitThemeProperty, value);
        public static bool GetUseToolkitTheme(UIElement element) => (bool) element.GetValue(UseToolkitThemeProperty);

        private static void UseToolkitThemePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            _toolkitThemeProvider?.UseToolkitThemePropertyChanged(d, e);
        }

        /// <summary>
        /// Theme related Brush Keys to be referenced by Xaml UIs.
        /// This class is responsible for providing the backing value, so that
        /// each UI does not need to reference specific colors.
        /// </summary>
        public static object HintTextBrushKey => GetThemeBrushKeys().HintText;

        /// <summary>
        /// Obtains the source of Brush Keys to be used by the Toolkit
        /// As a fall back, it routes to a simplified source of brushes. This is
        /// necessary at design time, so that the XAML Designer is capable of
        /// displaying controls while developing the Toolkit.
        /// </summary>
        private static IToolkitThemeBrushKeys GetThemeBrushKeys()
        {
            return _toolkitThemeProvider?.GetToolkitThemeBrushKeys() ?? DesignTimeToolkitThemeBrushKeys;
        }
    }
}
