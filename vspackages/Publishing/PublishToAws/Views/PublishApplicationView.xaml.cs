using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown while a publish is in progress.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class PublishApplicationView : UserControl
    {
        public PublishApplicationView()
        {
            InitializeComponent();
        }

        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
