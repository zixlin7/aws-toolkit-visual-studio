using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for HyperlinkText.xaml
    /// </summary>
    public partial class HyperlinkText : UserControl
    {
        public HyperlinkText()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty LinkTextProperty =
            DependencyProperty.Register(
                nameof(LinkText), typeof(string), typeof(HyperlinkText));


        public static readonly DependencyProperty UrlProperty =
            DependencyProperty.Register(
                nameof(Url), typeof(string), typeof(HyperlinkText));

        public static readonly DependencyProperty OpenUrlCommandProperty =
            DependencyProperty.Register(
                nameof(OpenUrlCommand), typeof(ICommand), typeof(HyperlinkText));

        public string LinkText
        {
            get => (string) GetValue(LinkTextProperty);
            set => SetValue(LinkTextProperty, value);
        }

        public string Url
        {
            get => (string) GetValue(UrlProperty);
            set => SetValue(UrlProperty, value);
        }

        public ICommand OpenUrlCommand
        {
            get => (ICommand) GetValue(OpenUrlCommandProperty);
            set => SetValue(OpenUrlCommandProperty, value);
        }
    }
}
