using System.Windows;

using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.IdentityManagement.Model;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    public class SaveServiceSpecificCredentialsController
    {
        public SaveServiceSpecificCredentialsController(ServiceSpecificCredential generatedCredentials, string msg = null)
        {
            Model = new SaveServiceSpecificCredentialsModel(generatedCredentials);
            View = new SaveServiceSpecificCredentialsControl(this, msg);
        }

        public SaveServiceSpecificCredentialsModel Model { get; }

        public SaveServiceSpecificCredentialsControl View { get; }

        public string SelectedFilename => Model.Filename;

        public ActionResults Execute()
        {
            return new ActionResults().WithSuccess(ToolkitFactory.Instance.ShellProvider.ShowModal(View, MessageBoxButton.OK));
        }
    }
}
