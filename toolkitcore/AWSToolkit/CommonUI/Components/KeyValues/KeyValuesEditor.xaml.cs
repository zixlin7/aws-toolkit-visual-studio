using System.Windows;
using System.Windows.Controls;

namespace Amazon.AWSToolkit.CommonUI.Components.KeyValues
{
    /// <summary>
    /// Graphical editor for a list of <see cref="ViewModels.KeyValuesViewModel"/>
    /// </summary>
    public partial class KeyValuesEditor : UserControl
    {
        public KeyValuesEditor()
        {
            InitializeComponent();
        }

        private void TextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.SelectAll();
            }
        }
    }
}
