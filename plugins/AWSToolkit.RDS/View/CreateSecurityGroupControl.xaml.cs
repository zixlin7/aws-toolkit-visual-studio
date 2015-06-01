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
    /// Interaction logic for CreateSecurityGroupControl.xaml
    /// </summary>
    public partial class CreateSecurityGroupControl : BaseAWSControl
    {
        static ILog LOGGER = LogManager.GetLogger(typeof(CreateSecurityGroupControl));

        public CreateSecurityGroupControl()
        {
            InitializeComponent();
        }

        CreateSecurityGroupController _controller;

        public CreateSecurityGroupControl(CreateSecurityGroupController controller)
        {
            InitializeComponent();
            this._controller = controller;
            this.DataContext = this._controller.Model;
        }

        public override string Title
        {
            get { return "Create Security Group"; }
        }

        public override bool Validated()
        {
            if (string.IsNullOrEmpty(this._controller.Model.Name))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Name is a required field.");
                return false;
            }

            foreach (var c in this._controller.Model.Name)
            {
                if (!char.IsLetterOrDigit(c) && c != '-')
                {
                    throw new Exception("Name must contain only letters, digits, or hyphens.");
                }
            }

            if (string.IsNullOrEmpty(this._controller.Model.Description))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Description is a required field.");
                return false;
            }
            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                this._controller.CreateSecurityGroup();
                return true;
            }
            catch (Exception e)
            {
                LOGGER.Error("Error creating security group", e);
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating security group: " + e.Message);
                return false;
            }
        }

        void onLoad(object sender, RoutedEventArgs e)
        {
            this._ctlName.Focus();
        }
    }
}
