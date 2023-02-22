using System;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Context;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for PublishControl.xaml
    /// </summary>
    public partial class PublishControl : BaseAWSControl
    {
        private readonly PublishController _controller;

        public PublishControl()
            : this(null)
        {
        }

        public PublishControl(PublishController controller)
        {
            _controller = controller;
            DataContext = _controller.Model;
            InitializeComponent();

        }

        public override string Title => "Publish";

        public override bool OnCommit()
        {
            try
            {
                if (string.IsNullOrEmpty(_controller.Model.Message))
                {
                    throw new ToolkitException("Message is required!", ToolkitException.CommonErrorCode.UnsupportedState);
                }

                _controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                _controller.ToolkitContext.ToolkitHost.ShowError($"Error publishing message to SNS topic:{Environment.NewLine}{e.Message}");

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }    
    }
}
