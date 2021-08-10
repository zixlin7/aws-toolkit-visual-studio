using System.Windows;

namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Defines a Theme that can be applied to UIs
    /// </summary>
    public interface IToolkitThemeProvider
    {
        /// <summary>
        /// Perform Toolkit wide Theme initialization
        /// </summary>
        void Initialize();

        /// <summary>
        /// Implements the handler to apply or un-set the Toolkit Theme on the provided control.
        /// </summary>
        void UseToolkitThemePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs eventArgs);

        /// <summary>
        /// Provides the Brush Keys used by this theme.
        /// </summary>
        /// <returns></returns>
        IToolkitThemeBrushKeys GetToolkitThemeBrushKeys();

        /// <summary>
        /// Provides the Font Keys used by this theme.
        /// </summary>
        /// <returns></returns>
        IToolkitThemeFontKeys GetToolkitThemeFontKeys();
    }
}
