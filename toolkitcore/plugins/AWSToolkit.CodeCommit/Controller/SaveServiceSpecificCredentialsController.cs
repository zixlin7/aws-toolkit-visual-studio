using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Amazon.AWSToolkit.CodeCommit.Model;
using Amazon.AWSToolkit.CodeCommit.View;
using Amazon.AWSToolkit.Navigator;
using Amazon.IdentityManagement.Model;
using log4net;

namespace Amazon.AWSToolkit.CodeCommit.Controller
{
    public class SaveServiceSpecificCredentialsController
    {
        private readonly ILog LOGGER = LogManager.GetLogger(typeof(SaveServiceSpecificCredentialsController));

        public SaveServiceSpecificCredentialsModel Model { get; }

        public SaveServiceSpecificCredentialsControl View { get; }

        public string SelectedFilename
        {
            get { return Model.Filename; }
        }

        public SaveServiceSpecificCredentialsController(ServiceSpecificCredential generatedCredentials, string msg = null)
        {
            Model = new SaveServiceSpecificCredentialsModel(generatedCredentials);
            View = new SaveServiceSpecificCredentialsControl(this, msg);
        }

        public ActionResults Execute()
        {
            if (ToolkitFactory.Instance.ShellProvider.ShowModal(View, MessageBoxButton.OK))
            {

                return new ActionResults().WithSuccess(true);
            }

            return new ActionResults().WithSuccess(false);
        }
    }
}
