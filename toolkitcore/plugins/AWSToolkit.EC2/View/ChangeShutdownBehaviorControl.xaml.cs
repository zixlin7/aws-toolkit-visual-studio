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
    /// Interaction logic for ChangeShutdownBehaviorControl.xaml
    /// </summary>
    public partial class ChangeShutdownBehaviorControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ChangeShutdownBehaviorControl));

        ChangeShutdownBehaviorController _controller;

        public ChangeShutdownBehaviorControl(ChangeShutdownBehaviorController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Change Shutdown Behavior"; }
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.ChangeShutdownBehavior();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing shutdown behavior", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing shutdown behavior: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlBehavior.Focus();
        }
    }
}
