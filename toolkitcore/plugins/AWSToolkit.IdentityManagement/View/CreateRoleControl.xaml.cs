using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
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

        public override string Title => "Create Role";

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
