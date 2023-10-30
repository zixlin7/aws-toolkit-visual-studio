using Amazon.AWSToolkit.Themes;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AWSToolkit.VisualStudio
{
    /// <summary>
    /// Visual Studio specific Theme Brushes
    /// </summary>
    public class ToolkitThemeBrushKeys : IToolkitThemeBrushKeys
    {
        // The following brush keys are only available in VS2022 VSSDK, toolkit-provided values
        // are supplied for VS2019.
#if VS2022_OR_LATER
        public object ErrorBackground => CommonDocumentColors.StatusBannerErrorBrushKey;
        public object ErrorBorder => CommonDocumentColors.StatusBannerErrorBorderTextBrushKey;
        public object ErrorText => CommonDocumentColors.StatusBannerErrorTextBrushKey;
        public object SuccessBackground => CommonDocumentColors.StatusBannerSuccessBrushKey;
        public object SuccessBorder => CommonDocumentColors.StatusBannerSuccessBorderTextBrushKey;
        public object SuccessText => CommonDocumentColors.StatusBannerSuccessTextBrushKey;
#else
        public object ErrorBackground => ThemeResourceKeys.VS2019ErrorBackgroundBrushKey;
        public object ErrorBorder => ThemeResourceKeys.VS2019ErrorBorderBrushKey;
        public object ErrorText => ThemeResourceKeys.VS2019ErrorTextBrushKey;
        public object SuccessBackground => ThemeResourceKeys.VS2019SuccessBackgroundBrushKey;
        public object SuccessBorder => ThemeResourceKeys.VS2019SuccessBorderBrushKey;
        public object SuccessText => ThemeResourceKeys.VS2019SuccessTextBrushKey;
#endif
        public object HintText => EnvironmentColors.ControlEditHintTextBrushKey;
        public object InfoText => EnvironmentColors.SystemInfoTextBrushKey;
        public object InfoBackground => EnvironmentColors.SystemInfoBackgroundBrushKey;
        public object PanelBorder => EnvironmentColors.PanelBorderBrushKey;
        public object ToolTipBorder => EnvironmentColors.ToolTipBorderBrushKey;
        public object ToolTipBackground => EnvironmentColors.ToolTipBrushKey;
        public object ToolTipText => EnvironmentColors.ToolTipTextBrushKey;
        public object ToolBarBackground => EnvironmentColors.CommandBarGradientBrushKey;
    }
}
