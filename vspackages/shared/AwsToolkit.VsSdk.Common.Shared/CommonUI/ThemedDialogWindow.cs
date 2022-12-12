using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    public class ThemedDialogWindow : DialogWindow
    {
        public static readonly DependencyProperty CloseCommandProperty = DependencyProperty.Register(
            nameof(CloseCommand), typeof(ICommand), typeof(ThemedDialogWindow), new PropertyMetadata());

        public ICommand CloseCommand
        {
            get => (ICommand)GetValue(CloseCommandProperty);
            set => SetValue(CloseCommandProperty, value);
        }

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
