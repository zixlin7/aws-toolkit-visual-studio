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


        public override string Title
        {
            get { return "User Data"; }
        }

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
