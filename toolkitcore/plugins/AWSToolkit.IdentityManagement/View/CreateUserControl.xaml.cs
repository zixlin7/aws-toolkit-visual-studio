using System;
using System.Windows;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.IdentityManagement.Controller;
using Amazon.AWSToolkit.Navigator;

namespace Amazon.AWSToolkit.IdentityManagement.View
{
    /// <summary>
    /// Interaction logic for CreateUserControl.xaml
    /// </summary>
    public partial class CreateUserControl : BaseAWSControl
    {
        CreateUserController _controller;

        public CreateUserControl(CreateUserController controller)
        {
            this._controller = controller;
            this.DataContext = this._controller.Model;
            InitializeComponent();
        }

        public override string Title => "Create User";

        public override bool Validated()
        {
            if (this._controller.Model.UserName == null || this._controller.Model.UserName.Trim() == "")
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("The user name is a required field.");
                return false;
            }

            return true;
        }

        public override bool OnCommit()
        {
            try
            {
                string newName = this._controller.Model.UserName == null ? string.Empty : this._controller.Model.UserName;
                if (newName.Trim().Equals(string.Empty))
                {
                    ToolkitFactory.Instance.ShellProvider.ShowError("Name is required.");
                    _controller.RecordMetric(ActionResults.CreateFailed());
                    return false;
                }

                this._controller.Persist();
                return true;
            }
            catch (Exception e)
            {
                ToolkitFactory.Instance.ShellProvider.ShowError("Error creating user: " + e.Message);

                // Record failures immediately -- the top level call records success/cancel once the dialog is closed
                _controller.RecordMetric(ActionResults.CreateFailed(e));
                return false;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            this._ctlUserName.Focus();
        }
    }
}
