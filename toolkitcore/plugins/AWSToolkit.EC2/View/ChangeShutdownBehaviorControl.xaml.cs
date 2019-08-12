using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
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

        public override string Title => "Change Shutdown Behavior";

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
