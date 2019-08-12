using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for TakeSnapshotControl.xaml
    /// </summary>
    public partial class TakeSnapshotControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        TakeSnapshotController _controller;

        public TakeSnapshotControl(TakeSnapshotController controller)
        {
            InitializeComponent();

            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Take DB Instance";

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.SnapshotName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("\"Snapshot Name\" is a required field.");
                return false;
            }

            if (!char.IsLetter(this._controller.Model.SnapshotName[0]))
            {
                throw new Exception("First charater of snapshot name must be a letter.");
            }
            foreach (var c in this._controller.Model.SnapshotName)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    throw new Exception("Snapshot name must contain only letters, digits, or hyphens.");
                }
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.TakeSnapshot();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error take DB instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error take DB instance: " + e.Message);
                return false;
            }
        }
    }
}
