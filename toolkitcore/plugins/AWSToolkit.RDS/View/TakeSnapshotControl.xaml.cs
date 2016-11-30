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

        public override string Title
        {
            get
            {
                return "Take DB Instance";
            }
        }

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
