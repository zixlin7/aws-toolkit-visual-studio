using System;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Controller;
using Amazon.AWSToolkit.Exceptions;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for CreateSubscriptionControl.xaml
    /// </summary>
    public partial class CreateSubscriptionControl : BaseAWSControl
    {
        CreateSubscriptionController _controller;

        public CreateSubscriptionControl()
            : this(null)
        {
        }

        public CreateSubscriptionControl(CreateSubscriptionController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();

            int selectedIndex = 0;
            for (int i = 0; i < this._ctlProtocols.Items.Count; i++)
            {
                if (this._ctlProtocols.Items[i].Equals(this._controller.Model.Protocol))
                {
                    selectedIndex = i;
                    break;
                }
            }
            
            this._ctlProtocols.SelectedIndex = selectedIndex;

            if (this._controller.Model.IsTopicARNReadOnly)
            {
                this._ctlTopicARN.Visibility = Visibility.Hidden;
                this._ctlTopicARNRO.Visibility = Visibility.Visible;
            }
            else
            {
                this._ctlTopicARN.Visibility = Visibility.Visible;
                this._ctlTopicARNRO.Visibility = Visibility.Hidden;
            }

            onProtocolsSelectionChanged(null, null);
        }

        public override string Title => "Create New Subscription";

        public override bool OnCommit()
        {
            try
            {
                if (string.IsNullOrEmpty(_controller.Model.TopicArn))
                {
                    throw new ToolkitException("Topic ARN is required!", ToolkitException.CommonErrorCode.UnsupportedState);
                }
                if (string.IsNullOrEmpty(_controller.Model.Endpoint))
                {
                    throw new ToolkitException("Endpoint is required!", ToolkitException.CommonErrorCode.UnsupportedState);
                }

                _controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                _controller.ToolkitContext.ToolkitHost.ShowError($"Error creating subscription:{Environment.NewLine}{e.Message}");

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }

        private void onProtocolsSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this._ctlEndpoints == null)
                return;

            if (SubscriptionProtocol.SQS.Equals(_ctlProtocols.SelectedValue))
            {
                this._ctlEndpoints.Visibility = Visibility.Hidden;
                this._ctlAWSResourcesEndpoints.Visibility = Visibility.Visible;
                this._ctlAWSResourcesEndpoints.ItemsSource = this._controller.Model.PossibleSQSEndpoints;
                this._ctlAddSQSPermission.IsEnabled = true;
            }
            else if (SubscriptionProtocol.LAMBDA.Equals(_ctlProtocols.SelectedValue))
            {
                this._ctlEndpoints.Visibility = Visibility.Hidden;
                this._ctlAWSResourcesEndpoints.Visibility = Visibility.Visible;
                this._ctlAWSResourcesEndpoints.ItemsSource = this._controller.Model.PossibleLambdaEndpoints;
            }
            else
            {
                this._ctlEndpoints.Visibility = Visibility.Visible;
                this._ctlAWSResourcesEndpoints.Visibility = Visibility.Hidden;
                this._ctlAddSQSPermission.IsEnabled = false;
            }
        }
    }
}
