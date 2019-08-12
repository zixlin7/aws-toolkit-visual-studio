using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.IdentityManagement.Controller;

namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for CreateGroupControl.xaml
    /// </summary>
    public partial class CreateGroupControl : BaseAWSControl
    {
        CreateGroupController _controller;

        public CreateGroupControl(CreateGroupController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title => "Create Group";

        public override bool Validated()
        {
            if (this._controller.Model.GroupName == null || this._controller.Model.GroupName.Trim() == "")
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The group name is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                string newName = this._controller.Model.GroupName == null ? string.Empty : this._controller.Model.GroupName;
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
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating group: " + e.Message);
                return false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlGroupName.Focus();
        }
    }
}
