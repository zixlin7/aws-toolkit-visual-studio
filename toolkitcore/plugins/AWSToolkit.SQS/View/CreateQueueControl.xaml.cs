using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.SQS.Model;
using Amazon.AWSToolkit.SQS.Workers;
using log4net;

namespace Amazon.AWSToolkit.SQS.View
{
    /// <summary>
    /// Interaction logic for CreateQueueControl.xaml
    /// </summary>
    public partial class CreateQueueControl : BaseAWSControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(CreateQueueControl));

        public CreateQueueControl()
            : this(new CreateQueueControlModel())
        {
        }

        public CreateQueueControl(CreateQueueControlModel model)
        {
            this.Model = model;
            this.DataContext = model;
            InitializeComponent();
            this._ctlDeadLetterQueues.ItemsSource = Model.ExistingQueues;
        }

        public CreateQueueControlModel Model
        {
            get;
            set;
        }

        public override string Title => "Create Queue";

        // Validate all dependency objects in a window
        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this.Model.QueueName))
            {
                this._ctlQueueName.Focus();
                ToolkitFactory.Instance.ShellProvider.ShowError("Queue name is required.");
                return false;
            }

            if (Validation.GetHasError(this._ctlDefaultVisibilityTimeout))
            {
                this._ctlDefaultVisibilityTimeout.Focus();
                ToolkitFactory.Instance.ShellProvider.ShowError("Default visibility timeout must be between 0 to 43200 (maximum 12 hours).");
                return false;
            }

            if (this.Model.UseRedrivePolicy)
            {
                if (string.IsNullOrEmpty(this.Model.DeadLetterQueueUrl))
                {
                    this._ctlDeadLetterQueues.Focus();
                    ToolkitFactory.Instance.ShellProvider.ShowError("An existing queue to which to send unprocessed messages must be selected.");
                    return false;
                }

                if (Validation.GetHasError(this._ctlMaxReceives))
                {
                    this._ctlMaxReceives.Focus();
                    ToolkitFactory.Instance.ShellProvider.ShowError("Maximum receives value must be between 1 to 1000.");
                    return false;
                }
            }

            return true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlQueueName.Focus();
            LoadExistingQueues();  // could defer this until redrive policy is checked...
        }

        private void LoadExistingQueues()
        {
            if (Model.SQSClient == null)
                return;

            new QueryExistingQueuesWorker(Model.SQSClient, LOGGER, OnExistingQueuesQueried);
        }

        private void OnExistingQueuesQueried(IEnumerable<string> queues)
        {
            Model.ExistingQueues.Clear();
            foreach (var q in queues)
            {
                var qvm = new QueueViewBaseModel
                    {
                        Name = q.Substring(q.LastIndexOf('/') + 1), 
                        QueueURL = q
                    };
                Model.ExistingQueues.Add(qvm);
            }
        }
    }
}
