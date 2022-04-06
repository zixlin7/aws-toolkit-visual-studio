using System.Windows.Input;

using Amazon.AWSToolkit.Commands;
using Amazon.AWSToolkit.CommonUI;
using Amazon.AWSToolkit.Publish.Models;
using Amazon.AWSToolkit.Publish.Models.Configuration;

namespace Amazon.AWSToolkit.Publish.Commands
{
    public static class SelectEcrRepoCommandFactory
    {
        public static ICommand Create(EcrRepositoryConfigurationDetail repoDetail,
            IPublishToAwsProperties publishToAwsProperties, IDialogFactory dialogFactory)
        {
            return new RelayCommand(
                _ =>
                {
                    var dlg = dialogFactory.CreateEcrRepositorySelectionDialog();

                    dlg.Region = publishToAwsProperties.Region;
                    dlg.CredentialsId = publishToAwsProperties.CredentialsId;
                    dlg.RepositoryName = repoDetail.Value as string;

                    if (!dlg.Show()) return;

                    repoDetail.Value = dlg.RepositoryName;
                });
        }
    }
}
