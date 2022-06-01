using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Represents a textbox control that displays a hint text
    /// </summary>
    public partial class HintTextBox : UserControl
    {
        public HintTextBox()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty TextProperty =
            DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(HintTextBox));

        public static readonly DependencyProperty HintTextProperty =
            DependencyProperty.Register(
                nameof(HintText), typeof(string), typeof(HintTextBox));

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string HintText
        {
            get => (string) GetValue(HintTextProperty);
            set => SetValue(HintTextProperty, value);
        }
    }
}
