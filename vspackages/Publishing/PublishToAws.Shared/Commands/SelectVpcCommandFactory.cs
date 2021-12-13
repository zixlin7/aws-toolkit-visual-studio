using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class SelectVpcCommandFactory
    {
        public static ICommand Create(VpcConfigurationDetail vpcDetail,
            IPublishToAwsProperties publishToAwsProperties, IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ => vpcDetail.VpcOption == VpcOption.Existing,
                _ =>
                {
                    var dlg = dialogFactory.CreateVpcSelectionDialog();
                    dlg.Region = publishToAwsProperties.Region;
                    dlg.CredentialsId = publishToAwsProperties.CredentialsId;
                    dlg.VpcId = vpcDetail.VpcIdDetail.Value as string;
                    if (dlg.Show())
                    {
                        vpcDetail.VpcIdDetail.Value = dlg.VpcId;
                    }
                });
        }
    }
}
