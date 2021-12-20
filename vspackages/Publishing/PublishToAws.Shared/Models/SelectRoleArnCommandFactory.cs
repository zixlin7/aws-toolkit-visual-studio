using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;

namespace Amazon.AWSToolkit.Publish.Models
{
    public static class SelectRoleArnCommandFactory
    {
        public static ICommand Create(IamRoleConfigurationDetail roleDetail, IPublishToAwsProperties _publishToAwsProperties, IDialogFactory _dialogFactory)
        {
            return new RelayCommand(
                _ => !roleDetail.CreateNewRole && roleDetail.RoleArnDetail != null,
                _ =>
                {
                    var dlg = _dialogFactory.CreateIamRoleSelectionDialog();
                    dlg.Region = _publishToAwsProperties.Region;
                    dlg.CredentialsId = _publishToAwsProperties.CredentialsId;
                    dlg.RoleArn = roleDetail.RoleArnDetail.Value as string;
                    if (dlg.Show())
                    {
                        roleDetail.RoleArnDetail.Value = dlg.RoleArn;
                    }
                });
        }
    }
}
