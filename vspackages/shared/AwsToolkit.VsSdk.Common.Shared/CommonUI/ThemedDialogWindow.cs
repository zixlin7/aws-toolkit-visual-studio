using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public class ThemedDialogWindow : DialogWindow
    {
        static ThemedDialogWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedDialogWindow), new FrameworkPropertyMetadata(typeof(ThemedDialogWindow)));
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            // No window chrome, have to support moving the window ourselves
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
}
