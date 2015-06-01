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
using Amazon.AWSToolkit.IdentityManagement.Model;
using Amazon.AWSToolkit.IdentityManagement.Controller;

namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for CreateRoleControl.xaml
    /// </summary>
    public partial class CreateRoleControl : BaseAWSControl
    {
        CreateRoleController _controller;

        public CreateRoleControl(CreateRoleController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title
        {
            get
            {
                return "Create Role";
            }
        }

        public override bool Validated()
        {
            if (this._controller.Model.RoleName == null || this._controller.Model.RoleName.Trim() == "")
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The role name is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                string newName = this._controller.Model.RoleName == null ? string.Empty : this._controller.Model.RoleName;
                if (newName.Trim().Equals(string.Empty))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                    return false;
                }

                this._controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating role: " + e.Message);
                return false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlRoleName.Focus();
        }    
    }
}
