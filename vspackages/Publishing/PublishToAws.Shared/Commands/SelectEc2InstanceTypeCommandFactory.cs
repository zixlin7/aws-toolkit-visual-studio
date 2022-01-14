using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class SelectEc2InstanceTypeCommandFactory
    {
        public static ICommand Create(
            Ec2InstanceConfigurationDetail instanceTypeDetail,
            IPublishToAwsProperties publishToAwsProperties,
            IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ => instanceTypeDetail != null,
                _ =>
                {
                    var dlg = dialogFactory.CreateInstanceTypeSelectionDialog();
                    dlg.Region = publishToAwsProperties.Region;
                    dlg.CredentialsId = publishToAwsProperties.CredentialsId;
                    dlg.InstanceTypeId = instanceTypeDetail.InstanceTypeId as string;
                    if (dlg.Show())
                    {
                        instanceTypeDetail.InstanceTypeId = dlg.InstanceTypeId;
                    }
                });
        }
    }
}
