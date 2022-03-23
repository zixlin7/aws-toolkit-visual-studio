using System.Windows.Controls;

using Amazon.AWSToolkit.Publish.ViewModels;

namespace Amazon.AWSToolkit.Publish.Views
{
    /// <summary>
    /// View panel presenting what Credentials and Region are being used for the Publish session.
    /// Data bound to <see cref="PublishToAwsDocumentViewModel"/>
    /// </summary>
    public partial class CredentialsRegionControl : UserControl
    {
        public CredentialsRegionControl()
        {
            InitializeComponent();
        }
    }
}
