using System;
using System.Windows;

using Amazon.AWSToolkit;
using Amazon.AWSToolkit.Shared;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Utility to host controls within a VS SDK <see cref="DialogWindow"/>
    /// Parallels <see cref="DialogWindowHost"/>, but is VS SDK centric.
    /// </summary>
    public class DialogWindowHost
    {
        public static DialogWindow CreateDialogHost(MessageBoxButton buttons,
            IAWSToolkitControl hostedControl,
            IAWSToolkitShellProvider shellProvider)
        {
            var window = CreateWindow(buttons, hostedControl, shellProvider);

            ThemeUtil.UpdateDictionariesForTheme(window.Resources);

            window.Loaded += (sender, e) => hostedControl.ExecuteBackGroundLoadDataLoad();
            return window;
        }

        private static DialogWindow CreateWindow(MessageBoxButton buttons,
            IAWSToolkitControl hostedControl,
            IAWSToolkitShellProvider shellProvider)
        {
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    return new OkCancelDialogWindowHost(hostedControl, MessageBoxButton.OKCancel, shellProvider);
                case MessageBoxButton.YesNo:
                    return new OkCancelDialogWindowHost(hostedControl, MessageBoxButton.YesNo, shellProvider);
                default:
                    throw new NotImplementedException($"{nameof(DialogWindowHost)} does not support {buttons}");
            }
        }
    }
}
