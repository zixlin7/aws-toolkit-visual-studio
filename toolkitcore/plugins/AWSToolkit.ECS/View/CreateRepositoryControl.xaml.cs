using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.ECS.Model;
using Amazon.AWSToolkit.ECS.Controller;

namespace Amazon.AWSToolkit.ECS.View
{
    /// <summary>
    /// Interaction logic for CreateRepositoryControl.xaml
    /// </summary>
    public partial class CreateRepositoryControl : BaseAWSControl
    {
        CreateRepositoryController _controller;

        public CreateRepositoryControl()
            : this(null)
        {
        }

        public CreateRepositoryControl(CreateRepositoryController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public CreateRepositoryModel Model => this._controller.Model;

        public override string Title => "Create Repository";

        public override bool OnCommit()
        {
            string newName = this._controller.Model.RepositoryName == null ? string.Empty : this._controller.Model.RepositoryName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                return false;
            }

            return this._controller.Persist();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewRepositoryName.Focus();
        }
    }
}
