using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for NewTopicControl.xaml
    /// </summary>
    public partial class CreateTopicControl : BaseAWSControl
    {
        CreateTopicController _controller;

        public CreateTopicControl()
            : this(null)
        {
        }

        public CreateTopicControl(CreateTopicController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title => "Create New Topic";

        public override bool OnCommit()
        {
            string newName = this._controller.Model.TopicName == null ? string.Empty : this._controller.Model.TopicName;
            if (newName.Trim().Equals(string.Empty))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                return false;
            }

            this._controller.Persist();
            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlNewTopicName.Focus();
        }

    }
}
