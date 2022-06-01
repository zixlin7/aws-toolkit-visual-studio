using System.Windows;
using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel that is shown when a user is selecting their publish target.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class SelectTargetView : UserControl
    {
        public SelectTargetView()
        {
            InitializeComponent();
        }
    }
}
