using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.EC2.Controller;
using log4net;

namespace Amazon.AWSToolkit.EC2.View
{
    /// <summary>
    /// Interaction logic for ChangeUserDataControl.xaml
    /// </summary>
    public partial class ChangeUserDataControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(ChangeUserDataControl));

        ChangeUserDataController _controller;

        public ChangeUserDataControl(ChangeUserDataController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
            checkWarningMessage();
        }


        public override string Title => "User Data";

        public override bool OnCommit()
        {
            try
            {
                this._controller.ChangeUserData();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error changing user data", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error changing user data: " + e.Message);
                return false;
            }
        }

        void checkWarningMessage()
        {
            if (this._controller.Model.IsReadOnly)
            {
                this._ctlWarning.Visibility = Visibility.Visible;
                this._ctlWarning.Height = double.NaN;
            }
            else
            {
                this._ctlWarning.Visibility = Visibility.Hidden;
                this._ctlWarning.Height = 0;
            }
        }
    }
}
