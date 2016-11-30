using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
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


using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.RDS.Controller;
using Amazon.AWSToolkit.RDS.Model;

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

        public override string Title
        {
            get
            {
                return "Delete DB Instance";
            }
        }

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
