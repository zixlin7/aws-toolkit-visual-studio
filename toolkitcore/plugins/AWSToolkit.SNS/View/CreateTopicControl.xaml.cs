using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// UI for creating new SNS Topic
    /// </summary>
    public partial class CreateTopicControl : BaseAWSControl
    {
        private readonly CreateTopicController _controller;

        public CreateTopicControl()
            : this(null)
        {
        }

        public CreateTopicControl(CreateTopicController controller)
        {
            _controller = controller;
            DataContext = _controller.Model;
            InitializeComponent();
        }

        public override string Title => "Create New SNS Topic";

        public override bool OnCommit()
        {
            var errorMessage = _controller.Model.AsIDataErrorInfo.Error;
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Unable to create SNS Topic", errorMessage);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(new ToolkitException(errorMessage,
                    ToolkitException.CommonErrorCode.UnsupportedState)));
                return false;
            }

            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _ctlNewTopicName.Focus();
        }

    }
}
