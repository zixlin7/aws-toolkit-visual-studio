using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Navigator;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for DeleteDBInstanceControl.xaml
    /// </summary>
    public partial class DeleteDBInstanceControl : BaseAWSControl
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        DeleteDBInstanceController _controller;

        public DeleteDBInstanceControl(DeleteDBInstanceController controller)
        {
            InitializeComponent();
            _controller = controller;
            DataContext = _controller.Model;
        }

        public override string Title => "Delete DB Instance";

        public override bool Validated()
        {
            if (_controller.Model.CreateFinalSnapshot)
            {
                if (string.IsNullOrEmpty(_controller.Model.FinalSnapshotName))
                {
                    _controller.ToolkitContext.ToolkitHost.ShowError("\"Final Snapshot Name\" is a required field when \"Create Final Snapshot\" is selected.");
                    return false;
                }

                if (!char.IsLetter(_controller.Model.FinalSnapshotName[0]))
                {
                    throw new Exception("First character of snapshot name must be a letter.");
                }
                foreach (var c in _controller.Model.FinalSnapshotName)
                {
                    if (!char.IsLetterOrDigit(c) && c != '-')
                    {
                        throw new Exception("Name must contain only letters, digits, or hyphens.");
                    }
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                _controller.DeleteDBInstance();
                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Error deleting DB instance", e);
                _controller.ToolkitContext.ToolkitHost.ShowError("Error deleting DB instance: " + e.Message);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }
    }
}
