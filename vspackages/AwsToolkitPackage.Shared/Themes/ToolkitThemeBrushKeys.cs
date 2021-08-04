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
    }
}
