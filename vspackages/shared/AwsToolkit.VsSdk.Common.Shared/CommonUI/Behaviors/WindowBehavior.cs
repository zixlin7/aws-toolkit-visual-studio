using System.Windows;

namespace AwsToolkit.VsSdk.Common.CommonUI.Behaviors
{
    public static class WindowBehavior
    {
        public static readonly DependencyProperty BindDialogResultProperty = DependencyProperty.RegisterAttached(
            "BindDialogResult",
            typeof(bool?),
            typeof(WindowBehavior),
            new PropertyMetadata(BindDialogResult_PropertyChanged));

        public static bool? GetBindDialogResult(DependencyObject d)
        {
            return (bool?)d.GetValue(BindDialogResultProperty);
        }

        public static void SetBindDialogResult(DependencyObject d, bool? value)
        {
            d.SetValue(BindDialogResultProperty, value);
        }

        private static void BindDialogResult_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                try
                {
                    // DialogResult can only be set between the call and completion of ShowDialog, otherwise the Window will
                    // throw an exception.  There is no public state to indicate when a Window is in the ShowDialog method.
                    window.DialogResult = (bool?) e.NewValue;
                }
                catch { }
            }
        }
    }
}
