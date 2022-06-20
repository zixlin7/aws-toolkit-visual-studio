using System.Windows;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.S3.Controller;
using Amazon.AWSToolkit.S3.Model;


namespace Amazon.AWSToolkit.S3.View
{
    /// <summary>
    /// Interaction logic for NewFolderControl.xaml
    /// </summary>
    public partial class NewFolderControl : BaseAWSControl
    {
        private readonly NewFolderController _controller;

        public NewFolderControl()
            : this(null) { }

        public NewFolderControl(NewFolderController controller)
        {
            _controller = controller;
            DataContext = _controller.Model;
            InitializeComponent();
        }

        public NewFolderModel Model => _controller.Model;

        public override string Title => "New Folder";

        public override bool OnCommit()
        {
            if (string.IsNullOrWhiteSpace(Model.NewFolderName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("New folder name is required.");
                return false;
            }

            _controller.Persist();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _ctlNewFolderName.Focus();
        }
    }
}
