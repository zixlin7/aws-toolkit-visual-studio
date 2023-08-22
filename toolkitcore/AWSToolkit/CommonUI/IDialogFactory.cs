using Amazon.AWSToolkit.CommonUI.CredentialSelector;
using Amazon.AWSToolkit.CommonUI.Dialogs;
using Amazon.AWSToolkit.Telemetry.Model;

namespace Amazon.AWSToolkit.CommonUI
{
    /// <summary>
    /// Toolkit dialog producer
    /// </summary>
    public interface IDialogFactory
    {
        IOpenFileDialog CreateOpenFileDialog();
        IFolderBrowserDialog CreateFolderBrowserDialog();
        ISelectionDialog CreateSelectionDialog();
        IIamRoleSelectionDialog CreateIamRoleSelectionDialog();
        IVpcSelectionDialog CreateVpcSelectionDialog();
        IInstanceTypeSelectionDialog CreateInstanceTypeSelectionDialog();
        IKeyValueEditorDialog CreateKeyValueEditorDialog();
        ICredentialSelectionDialog CreateCredentialsSelectionDialog();
        IEcrRepositorySelectionDialog CreateEcrRepositorySelectionDialog();
        ICloneCodeCommitRepositoryDialog CreateCloneCodeCommitRepositoryDialog();
        ICloneCodeCatalystRepositoryDialog CreateCloneCodeCatalystRepositoryDialog();
        ISsoLoginDialog CreateSsoLoginDialog();
        ICredentialProfileDialog CreateCredentialProfileDialog(BaseMetricSource saveMetricSource);
    }
}
