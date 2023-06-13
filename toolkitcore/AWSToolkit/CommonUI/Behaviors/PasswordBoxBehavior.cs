using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Amazon.AWSToolkit.CommonUI.Behaviors
{
    public static class PasswordBoxBehavior
    {
        public static readonly DependencyProperty BindPasswordProperty = DependencyProperty.RegisterAttached(
            "BindPassword",
            typeof(bool),
            typeof(PasswordBoxBehavior),
            new PropertyMetadata(BindPassword_PropertyChanged));

        public static bool GetBindPassword(DependencyObject d)
        {
            return (bool) d.GetValue(BindPasswordProperty);
        }

        public static void SetBindPassword(DependencyObject d, bool value)
        {
            d.SetValue(BindPasswordProperty, value);
        }

        private static void BindPassword_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox)
            {
                if ((bool) e.NewValue)
                {
                    passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                }
                else
                {
                    passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                }
            }
        }

        private static void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && passwordBox.Password != GetPassword(passwordBox))
            {
                SetPassword(passwordBox, passwordBox.Password);
            }
        }

        public static readonly DependencyProperty PasswordProperty = DependencyProperty.RegisterAttached(
            "Password",
            typeof(string),
            typeof(PasswordBoxBehavior),
            new FrameworkPropertyMetadata()
            {
                BindsTwoWayByDefault = true,
                DefaultUpdateSourceTrigger = UpdateSourceTrigger.LostFocus,
                PropertyChangedCallback = Password_PropertyChanged
            });

        public static string GetPassword(DependencyObject d)
        {
            return (string) d.GetValue(PasswordProperty);
        }

        public static void SetPassword(DependencyObject d, string value)
        {
            d.SetValue(PasswordProperty, value);
        }

        private static void Password_PropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PasswordBox passwordBox && e.NewValue is string newPassword && passwordBox.Password != newPassword)
            {
                passwordBox.Password = newPassword;
            }
        }
    }
}
