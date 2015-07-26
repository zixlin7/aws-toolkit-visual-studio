using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Amazon.AWSToolkit.Lambda.Controller;

using log4net;

namespace Amazon.AWSToolkit.Lambda.View
{
    /// <summary>
    /// Interaction logic for AddEventSourceControl.xaml
    /// </summary>
    public partial class AddEventSourceControl
    {
        static readonly ILog LOGGER = LogManager.GetLogger(typeof(AddEventSourceControl));

        public const int SOURCE_TYPE_S3_INDEX = 0;
        public const int SOURCE_TYPE_DYNAMODB_STREAM_INDEX = 1;
        public const int SOURCE_TYPE_SNS_INDEX = 2;
        public const int SOURCE_TYPE_KINESIS_INDEX = 3;

        public enum SourceType { Unknown, S3, DynamoDBStream, Kinesis, SNS };

        AddEventSourceController _controller;
        public AddEventSourceControl(AddEventSourceController controller)
        {
            InitializeComponent();
            this._controller = controller;

            EventSourceType_SelectionChanged(this, null);
        }

        public override string Title
        {
            get
            {
                return "Add Event Source";
            }
        }

        public SourceType EventSourceType
        {
            get
            {
                switch (this._ctlSourceType.SelectedIndex)
                {
                    case SOURCE_TYPE_S3_INDEX:
                        return SourceType.S3;
                    case SOURCE_TYPE_DYNAMODB_STREAM_INDEX:
                        return SourceType.DynamoDBStream;
                    case SOURCE_TYPE_KINESIS_INDEX:
                        return SourceType.Kinesis;
                    case SOURCE_TYPE_SNS_INDEX:
                        return SourceType.SNS;
                    default:
                        return SourceType.Unknown;
                }
            }
        }

        public string Resource
        {
            get
            {
                return this._ctlResources.Text;
            }
        }

        public int BatchSize
        {
            get
            {
                int value;
                if (int.TryParse(this._ctlBatchSize.Text, out value))
                    return value;

                return 100;
            }
        }

        public string StartPosition
        {
            get { return this._ctlStartingPosition.Text; }
        }

        public override bool Validated()
        {
            int batchSize;
            if (this._ctlBatchSize.IsEnabled && (!int.TryParse(this._ctlBatchSize.Text, out batchSize) || batchSize <= 0))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Batch size must be a positive integer.");
                return false;
            }

            if (string.IsNullOrEmpty(this._ctlResources.Text))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("A resource is required to create event source.");
                return false;
            }


            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error adding event source", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error adding event source: " + e.Message);
                return false;
            }
        }

        private void EventSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If this is null it means the event is being fired before the whole screen is initialized.
            if (this._ctlBatchSize == null)
                return;


            switch (this._ctlSourceType.SelectedIndex)
            {
                case SOURCE_TYPE_S3_INDEX:
                    UpdateForS3Type();
                    this._ctlBatchSize.IsEnabled = false;
                    this._ctlStartingPosition.IsEnabled = false;
                    break;
                case SOURCE_TYPE_KINESIS_INDEX:
                    UpdateForKinesisType();
                    this._ctlBatchSize.IsEnabled = true;
                    this._ctlStartingPosition.IsEnabled = true;
                    break;
                case SOURCE_TYPE_DYNAMODB_STREAM_INDEX:
                    UpdateForDynamoDBType();
                    this._ctlBatchSize.IsEnabled = true;
                    this._ctlStartingPosition.IsEnabled = true;
                    break;
                case SOURCE_TYPE_SNS_INDEX:
                    UpdateForSNSType();
                    this._ctlBatchSize.IsEnabled = false;
                    this._ctlStartingPosition.IsEnabled = false;
                    break;
            }
        }

        private void UpdateForS3Type()
        {
            this._ctlResourceLabel.Text = "S3 Bucket:";

            this._ctlResources.Items.Clear();

            try
            {
                PopulateResources(this._controller.GetS3Buckets());
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting the list of buckets: " + e.Message);
            }
        }

        private void UpdateForSNSType()
        {
            this._ctlResourceLabel.Text = "SNS Topic:";

            this._ctlResources.Items.Clear();

            try
            {
                PopulateResources(this._controller.GetSNSTopics());
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting the list of topics: " + e.Message);
            }
        }

        private void UpdateForDynamoDBType()
        {
            this._ctlResourceLabel.Text = "DynamoDB Stream:";

            try
            {
                PopulateResources(this._controller.GetDynamoDBStreams());
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting the list of dynamodb streams: " + e.Message);
            }
        }

        private void UpdateForKinesisType()
        {
            this._ctlResourceLabel.Text = "Kinesis Stream:";

            try
            {
                PopulateResources(this._controller.GetKinesisStreams());
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error getting the list of Kinesis streams: " + e.Message);
            }
        }

        private void PopulateResources(IEnumerable<string> resources)
        {
            this._ctlResources.Items.Clear();
            foreach (var resource in resources)
            {
                this._ctlResources.Items.Add(resource);
            }
            this._ctlResources.SelectedIndex = 0;
        }
    }
}
