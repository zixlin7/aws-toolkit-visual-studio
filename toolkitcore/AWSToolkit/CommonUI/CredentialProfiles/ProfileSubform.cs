using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Credentials.Utils;

namespace Amazon.AWSToolkit.CommonUI.CredentialProfiles
{
    public abstract class ProfileSubform : UserControl
    {
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header),
            typeof(FrameworkElement),
            typeof(ProfileSubform));

        public FrameworkElement Header
        {
            get => (FrameworkElement) GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public static readonly DependencyProperty FooterProperty = DependencyProperty.Register(
            nameof(Footer),
            typeof(FrameworkElement),
            typeof(ProfileSubform));

        public FrameworkElement Footer
        {
            get => (FrameworkElement) GetValue(FooterProperty);
            set => SetValue(FooterProperty, value);
        }

        public abstract CredentialType CredentialType { get; }
    }
}
