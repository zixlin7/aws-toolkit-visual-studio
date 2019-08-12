using System.Windows;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Interaction logic for ImageButton.xaml
    /// </summary>
    public partial class ImageButton 
    {
        public ImageButton()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register("Text", typeof(string),
                typeof(ImageButton), new FrameworkPropertyMetadata(
                new PropertyChangedCallback(OnTextChanged)));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        private static void OnTextChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            ImageButton btn = (ImageButton)sender;
            btn._ctlLabel.Text = e.NewValue.ToString();
        }
    }
}
