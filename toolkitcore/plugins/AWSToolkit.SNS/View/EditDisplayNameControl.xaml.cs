using System;
using System.Collections.Generic;
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
using Amazon.AWSToolkit.SNS.Model;
using Amazon.AWSToolkit.SNS.Controller;

namespace Amazon.AWSToolkit.SNS.View
{
    /// <summary>
    /// Interaction logic for EditDisplayNameControl.xaml
    /// </summary>
    public partial class EditDisplayNameControl : BaseAWSControl
    {
        EditDisplayNameController _controller;


        public EditDisplayNameControl(EditDisplayNameController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }


        public override string Title
        {
            get
            {
                return "Edit Display Name";
            }
        }

        public override bool OnCommit()
        {
            if (string.IsNullOrEmpty(this._controller.Model.DisplayName))
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Display name is required!");
                return false;
            }

            this._controller.Persist();
            return true;
        }    

    }
}
