using Amazon.AWSToolkit.Themes;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Visual Studio specific Theme Brushes
    /// </summary>
    public class ToolkitThemeBrushKeys : IToolkitThemeBrushKeys
    {
        // ToolWindowValidationErrorBorderBrushKey is only in VS 2019+, we
        // cannot use it for as long as we support VS 2017
#if VS2022_OR_LATER
        public object ErrorBorder => EnvironmentColors.ToolWindowValidationErrorBorderBrushKey;
#else
        public object ErrorBorder => ThemeResourceKeys.VS2019ErrorBrushKey;
#endif
        public object HintText => EnvironmentColors.ControlEditHintTextBrushKey;
        public object InfoText => EnvironmentColors.SystemInfoTextBrushKey;
        public object InfoBackground => EnvironmentColors.SystemInfoBackgroundBrushKey;
        public object ToolTipBorder => EnvironmentColors.ToolTipBorderBrushKey;
        public object ToolTipBackground => EnvironmentColors.ToolTipBrushKey;
        public object ToolTipText => EnvironmentColors.ToolTipTextBrushKey;
        public object ToolBarBackground => EnvironmentColors.CommandBarGradientBrushKey;
    }
}
