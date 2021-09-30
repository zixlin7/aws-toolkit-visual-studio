using Amazon.AWSToolkit.Themes;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Visual Studio specific Theme Brushes
    /// </summary>
    public class ToolkitThemeBrushKeys : IToolkitThemeBrushKeys
    {
        public object HintText => EnvironmentColors.ControlEditHintTextBrushKey;
        public object ToolTipBorder => EnvironmentColors.ToolTipBorderBrushKey;
        public object ToolTipBackground => EnvironmentColors.ToolTipBrushKey;
        public object ToolTipText => EnvironmentColors.ToolTipTextBrushKey;
    }
}
