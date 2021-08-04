﻿namespace Amazon.AWSToolkit.Themes
{
    /// <summary>
    /// Collection of Brush Keys defining a Toolkit Theme.
    /// These back the brush keys in <see cref="ToolkitThemes"/> that are
    /// referenced by Xaml UIs (for example, <see cref="ToolkitThemes.HintTextBrushKey"/>)
    /// </summary>
    public interface IToolkitThemeBrushKeys
    {
        /// <summary>
        /// Brush used for Hint Text in a UI (Foreground color)
        /// </summary>
        object HintText { get; }
    }
}
