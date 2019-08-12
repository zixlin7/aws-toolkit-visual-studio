using System;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using log4net;

namespace Amazon.AWSToolkit.RDS.View
{
    /// <summary>
    /// Interaction logic for DeleteDBInstanceControl.xaml
    /// </summary>
    public partial class DeleteDBInstanceControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        DeleteDBInstanceController _controller;

        public DeleteDBInstanceControl(DeleteDBInstanceController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title => "Delete DB Instance";

        public override bool Validated()
        {
            if (this._controller.Model.CreateFinalSnapshot)
            {
                if (string.IsNullOrEmpty(this._controller.Model.FinalSnapshotName))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("\"Final Snapshot Name\" is a required field when \"Create Final Snapshot\" is selected.");
                    return false;
                }

                if (!char.IsLetter(this._controller.Model.FinalSnapshotName[0]))
                {
                    throw new Exception("First charater of snapshot name must be a letter.");
                }
                foreach (var c in this._controller.Model.FinalSnapshotName)
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
                this._controller.DeleteDBInstance();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error deleting DB instance", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error deleting DB instance: " + e.Message);
                return false;
            }
        }
    }
}
