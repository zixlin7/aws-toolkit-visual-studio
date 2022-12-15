using System.ComponentModel;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

using Microsoft.VisualStudio.PlatformUI;

namespace AwsToolkit.VsSdk.Common.CommonUI
{
    [TemplatePart(Name = PART_CloseWindowButton, Type = typeof(ButtonBase))]
    public class ThemedDialogWindow : DialogWindow
    {
        private const string PART_CloseWindowButton = "PART_CloseWindowButton";

        private ButtonBase _closeButton;

        static ThemedDialogWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ThemedDialogWindow), new FrameworkPropertyMetadata(typeof(ThemedDialogWindow)));
        }

        private void CloseWindowButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RemoveCloseButtonClickHandler()
        {
            if (_closeButton != null)
            {
                _closeButton.Click -= CloseWindowButton_Click;
            }
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            RemoveCloseButtonClickHandler();

            _closeButton = GetTemplateChild(PART_CloseWindowButton) as ButtonBase;
            if (_closeButton != null)
            {
                _closeButton.Click += CloseWindowButton_Click;
            }
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            RemoveCloseButtonClickHandler();

            base.OnClosing(e);
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
