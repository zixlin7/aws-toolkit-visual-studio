using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for CreateSnapshotControl.xaml
    /// </summary>
    public partial class CreateSnapshotControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSnapshotControl));
        CreateSnapshotController _controller;
        public CreateSnapshotControl(CreateSnapshotController controller)
        {
            InitializeComponent();
            _controller = controller;
            this.DataContext = _controller.Model;
        }

        public override string Title => "Create Snapshot";

        protected override object LoadAndReturnModel()
        {
            return _controller.Model;
        }

        public override bool Validated()
        {
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                string snapshotId = _controller.CreateSnapshot();
                ToolkitFactory.Instance.ShellProvider.ShowMessage("Snapshot created", String.Format("Snapshot created with id {0}.", snapshotId));
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating snapshot", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating snapshot: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs evt)
        {
        }

    }
}
