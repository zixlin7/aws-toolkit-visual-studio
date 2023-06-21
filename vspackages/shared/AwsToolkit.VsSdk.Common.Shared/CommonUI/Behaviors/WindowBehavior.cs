using System.Windows;

namespace AwsToolkit.VsSdk.Common.CommonUI.Behaviors
{
    public static class WindowBehavior
    {
        /// <summary>
        /// Allows binding from a view model to the Window.DialogResult property.
        /// </summary>
        /// <remarks>
        /// Only binds from view model to Window.DialogResult.  As Window.DialogResult does not raise a property changed
        /// event nor is it a DependencyProperty, it cannot update the view model property when it changes.  If the DialogResult
        /// of the Window is needed, either use the value upon return from ShowDialog or listen for the Window.OnClosing/OnClosed
        /// events.
        /// </remarks>
        public static readonly DependencyProperty BindDialogResultProperty = DependencyProperty.RegisterAttached(
            "BindDialogResult",
            typeof(bool?),
            typeof(WindowBehavior),
            new FrameworkPropertyMetadata()
            {
                BindsTwoWayByDefault = false,
                PropertyChangedCallback = BindDialogResult_PropertyChanged
            });

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
