using System;
using System.Windows;
using Amazon.DynamoDBv2;

using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.DynamoDB.Controller;
using log4net;

namespace Amazon.AWSToolkit.DynamoDB.View
{
    /// <summary>
    /// Interaction logic for StreamPropertiesControl.xaml
    /// </summary>
    public partial class StreamPropertiesControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(StreamPropertiesControl));

        StreamPropertiesController _controller;

        public StreamPropertiesControl(StreamPropertiesController controller)
        {
            InitializeComponent();

            this._controller = controller;
        }

        public override bool SupportsBackGroundDataLoad => true;

        protected override object LoadAndReturnModel()
        {
            this._controller.LoadModel();
            return this._controller.Model;
        }

        public override string Title => "Stream Properties";

        private void _ctlEnabled_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsEnabled && this._ctlEnabled.IsChecked.GetValueOrDefault() && this._ctlViewType.SelectedItem == null)
            {
                this._controller.Model.SelectedViewType = this._controller.Model.FindViewType(StreamViewType.NEW_AND_OLD_IMAGES);
            }
        }

        public override bool Validated()
        {
            if (this._controller.Model.EnableStream && this._controller.Model.SelectedViewType == null)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("View type must be set when streams are enabled.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                return this._controller.Persist();
            }
            catch (Exception e)
            {
                LOGGER.Error("Error updating stream", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error updating stream: " + e.Message);
                return false;
            }
        }

    }
}
