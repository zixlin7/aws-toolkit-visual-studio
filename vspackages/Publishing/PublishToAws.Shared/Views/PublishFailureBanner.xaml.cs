using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// Banner that is shown when a publish has failed.
    /// Data bound to <see cref="PublishProjectViewModel"/>
    /// </summary>
    public partial class PublishFailureBanner : UserControl
    {
        public PublishFailureBanner()
        {
            InitializeComponent();
        }
    }
}
