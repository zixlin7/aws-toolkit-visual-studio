using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for PublishControl.xaml
    /// </summary>
    public partial class PublishControl : BaseAWSControl
    {
        PublishController _controller;

        public PublishControl()
            : this(null)
        {
        }

        public PublishControl(PublishController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();

        }

        public override string Title => "Publish";

        public override bool OnCommit()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Message))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Message is required!");
                return false;
            }

            this._controller.Persist();
            return true;
        }    
    }
}
