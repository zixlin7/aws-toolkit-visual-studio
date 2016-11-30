using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using Amazon.AWSToolkit.Shared;

namespace Amazon.AWSToolkit.CommonUI
{
    public class DialogHostUtil
    {
        public static Window CreateDialogHost(MessageBoxButton buttons, IAWSToolkitControl hostedControl)
        {
            Window window = null;
            switch (buttons)
            {
                case MessageBoxButton.OKCancel:
                    window = new OkCancelDialogHost(hostedControl);
                    break;
                case MessageBoxButton.OK:
                    window = new OkDialogHost(hostedControl);
                    break;
                case MessageBoxButton.YesNo:
                    window = new OkCancelDialogHost(hostedControl, MessageBoxButton.YesNo);
                    break;
                default:
                    throw new NotImplementedException("DialogHost for " + buttons.ToString() + " is not implemented");
            }

            RoutedEventHandler handler = delegate(object sender, RoutedEventArgs e)
            {
                hostedControl.ExecuteBackGroundLoadDataLoad();
            };

            ThemeUtil.UpdateDictionariesForTheme(window.Resources);

            window.Loaded += new RoutedEventHandler(handler);
            return window;
        }

        public static Window CreateFramelessDialogHost(IAWSToolkitControl hostedControl)
        {
            Window window = null;
            window = new FramelessDialogHost(hostedControl);

            RoutedEventHandler handler = delegate(object sender, RoutedEventArgs e)
            {
                hostedControl.ExecuteBackGroundLoadDataLoad();
            };

            window.Loaded += new RoutedEventHandler(handler);
            return window;
        }
    }
}
