using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio.PlatformUI;

namespace Amazon.AwsToolkit.VsSdk.Common.CommonUI
{
    /// <summary>
    /// Base class for themed dialog windows.
    /// </summary>
    /// <remarks>
    /// Do not use IsCancel on buttons in subclasses as the X in the upper right of the window uses IsCancel for cancelling the dialog.
    /// IsDefault can be used in subclasses.  Typically buttons should bind to ICommand objects in the view model to perform commit (Default)
    /// and cancel operations.
    /// </remarks>
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
