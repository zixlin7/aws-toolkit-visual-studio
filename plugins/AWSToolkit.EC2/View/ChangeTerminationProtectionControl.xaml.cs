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
using Amazon.AWSToolkit.EC2.Controller;
using Amazon.AWSToolkit.EC2.Model;

using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeTerminationProtectionControl.xaml
    /// </summary>
    public partial class ChangeTerminationProtectionControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ChangeTerminationProtectionControl));

        ChangeTerminationProtectionController _controller;

        public ChangeTerminationProtectionControl(ChangeTerminationProtectionController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Change Termination Protection"; }
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.ChangeTerminationProtection();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing termination protection", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing termination protection: " + e.Message);
                return false;
            }
        }
    }
}
