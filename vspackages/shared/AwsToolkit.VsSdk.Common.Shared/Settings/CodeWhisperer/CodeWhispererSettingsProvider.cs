using System.Runtime.InteropServices;

using Community.VisualStudio.Toolkit;

namespace AwsToolkit.VsSdk.Common.Settings.CodeWhisperer
{
    /// <summary>
    /// This provides a stock "object explorer" view of the CodeWhisperer settings into the VS "Tools > Options" dialog.
    /// Reference: https://www.vsixcookbook.com/recipes/settings-and-options.html
    /// </summary>
    [ComVisible(true)]
    public class CodeWhispererSettingsProvider : BaseOptionPage<CodeWhispererSettings> { }
}
